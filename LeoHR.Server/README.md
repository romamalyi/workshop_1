# LeoHR.Server — Backend Architecture Guide

This document explains **how the backend is organized, why, and how to add new code**.

If you just want the rules, see `CLAUDE.md` at the repo root.

---

## 1. What architecture do we use?

We use **Vertical Slice Architecture (VSA)** with plain ASP.NET Core controllers and **one service per feature**.

### What that means in one sentence

> Organize code by **feature** (Promotions, Onboarding, Employees…), not by **technical layer** (Controllers, Services, Repositories).

Each feature has its own folder: a thin controller + a service (interface + implementation) + its DTOs. Use cases are methods on the service, not separate classes.

---

## 2. Why VSA (and not classic N-tier)?

The old layered style looked like this:

```
Controllers/     PromotionsController.cs, EmployeesController.cs
Services/        IPromotionsService.cs, PromotionsService.cs, ...
Models/Dto/      CreatePromotionDto.cs, UpdatePromotionDto.cs, ...
Models/Entities/ Promotion.cs, Employee.cs, ...
```

**Problems we hit as the project grew:**
- To change *one* feature you open *many* folders.
- Services start calling each other (`EmployeesService` used inside `PromotionsService`) → tangled dependencies.
- Finding "where the promotion code lives" requires a tour.

**Vertical Slice fixes this by:**
- Grouping files by feature → open one folder, see everything.
- Slices are independent — work on Promotions doesn't touch Employees.
- Matches the Angular client, which is already feature-folder organized.

### Why service-per-feature (not handler-per-use-case)?

We deliberately keep it simple: one service with multiple methods per feature, instead of one class per operation (the CQRS / handler-per-use-case style). Reasons:
- Fewer files, less ceremony.
- No MediatR dependency (commercial license since v12, extra indirection).
- Easy to navigate — one file per feature's logic.
- Still trivial to migrate to handlers later if a real need appears (see section 12).

### Trade-off we accept

Some duplication between slices (e.g. two slices issuing similar queries). That's **fine** — duplication is cheaper than the wrong abstraction. Extract shared code only when the same logic appears in 3+ places.

---

## 3. Folder layout

```
LeoHR.Server/
├── Features/                      ← ALL feature code goes here
│   ├── Promotions/
│   │   ├── PromotionsController.cs
│   │   ├── IPromotionsService.cs
│   │   ├── PromotionsService.cs
│   │   └── Dto/
│   │       └── PromotionDto.cs
│   ├── Employees/
│   ├── Onboarding/
│   ├── EmailTemplates/
│   └── AppSettings/               ← Same pattern, even for cross-cutting features
├── Shared/                        ← Everything cross-feature (behavior AND passive types)
│   ├── Data/                      ← LeoHrDbContext, TimeTrackingDbContext
│   ├── Employees/                 ← IEmployeeApiClient, IEmployeeLookup (Employee API)
│   ├── EmployeeActivities/        ← IPromotionActivities (a write the EmployeeActivities slice owns)
│   ├── Email/                     ← IEmailSender + impl
│   ├── Auth/                      ← auth helpers
│   └── Dto/                       ← cross-slice passive DTOs (e.g. EmployeeRef)
├── Models/Entities/               ← EF Core entities (shared)
├── Migrations/
├── Program.cs
└── appsettings.json
```

### Why one `Shared/` folder instead of `Infrastructure/` + `Shared/`?

Classical Clean Architecture splits them: "Infrastructure" = adapters to the outside world (DB, email, file I/O), "Shared/Common" = passive types or cross-cutting domain helpers. That split pays off in large solutions with strict layering rules.

At our size it adds ceremony without value. Everything that isn't a feature slice goes in `Shared/`, organized by concern (`Shared/Data/`, `Shared/Email/`, etc.). If `Shared/` grows past ~15–20 files and the "adapters vs types" distinction starts mattering, we can split off `Infrastructure/` then — not preemptively.

### Where does each kind of code go?

