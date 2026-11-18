# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

LeoHR is an internal web app for HR staff (employee records, promotions, onboarding, activities).
**Frontend:** Angular 21 (`leohr.client/`). **Backend:** ASP.NET Core Web API, .NET 10 / C# 14 (`LeoHR.Server/`).
The user base is tiny — a single company's HR team. **Don't over-engineer:** no high-load caching, no
premature abstraction. Simple, readable code wins. A short-lived `IMemoryCache` is fine for a frequently
opened read-only list or lookup (e.g. `EmployeeApiClient`) — never for a value a write decision depends on.

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

Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `Program.cs`).
`<TreatWarningsAsErrors>`-style analysis is on
(`CodeAnalysisTreatWarningsAsErrors`, `EnforceCodeStyleInBuild`) — a warning fails the build.

### Frontend (`leohr.client/`)

```bash
npm install                       # CI uses: npm ci --legacy-peer-deps
npm run lint                      # angular-eslint; template prefer-control-flow is an ERROR
npm run format:check              # prettier (npm run format to fix)
npm run build                     # prod build; add -- --configuration=test for the Test build
npm test                          # Angular @angular/build:unit-test runner (Vitest); specs live next to the code as *.spec.ts
```

A husky `pre-commit` hook runs `lint-staged` (prettier + `eslint --fix` on staged files).

```bash
dotnet test LeoHR.Server.IntegrationTests   # CI adds -p:ShouldRunNpmInstall=false
```

Backend tests live in `LeoHR.Server.IntegrationTests` (xUnit; Testcontainers Postgres, so Docker must be running, or set
`LEOHR_TEST_CONNECTION_STRING`). There is no separate unit-test project and no mocking library: pure logic classes
are tested there with hand-written fakes. Frontend specs follow `send-promotion-dialog.component.spec.ts`.

E2E (Playwright against the shared Test env): `cd e2e && npm install && npm test`. Needs a saved Google SSO
session — see `e2e/README.md`.

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

Rules (one service per feature, slices never reference each other, Results not exceptions, etc.) live in
`.claude/rules/architecture.md` and load when backend files are touched; the full guide is `LeoHR.Server/README.md`.

> The README's section 11 ("throw domain-meaningful exceptions") is outdated: the codebase uses the
> **FluentResults** Results pattern: services return `Result`/`Result<T>`, controllers call
> `.ToActionResult()` from `Shared/Extensions/ResultExtensions.cs`.

### Frontend structure

- **NgModule-based** (`angular.json` schematics set `standalone: false`; feature components declare
  `standalone: false` explicitly and are listed in the module's `declarations`). This overrides the
  "standalone always" guidance in the `angular21-best-practices` skill — do **not** convert existing components.
  **Exception:** a component meant for reuse across feature modules without importing the owning module is
  `standalone: true` and listed in the consuming module's `imports` (e.g. `app-confirm-dialog`).
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
  component styles only when unavoidable, scoped to the file that uses it. A component whose design specifies
  its own palette may use a component `.scss` (e.g. `employee-activities-tab.component.scss`); reuse the global
  `.modal-*` / `.btn` classes for layout.

### Deployment (GitLab CI, `.gitlab-ci.yml`)

`develop` → build + deploy to Test (automatic). `master` → build + deploy to Production (manual gate).
`dotnet publish` runs the Angular build itself (via the `.esproj` project reference) and emits the SPA into
`wwwroot/`, so a single ASP.NET process serves both the SPA and `/api`, reverse-proxied by Nginx on EC2.
CI fails the build on `dotnet format --verify-no-changes`.
