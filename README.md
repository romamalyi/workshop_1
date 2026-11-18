# LeoHR

Internal web app for a single company's HR team: employee records, promotions, onboarding, and
promotion activities. **Angular 21** frontend (`leohr.client/`) + **.NET 10** ASP.NET Core API
(`LeoHR.Server/`). The user base is tiny — favour simple, readable code over abstraction and caching.

## Architecture

The backend follows **Vertical Slice Architecture**: one folder per feature under `Features/`, one
service per feature, thin controllers, DTO validation via `DataAnnotations`, cross-cutting code
behind interfaces in `Shared/`. Services return `Result`/`Result<T>` (FluentResults); controllers
call `.ToActionResult()`. Slices never reference each other: a slice reads other slices' tables straight
from `LeoHrDbContext` and writes to them only through the owning slice. Full guide and rationale:
[`LeoHR.Server/README.md`](LeoHR.Server/README.md); the enforced rules are in
[`CLAUDE.md`](CLAUDE.md).

### Two databases

| Context | Database | Access |
|---|---|---|
| `LeoHrDbContext` | PostgreSQL (local Docker in dev) | read/write, snake_case, EF migrations applied on startup |
| `TimeTrackingDbContext` | external SQL Server (`leobit.database.windows.net`) | **read-only** — `SaveChanges` throws; source of employee and time-report data; needs VPN |

`LeoHrDbContext.SaveChangesAsync` fills audit fields (`CreatedAt`/`CreatedById`/`UpdatedAt`/`UpdatedBy`)
for `BaseAuditableEntity` — never set them by hand. Enum columns use `HasConversion<string>()`, so a
new enum value needs no migration.

### Config, secrets, auth

`appsettings.{Environment}.json` is committed; `secrets.{Environment}.json` is gitignored and holds
the TimeTracking connection string (see "Set up secrets" below). Environments are **Test** (local dev
and the test server) and **Production**. Auth is OIDC against Leobit IdentityServer, with two
policies: `Policies.Default` and `Policies.Manage` (manage-only controller actions add
`[Authorize(Policy = nameof(Policies.Manage))]`).

## Domain model

### Activities and promotions

- **Activity** (`ActivityEntity`) — a catalogue entry an employee can complete for points toward a
  promotion. Has an `ActivityCategory`, a point value, and an `ActivityPeriod` (`WithinPeriod`) that
  decides which time window the completion must fall in: `SixMonths`, `TwelveMonths`, `OverallActive`
  (any time while active), or `OverallInLevel` (since the employee entered the current level).
- **EmployeeActivity** (`EmployeeActivityEntity`) — one activity attached to one employee. Status
  goes `Planned → SelfReported → Approved`. `PointsSnapshot` freezes the catalogue points at
  approval. May be linked to a `Promotion`. An activity edit with "Preserve history" on adds a new version of
  the activity: `Planned` and `SelfReported` records outside `Sent` promotions move to it,
  the rest keep the old version.
- **Promotion** (`PromotionEntity`) — an employee's move from one role/level to the next: a checklist
  of steps, a `Status` (`InProgress ⇄ Ready → Sent`),
  a `ReferenceDate` (set when it is sent), `IsDeleted` (a soft delete, see below), and `LevelStartDate` — a **snapshot** of when the employee
  entered the level, taken when the promotion is created and never moved afterwards. It reads its level's active
  promotion criteria, unless `PromotionCriteriaId` links it to an older version: a criteria edit with "Preserve
  history" on links the promotions using the old version to it before the new version replaces it.

### Automatic promotions

Every active employee keeps one open promotion toward the next level their technology has. A technology's
levels are its active promotion criteria (seniority + sub-level), so they differ per technology and HR changes
them in the admin UI; an employee with no configured level above their own gets no promotion until HR adds one.
`MissingPromotionsJob` creates missing promotions at startup and then daily; sending a promotion creates the
next one straight away. New promotions start `InProgress`. Employees whose `Profile` doesn't map to a
`Technology`, or whose seniority can't be read, are skipped. On the same run the job soft-deletes (`IsDeleted`)
the open promotion of every employee who is no longer active and deletes all its activities; a query filter
hides deleted promotions everywhere, and the promotion list and pending approvals show active employees only.
HR can still create a promotion by hand for an employee without an open one — one the job skipped, or after
deleting the automatic one.