| Kind of code | Where |
|---|---|
| Controller for a feature | `Features/<Feature>/<Feature>Controller.cs` |
| Business logic for a feature | `Features/<Feature>/<Feature>Service.cs` (add methods as use cases grow) |
| Feature-specific DTOs | `Features/<Feature>/Dto/` |
| EF entity | `Models/Entities/` |
| DbContext, auth, email, caching | `Shared/<Concern>/` |
| Reading another slice's data | A query on `LeoHrDbContext` in your own service — no interface |
| A write another slice owns, or a business rule 2+ slices reuse | `Shared/<Concern>/I<Name>.cs`, implemented by a dedicated `<Purpose>Service` in the owning slice — never its main `<Feature>Service` |
| DTO used by 2+ slices | `Shared/Dto/` |

---

## 4. Anatomy of a slice

### `Features/Promotions/IPromotionsService.cs`

```csharp
public interface IPromotionsService
{
    Task<List<PromotionDto>> GetAllAsync();
    Task<PromotionDto> CreateAsync(CreatePromotionRequest request);
    Task<PromotionDetailDto?> GetDetailAsync(int id);
    Task<bool> ToggleStepAsync(int stepId, bool isDone);
}
```

### `Features/Promotions/PromotionsService.cs`

```csharp
public class PromotionsService(LeoHrDbContext db, IEmployeeLookup employeeLookup)
    : IPromotionsService
{
    public async Task<List<PromotionDto>> GetAllAsync()
    {
        return await db.Promotions
            .Select(p => new PromotionDto(p.Id, p.FromRole, p.ToRole))
            .ToListAsync();
    }

    public async Task<PromotionDto> CreateAsync(CreatePromotionRequest req)
    {
        var p = new Promotion { EmployeeId = req.EmployeeId, FromRole = req.FromRole, ToRole = req.ToRole };
        db.Promotions.Add(p);
        await db.SaveChangesAsync();
        return new PromotionDto(p.Id, p.FromRole, p.ToRole);
    }

    // ... more methods as new use cases appear
}
```

### `Features/Promotions/Dto/PromotionDto.cs`

```csharp
public record PromotionDto(int Id, string FromRole, string ToRole);

public record CreatePromotionRequest(
    [Required] string EmployeeId,
    [Required, MaxLength(50)] string FromRole,
    [Required, MaxLength(50)] string ToRole);
```

---

## 5. Controllers stay thin

```csharp
[ApiController]
[Route("api/[controller]")]
public class PromotionsController(IPromotionsService service) : ControllerBase
{
    [HttpGet]
    public Task<List<PromotionDto>> GetAll() => service.GetAllAsync();

    [HttpPost]
    public Task<PromotionDto> Create([FromBody] CreatePromotionRequest req) =>
        service.CreateAsync(req);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var detail = await service.GetDetailAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }
}
```

Rules:
- One controller action per use case.
- The action only calls the service and returns the result.
- No business logic in controllers.

---

## 6. Registering services

In `Program.cs`:

```csharp
builder.Services.AddScoped<IPromotionsService, PromotionsService>();
builder.Services.AddScoped<IEmployeesService, EmployeesService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
// one line per feature service

builder.Services.AddScoped<IPromotionActivities, PromotionActivitiesService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
```

---

## 7. Shared code and cross-slice data (the important part)

We follow Jimmy Bogard's Vertical Slice Architecture ([talk](https://www.youtube.com/watch?v=oAoaMlS1PWo)): every
slice uses the same `LeoHrDbContext`, and the coupling rules are about **writes**, not reads.

**Slices must not reference each other's services — not directly, and not through a `Shared/` interface that resolves
to another slice's main `<Feature>Service`.** A narrow interface over `PromotionsService` still injects the whole
Promotions slice into the caller. Instead:

