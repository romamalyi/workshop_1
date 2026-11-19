# CarriedFromPromotionId means two different things

2026-10-16 · dev-a · verified 2026-10-27

`EmployeeActivityEntity.CarriedFromPromotionId` is set by two writers, and the code comments only describe the first:

- **Carry-over clone.** `PromotionActivitiesService.AddCarryOverAsync` copies surplus points onto the next promotion as a new, always `Approved` row.
- **Adopted pending activity.** `PromotionActivitiesService.MoveOpenActivitiesAsync` moves an employee's `SelfReported` activity from the finished promotion to the new one and sets the flag too, so it shows under "carried" on the promotion page (`PromotionMapper` maps `IsCarriedOver` from the flag).

What follows:

- Reads that filter `CarriedFromPromotionId == null` (the employee's own list in `EmployeeActivitiesService`) also hide adopted activities from the employee.
- To exclude clones, check the status: clones are never `SelfReported` or `Planned`. Don't block write paths on the flag; that blocks adopted activities by mistake.
- If a write turns an adopted activity back into an editable one (reject → Planned), clear the flag.
- `DeleteForDeletedPromotionAsync` deletes rows with the flag set, so deleting a promotion also deletes the activities it adopted. Ask the PO whether that's wanted.

Found on 2026-10-16: the run that followed the "exclude carry-over clones" comments refused to reject an adopted activity; the run that cleared the flag was right.
