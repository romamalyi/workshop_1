# Managers are found by Employee API id, not by name

2026-11-05 · dev-c, merged with dev-a's note of 2026-11-08 · IPR-4126

The Employee API returns `managerId` and `hrManagerId`. It matches the manager's name to an employee itself, and leaves the id empty when the name is ambiguous (namesakes), so LeoHR has no "exactly one match" check of its own. LeoHR works with ids only:

- `ManagerId` / `HrManagerId` go `EmployeeJson` → `ApiEmployee` → `EmployeeRef`.
- `EmailService` resolves the HR manager, the project manager and their managers with `IEmployeeLookup.GetByIdAsync` (active people only, so a manager who left resolves to nothing).
- An unresolved role is dropped with a warning (`Email {TemplateId} for employee {EmployeeId} is sent without {Role}`). If no To recipient is left, the send fails with `BusinessRuleError` (400).
- The name lookups (`GetEmailsByFullNamesAsync`, `GetActiveByFullNameAsync`, `FindSingleEmailByName`) are removed. Names are kept only for display, filters and the promotion's `HrManager` snapshot.

Why: with name matching, a renamed manager or a namesake made emails go out without them.

Known gap: a renamed manager is still missed when the HR spreadsheet keeps the old name. Accepted for now.