| A slice needs… | Do this |
|---|---|
| to read another slice's data | Query its tables straight from `LeoHrDbContext`, with `.AsNoTracking()` unless the row is attached to an entity you save. No `Shared/` interface for a plain read. |
| to change another slice's data | Call the owning slice through a small interface in `Shared/`, implemented by a dedicated `<Purpose>Service` in the owning slice. The write carries the owner's rules, and the owner saves it. When your own change must land together with it, wrap both in a transaction. |
| data it can't reach from the database | Use the `Shared/` interface for it — the Employee API is `IEmployeeApiClient` / `IEmployeeLookup`. |
| a business rule another slice already has | Two copies may stay; on the third, move the rule behind an interface in `Shared/`. |

### What goes in `Shared/`

- **DbContexts** — one per database, referenced by every slice that needs data.
- **Infrastructure** — email, auth, the Employee API client, caching.
- **Owner-write interfaces** — a write another slice needs, implemented by a dedicated `<Purpose>Service` in the slice that owns the entity.
- **Reused business rules** — logic 2+ slices apply, behind an interface.
- **Passive cross-feature types** — DTOs/enums/records used by 2+ slices.

All in the same top-level folder, split into subfolders by concern.

### Example: reading another slice's data

Promotions needs the promotion criteria for a level; the `PromotionCriteria` slice owns them. Promotions reads them
itself:

```csharp
PromotionCriteriaEntity? criteria = await db.PromotionCriteria
    .AsNoTracking()
    .Include(c => c.RequiredActivity)
    .FirstOrDefaultAsync(c => c.IsActive && c.Technology == technology && c.Seniority == seniority);
```

### Example: writing another slice's data

Promotions adds planned activities to a promotion, but `EmployeeActivityEntity` belongs to `EmployeeActivities`. The
owner exposes the write:

```csharp
// Shared/EmployeeActivities/IPromotionActivities.cs
public interface IPromotionActivities
{
    List<EmployeeActivityEntity> AddPlanned(PromotionEntity promotion, IEnumerable<ActivityEntity> activities, string? note);
    Task<Result> RemovePlannedAsync(int promotionId, int employeeActivityId, ClaimsPrincipal user);
    // ...
}
```

`Features/EmployeeActivities/PromotionActivitiesService.cs` implements it and saves each write; `PromotionsService` calls it. Rules such
as "only planned activities can be removed" live with the owner, next to its other writes. The interface always gets its
own `<Purpose>Service` in the owning slice, as here — never the slice's main `<Feature>Service`. That keeps the caller from
depending on the owner's whole slice, and it also rules out DI cycles between slices; don't work around one with
`Lazy<T>` or `IServiceProvider`. If the main service needs the same logic, it calls the `<Purpose>Service` too.

### Example: passive shared DTO

```csharp
// Shared/Dto/EmployeeRef.cs
public record EmployeeRef(string Id, string FirstName, string LastName);
```

No behavior, no DI, just a shape reused by multiple slices.

### When NOT to share

- Two slices do *similar-looking* queries → leave them duplicated.
- Another slice's data is one query away → query it; don't wrap it in an interface.
- You think "I might need this later" → wait until you do.

### Resolve a cross-slice need in the change that meets it

When a change needs another slice's data or behaviour, or meets code that breaks these rules, fix it in the same
change: a read becomes a direct query, and a write gets a method on the owner's `Shared/` interface and its
dedicated `<Purpose>Service` — add them if there aren't any. Don't add a new exception or leave it for later.

---

## 8. Step-by-step: adding a new use case

Suppose you need `POST /api/promotions/{id}/approve`.

1. **Add method to interface** `Features/Promotions/IPromotionsService.cs`:
    ```csharp
    Task<bool> ApproveAsync(int id, string approverComment);
    ```
2. **Implement** in `PromotionsService.cs`:
    ```csharp
    public async Task<bool> ApproveAsync(int id, string approverComment)
    {
        var p = await db.Promotions.FindAsync(id);
        if (p is null) return false;
        p.Approve(approverComment);
        await db.SaveChangesAsync();
        return true;
    }
    ```
