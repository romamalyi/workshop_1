# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

LeoHR is an internal web app for HR staff (employee records, promotions, onboarding, activities).
**Frontend:** Angular 21 (`leohr.client/`). **Backend:** ASP.NET Core Web API, .NET 10 / C# 14 (`LeoHR.Server/`).
The user base is tiny — a single company's HR team. **Don't over-engineer:** no high-load caching, no
premature abstraction. Simple, readable code wins.

## Commands

### Full stack (local dev)

```bash
docker-compose up -d          # Postgres (5432) + pgAdmin (8080) for the LeoHR DB
```

Then either open `LeoHR.slnx` in Visual Studio and press F5 (starts backend + Angular dev server + browser),
or run the two sides manually:

```bash
cd LeoHR.Server && dotnet run --launch-profile Test    # backend on https://localhost:7019
cd leohr.client && npm start                            # frontend on https://localhost:5002
```

The Angular dev server proxies `/api` to the backend, `/connect/*` to the identity server, and `/v1` to
TimeTracking (`src/proxy.conf.js`). Run alone (no backend env vars) it proxies `/api` to the deployed test API.

### Backend (`LeoHR.Server/`)

```bash
dotnet build LeoHR.Server/LeoHR.Server.csproj
dotnet format LeoHR.Server/LeoHR.Server.csproj --verify-no-changes --severity warn   # CI enforces this
dotnet ef migrations add <Name> --project LeoHR.Server                               # EF is design-time via LeoHrDbContextFactory
```

Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `Program.cs`), which also
seeds reference data via `LeoHrSeeder`. `<TreatWarningsAsErrors>`-style analysis is on
(`CodeAnalysisTreatWarningsAsErrors`, `EnforceCodeStyleInBuild`) — a warning fails the build.

### Frontend (`leohr.client/`)

```bash
npm install                       # CI uses: npm ci --legacy-peer-deps
npm run lint                      # angular-eslint; template prefer-control-flow is an ERROR
npm run format:check              # prettier (npm run format to fix)
npm run build                     # prod build; add -- --configuration=test for the Test build
npm test                          # Angular @angular/build:unit-test runner — NOTE: there are currently no *.spec.ts files
```

A husky `pre-commit` hook runs `lint-staged` (prettier + `eslint --fix` on staged files).

There is no backend test project. There are no frontend tests yet.

## Big-picture architecture

### Two databases, one read-only

| Context | DB | Access |
|---|---|---|
| `LeoHrDbContext` | PostgreSQL (local Docker in dev) | read/write, `UseSnakeCaseNamingConvention`, EF migrations |
| `TimeTrackingDbContext` | external SQL Server (`leobit.database.windows.net`) | **read-only** — `SaveChanges` throws; `NoTracking`; source of employee/time data; needs VPN |

Enum columns use `HasConversion<string>()`, so adding a new enum value (e.g. a Technology track) needs no
migration — see `LeoHR.Server/README.md`.

`LeoHrDbContext.SaveChangesAsync` auto-populates audit fields for entities inheriting `BaseAuditableEntity`
(`CreatedAt/CreatedById/UpdatedAt/UpdatedBy`) — never set these by hand.

### Config & secrets

`appsettings.{Environment}.json` is committed (logging, identity authority, the local LeoHR connection string).
`secrets.{Environment}.json` is **gitignored** and holds the TimeTracking connection string; it is loaded by
`Program.cs` and, at design time, by `LeoHrDbContextFactory`. Copy `secrets.template.json` to
`secrets.Test.json` / `secrets.Production.json`. Environments: **Test** (local dev and the test server) and
**Production**.

### Auth

OIDC against Leobit IdentityServer (`test.identity.leobit.co` / `identity.leobit.co`). Client uses
`angular-oauth2-oidc`; server uses JwtBearer (`AddLeoHRAuthentication`). Two authorization policies:
`Policies.Default` (the default policy) and `Policies.Manage`. Controllers are `[Authorize]`; manage-only
actions add `[Authorize(Policy = nameof(Policies.Manage))]`.

### Backend — Vertical Slice Architecture

One folder per feature under `Features/`, one service per feature (methods, not handler classes), thin
controllers, DTO validation via `DataAnnotations`. Slices never reference each other — see rule 3 below
and `LeoHR.Server/README.md`.

> The server README's section 11 ("throw domain-meaningful exceptions") is outdated. The codebase uses the
> **FluentResults** Results pattern (rule 6 below): services return `Result`/`Result<T>`, controllers call
> `.ToActionResult()` from `Shared/Extensions/ResultExtensions.cs`.

### Frontend structure