### The activity window

An `OverallInLevel` activity counts toward a promotion only if its `AccomplishedOn` is on or after
that promotion's `LevelStartDate` and on or before its reference date
(`PromotionRules.GetValidApprovedActivities` / `PassesWindow`). `LevelStartDate` is snapshotted per
promotion precisely so a later promotion advancing the "level start" can't retroactively drop
activities out of an earlier one.

### Carry-over clones

When a promotion closes successfully with more approved points than required, the surplus is carried
onto the next promotion as **cloned** `EmployeeActivity` rows with `CarriedFromPromotionId` set.
These are per-promotion accounting artefacts, not activities the employee owns:

- They are excluded everywhere an employee's own activities are listed or edited — the filter is
  `CarriedFromPromotionId == null` (see `EmployeeActivitiesService`).
- Deleting a promotion deletes its clones, so the `ON DELETE SET NULL` foreign key can't strip
  `CarriedFromPromotionId` and turn them back into standalone activities.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Access to `leobit.database.windows.net` for the TimeTracking database (VPN or office network may be required)

## Getting started

### 1. Start the database

    docker-compose up -d

This starts a local PostgreSQL instance (port 5432) and pgAdmin (port 8080) for the LeoHR database. Data is persisted in a Docker volume so it survives restarts.

### 2. Install frontend dependencies

    cd leohr.client
    npm install

### 3. Set up secrets

Non-sensitive settings (`Logging`, `IdentityServer.Authority`, the LeoHR DB connection string) are stored in `appsettings.{Environment}.json` and committed to git.  
Sensitive values (the TimeTracking connection string) are stored in **`secrets.{Environment}.json`** files — these are **gitignored** and must be created manually.

#### Where to get credentials

TimeTracking credentials are stored in the team password manager (TeamPass).  
If you don't have access, ask a teammate or the project lead.

#### Create the files

Copy the template and fill in the real values:

```powershell
Copy-Item LeoHR.Server\secrets.template.json LeoHR.Server\secrets.Test.json
Copy-Item LeoHR.Server\secrets.template.json LeoHR.Server\secrets.Production.json
```

Then open each file and fill in the TimeTracking connection string:

```json
{
  "ConnectionStrings": {
    "TimeTracking": "Server=...;Database=...;User ID=...;Password=...;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  }
}
```

| Secrets file | Environment | Database |
|---|---|---|
| `secrets.Test.json` | Local dev / Test server | LeobitTimetrackingTest |
| `secrets.Production.json` | Production | LeobitTimetracking |

The `LeoHr` connection string is already pre-configured in `appsettings.Test.json` and points to the local Docker container — no manual setup needed.

#### Verify the files are ignored by git

After creating the files, confirm they are not tracked:

```powershell
git status
```

`secrets.Test.json` and `secrets.Production.json` must **not** appear in the output.

### 4. Run (recommended — Visual Studio)

Open `LeoHR.slnx` in Visual Studio and press **F5**.

Visual Studio will:
1. Start the .NET backend on `https://localhost:7019`
2. Automatically start the Angular dev server on `https://localhost:5002`
3. Open the browser

### Run (manual — two terminals)

**Terminal 1 — backend:**

    cd LeoHR.Server
    dotnet run --launch-profile Test

**Terminal 2 — frontend:**

    cd leohr.client
    npm start

Then open `https://localhost:5002` in the browser.

## Environment summary

| Environment | `ASPNETCORE_ENVIRONMENT` | Identity Server | Database |
|---|---|---|---|
| Local dev / Test server | `Test` | test.identity.leobit.co | LeobitTimetrackingTest |
| Production | `Production` | identity.leobit.co | LeobitTimetracking |

## Angular builds

| Command | Description |
|---|---|
| `ng serve` | Local dev (uses `environment.ts`) |
| `ng build` | Production build (uses `environment.prod.ts`) |
| `ng build --configuration=test` | Test build (uses `environment.test.ts`) |