3. **Add controller action** in `PromotionsController.cs`:
    ```csharp
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovePromotionRequest req)
    {
        var ok = await service.ApproveAsync(id, req.Comment);
        return ok ? NoContent() : NotFound();
    }
    ```
4. **Add the request DTO** in `Features/Promotions/Dto/`:
    ```csharp
    public record ApprovePromotionRequest([Required] string Comment);
    ```

---

## Adding a new Technology track (e.g. Php, Ruby)

Technology is stored as a string-converted enum (`HasConversion<string>()`), so **no DB migration is needed** when adding a new value.

**Backend — one file:**

`Models/Entities/LeoHr/Technology.cs` — add the new value:
```csharp
public enum Technology
{
    Dotnet,
    Php,   // ← add here
}
```

**Frontend — one file:**

`leohr.client/src/app/promotion-criteria/promotion-criteria.enums.ts` — mirror the change:
```typescript
export enum Technology {
  Dotnet = 'Dotnet',
  Php = 'Php',   // ← add here
}
```

**That's it.** After redeploy the new technology appears automatically in the tab bar and the modal dropdown. An admin can then create criteria rows for it — no further code changes needed.
5. **Run** and test. No DI changes needed — the service is already registered.

---

## Employee events from other apps (`POST api/integrations/events`)

Other internal apps report employee events to LeoHR, and LeoHR sends the email for the event.
Auth: the `Integrations` policy (scope `leohr.integrations`). Slice: `Features/Notifications/`.

```json
{
  "eventType": "EnglishLevelChanged",
  "employeeId": "<Employee API id>",
  "eventDescription": "English level changed from B1 to B2",
  "parameters": { "currentLevel": "B2", "previousLevel": "B1", "strengths": "Fluency\nVocabulary", "improvementAreas": "Grammar" }
}
```

Returns `202 Accepted` with no body. A repeated call sends a repeated email (no deduplication). It fails with
`404` for an unknown employee, and with `400` when a required parameter is missing or longer than 4000 characters.
If the email can't be sent (the template is missing, or SMTP is down) it returns `400` with a generic message and logs an error.

Supported `eventType` values (also visible in the OpenAPI document):

| `eventType` | Email | Recipients | `parameters` |
|---|---|---|---|
| `EnglishLevelChanged` with `parameters` | `english_level_changed` (HTML) | To: the employee, CC: their HR manager | `currentLevel` (required), `previousLevel`, `strengths`, `improvementAreas` |
| `EnglishLevelChanged` without `parameters` | `employee_event` | The employee's HR manager | — |
| `EnglishGroupAssigned` | `english_group_assigned` (HTML) | The employee only | `groupName`, `schedule`, `sessionsPerWeek` (all required) |

- `{employeeName}` comes from the Employee API, not from the caller. Every parameter value is HTML-encoded.
- `strengths` and `improvementAreas` are lists, one item per line (`\n`); LeoHR renders each as a `<ul>`. An empty level shows as `-`.
- When the HR manager of an `EnglishLevelChanged` employee is missing, has no email, or matches several active employees
  by name, the email goes to the employee without CC and a warning is logged.
- The generic `employee_event` email (parameters `{employeeName}`, `{eventType}`, `{eventDescription}`) fails with `400`
  when the employee has no HR manager, the HR manager's email isn't found, or the name matches several active employees.

All templates live in `email_templates` and HR can edit them. To add an event type, add a value to `EmployeeEventType` and
a row to the table above; it uses the generic `employee_event` email unless `NotificationsService` gives it its own.

---

## 9. Step-by-step: adding a new feature

Suppose we add **Vacations**.

1. Create `Features/Vacations/`.
2. Add entity `Models/Entities/Vacation.cs`.
3. Register `DbSet<Vacation>` in `LeoHrDbContext` (shared DbContext — fine).
4. Create and apply EF migration.
5. Add the slice files:
   - `Features/Vacations/IVacationsService.cs`
   - `Features/Vacations/VacationsService.cs`
   - `Features/Vacations/VacationsController.cs`
   - `Features/Vacations/Dto/VacationDto.cs`