- **NgModule-based**, not standalone (`angular.json` schematics set `standalone: false`; components declare
  `standalone: false` explicitly). This overrides the "standalone always" guidance in the
  `angular21-best-practices` skill — do **not** convert this project to standalone components.
- Feature folders under `src/app/<feature>/` with `components/`, `services/`, `models/`, and a
  `<feature>.module.ts` + `<feature>-routing.module.ts`. App-wide singletons in `src/app/core/`
  (auth, guards, strategies). Reusable UI in `src/app/shared/`.
- Modern Angular idioms still apply: `inject()`, `signal()`/`computed()`, `ChangeDetectionStrategy.OnPush`,
  `takeUntilDestroyed`, RxJS streams over imperative component code, and built-in control flow
  (`@if`/`@for`/`@switch`/`@empty`) — `*ngIf`/`*ngFor` are lint errors and `CommonModule` should not be imported.
- Signal-based component APIs: `input()`, `output()`, `model()` instead of `@Input()`/`@Output()`/`@Input() set`;
  `computed()` for derived state, `effect()` only when a side effect is unavoidable.
- Another feature module's service is called only to read; changes go through the owning feature.
- Styling: Tailwind v4 utilities directly in templates; colors as CSS variables in `:root` (`styles.css`);
  component CSS only when unavoidable, scoped to the file that uses it.

### Deployment (GitLab CI, `.gitlab-ci.yml`)

`develop` → build + deploy to Test (automatic). `master` → build + deploy to Production (manual gate).
`dotnet publish` runs the Angular build itself (via the `.esproj` project reference) and emits the SPA into
`wwwroot/`, so a single ASP.NET process serves both the SPA and `/api`, reverse-proxied by Nginx on EC2.
CI fails the build on `dotnet format --verify-no-changes`.

---

# LeoHR — Architecture Rules

Backend follows **Vertical Slice Architecture (VSA)** with plain controllers and **one service per feature**. No MediatR, no FluentValidation (use `DataAnnotations`), no CQRS, no per-use-case handler classes, no repository pattern.

See `LeoHR.Server/README.md` for the full guide and rationale.

## Folder layout

```
LeoHR.Server/
  Features/<FeatureName>/        ← one folder per business feature
    <FeatureName>Controller.cs    ← thin controller, one action per use case
    I<FeatureName>Service.cs      ← service interface
    <FeatureName>Service.cs       ← service impl, many methods
    Dto/                          ← feature-specific DTOs
  Shared/                         ← everything cross-feature: shared services (DbContexts, auth, email, toggles) AND passive types (DTOs, enums, constants)
  Models/Entities/                ← EF Core entities (shared across slices)
  Program.cs
```

## Rules

1. **One feature = one folder** under `Features/`: controller, `I<Feature>Service`, `<Feature>Service`, `Dto/`. A new use case is a new method on that service, not a new class.
2. **Thin controllers:** one action = one service call, no business logic. Validate DTOs with `DataAnnotations`.
3. **Slices never reference each other** — not directly, and not through a `Shared/` interface resolved to another slice's main service.
   - Read another slice's tables straight from `LeoHrDbContext` (`.AsNoTracking()` unless attached to an entity you save).
   - Write only your own entities. A write to another slice goes through a `Shared/` interface on a dedicated `<Purpose>Service` in the owning slice (e.g. `IPromotionActivities` → `PromotionActivitiesService`); wrap both saves in a transaction when they must land together.
   - Fix a violation in the MR that meets it.
4. **`Shared/`** holds cross-cutting infra (DbContexts, email, auth, Employee API) and types used by 2+ slices. Entities live in `Models/Entities/`.
5. **Register in `Program.cs`** as `AddScoped<IXxx, XxxService>()`. Never forward a `Shared/` interface with `GetRequiredService<…Service>()`.
6. **Results, not exceptions:** services return `Result`/`Result<T>`; controllers call `.ToActionResult()`.
7. **Audited entities inherit `BaseAuditableEntity`**; never set its fields by hand.
8. **Comments:** one line for a non-obvious *why*. Never describe UI behaviour.
9. **HR-editable seed data** goes in raw SQL in a migration's `Up()`, not `HasData` (a later seed change overwrites HR's edits). `HasData` is only for read-only lists.
10. **API dates are UTC `DateTime`:** every date in a response is a `DateTime` in UTC (ISO 8601 with `Z`); the client converts it to local time for display. Don't return `DateOnly`.
11. **List endpoints are paged:** take a filter model derived from `BaseFilterModel` (`PageNumber`, `PageSize`) `[FromQuery]` and return `PagedResult<T>` (`Shared/Models/Filter/`). Never return a bare `List<T>`.
