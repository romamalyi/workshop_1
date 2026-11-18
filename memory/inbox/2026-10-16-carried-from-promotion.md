# CarriedFromPromotionId means two different things

2026-10-16 · dev-a · verified 2026-10-27, re-checked 2026-11-18

`EmployeeActivityEntity.CarriedFromPromotionId` is set by two writers, and the code comments only describe the first:

- **Carry-over clone.** `PromotionActivitiesService.AddCarryOverAsync` copies surplus points onto the next promotion as a new, always `Approved` row.
- **Adopted pending activity.** `PromotionActivitiesService.AdoptOrphanedAsync` moves an employee's `SelfReported` activity from a finished promotion to the new one and sets the flag too. `PromotionMapper` maps `IsCarriedOver` from the flag, so both kinds show as "carried" on the promotion page.

What follows:

- Reads that filter `CarriedFromPromotionId == null` (the employee's own list in `EmployeeActivitiesService`, `EmployeeActivityData.FindUserActivity`) also hide adopted activities from the employee.
- To exclude clones, check the status: clones are never `SelfReported` or `Planned`. Don't block write paths on the flag; that blocks adopted activities by mistake. `RevertApprovalAsync` currently does block on the flag.
- If a write turns an adopted activity back into an editable one, clear the flag.
- Deleting promotions: `DeleteAllForPromotionsAsync` (leavers only) deletes by `PromotionId`, not by the flag. The FK on `CarriedFromPromotionId` is `SetNull`.
