---
paths:
  - "LeoHR.Server/**"
  - "LeoHR.Server.IntegrationTests/**"
---

# LeoHR — Backend Architecture Rules

Backend is **Vertical Slice Architecture** with plain controllers and **one service per feature**. No MediatR, no FluentValidation, no CQRS, no repository pattern. Full rationale and examples: `LeoHR.Server/README.md`.

```
LeoHR.Server/
  Features/<Feature>/   ← <Feature>Controller, I<Feature>Service, <Feature>Service, Dto/
  Shared/               ← cross-feature services (DbContexts, auth, email, toggles) and passive types (DTOs, enums)
  Models/Entities/      ← EF Core entities (shared across slices)
  Program.cs
```

1. **One feature = one folder.** A new use case is a new method on the feature's service, not a new class. Exception: a business rule that must be tested on its own and is shared by several service methods may be a small scoped class in the same feature folder.
2. **Thin controllers:** one action = one service call, no business logic. Validate DTOs with `DataAnnotations`.
3. **Slices never reference each other** — directly or via a `Shared/` interface resolved to another slice's main service. Read other slices' tables from `LeoHrDbContext` (`.AsNoTracking()`); write only your own entities; a write to another slice goes through a `Shared/` interface on a dedicated `<Purpose>Service` in the owning slice (e.g. `IPromotionActivities` → `PromotionActivitiesService`). Only exception: derived state — call the owner's service interface read-only, one direction, no cycles (e.g. `OnboardingFlowMapper.IsCompleted`). Fix a violation in the MR that meets it.
4. **`Shared/`** holds cross-cutting infra and types used by 2+ slices. Entities live in `Models/Entities/`.
5. **Register in `Program.cs`** as `AddScoped<IXxx, XxxService>()`. Never forward a `Shared/` interface with `GetRequiredService<…Service>()`.
6. **Results, not exceptions:** services return `Result`/`Result<T>`; controllers call `.ToActionResult()`.
7. **Audited entities inherit `BaseAuditableEntity`**; never set its fields by hand.
8. **Comments:** one line for a non-obvious *why*. Never describe UI behaviour.
9. **HR-editable seed data** goes in raw SQL in a migration's `Up()`, not `HasData` (a later seed change overwrites HR's edits). `HasData` is only for read-only lists.