6. Register the service in `Program.cs`:
    ```csharp
    builder.Services.AddScoped<IVacationsService, VacationsService>();
    ```

If Vacations needs the current user's settings → query `db.AppSettings` directly.
If Vacations needs employee names → use `IEmployeeLookup` (the Employee API); do **not** call `IEmployeesService` from another slice.
If Vacations needs to change another slice's data → call the interface that slice implements in `Shared/` (section 7).

---

## 10. Validation

Use `DataAnnotations` on the Request record:

```csharp
public record CreatePromotionRequest(
    [Required] string EmployeeId,
    [Required, MaxLength(50)] string FromRole,
    [Required, MaxLength(50)] string ToRole);
```

`[ApiController]` returns a 400 with problem details automatically when validation fails.

If/when validation becomes too complex for attributes, we'll consider FluentValidation — not yet.

---

## 11. Error handling

- Throw domain-meaningful exceptions (`NotFoundException`, `FeatureDisabledException`, etc.).
- A global exception middleware (in `Shared/`) translates them to proper HTTP responses.
- Services return plain result types or `bool` for found/not-found flags — controllers decide the HTTP shape.

---

## 12. When to consider CQRS later

Stay with services unless one of these real pains shows up:

- You want **pipeline behaviors** (logging, validation, auth) applied uniformly to every operation.
- A feature's **read model differs from its write model** (e.g. denormalized reporting view).
- A service grows past **~10 methods** and splitting helpers no longer helps.
- Multiple devs keep stepping on each other in the same service file.

When that happens, a single slice can be migrated to command/query + handler style without touching the others — that's the whole point of VSA. Both patterns can coexist during migration.

Do **not** add MediatR preemptively — it's commercial since v12 and adds indirection with no payoff at our current size.

---

## 13. FAQ

**Q: Can two use cases go into one file?**
Yes — they're methods on the same service file. That's the whole point.

**Q: What if two slices need the same 10-line query?**
Duplicate it — a query stays in each slice that runs it. Only a business rule that appears a third time moves to `Shared/`.

**Q: Can a slice call another slice's service?**
No. Read its tables directly from `LeoHrDbContext`; for a write, call the `Shared/` interface the owning slice implements on a dedicated `<Purpose>Service` (section 7). An interface forwarded to the owner's main service still counts as calling it.

**Q: Where do DTOs returned to the frontend go?**
In `Features/<Feature>/Dto/`. If the same DTO is returned by many slices, move it to `Shared/Dto/`.

**Q: Do I still need the top-level `Services/` folder?**
No. Existing services are being migrated into slices. Don't add new ones there.

**Q: Do I still need `Models/Dto/`?**
No for slice-specific DTOs — those live in the slice's `Dto/` folder. Yes only if a DTO is genuinely cross-slice; prefer `Shared/Dto/` in that case.

**Q: When do we split `Shared/` into `Infrastructure/` + `Shared/`?**
When `Shared/` passes ~15–20 files and the "external adapters vs. passive types" distinction starts mattering. Not yet.

**Q: Why not MediatR?**
Commercial since v12 and adds indirection we don't need at this size. Direct service injection does the job.

**Q: Why not a Repository pattern?**
EF Core's `DbContext` already is a Unit-of-Work + repository. Wrapping it adds boilerplate with no gain.

---

## 14. Summary

- **Features live in `Features/<Feature>/`** — one folder per feature, one service per folder, methods for use cases.
- **Everything cross-feature lives in `Shared/`** — DbContexts, email, auth, the Employee API, owner-write interfaces and passive shared DTOs, all in one place organized by concern.
- **Read across slices, write through the owner.** Other slices' tables are read straight from `LeoHrDbContext`; a write to them goes through the slice that owns them.
- **Controllers are thin. Validation via DataAnnotations. No MediatR. No Repository. No CQRS — yet.**

If you're unsure where something goes, ask: *"If I delete this feature folder, should this file disappear with it?"* If yes → `Features/`. If no → `Shared/`.
