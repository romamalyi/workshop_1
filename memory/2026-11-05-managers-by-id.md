# Managers are found by Employee API id, not by name

2026-11-05 · dev-c

The Employee API now returns `managerId` and `hrManagerId`. It matches the manager's name to an employee itself, and leaves the id empty when the name is ambiguous. LeoHR works with ids only:

- `ManagerId` / `HrManagerId` go `EmployeeJson` → `ApiEmployee` → `EmployeeRef`.
- `EmailService` resolves the HR manager and the project manager with `IEmployeeLookup.GetByIdAsync`, which returns active people only. No id, or a manager who has left → that recipient is skipped with a warning.
- The name lookups `GetEmailsByFullNamesAsync` and `GetActiveByFullNameAsync` are removed. Names are kept only for display, filters and the promotion's `HrManager` snapshot.

Known gap: a renamed manager is still missed, because the HR spreadsheet keeps the old name. Accepted for now.

IPR-4126.
