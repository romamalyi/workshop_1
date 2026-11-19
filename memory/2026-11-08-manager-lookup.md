# Manager emails: who gets resolved

2026-11-08 · dev-a

Since IPR-4126, `EmailService` finds the HR manager and the project manager by id: `IEmployeeLookup.GetByIdAsync(employee.HrManagerId / ManagerId)`.

- `GetByIdAsync` reads the full Employee API list, which includes former employees, so a manager who has left still resolves and still gets the email. History screens need leavers, so don't filter on `Active` inside the lookup; a caller that must skip leavers checks `Active` itself.
- `GetEmailsByFullNamesAsync` and `FindSingleEmailByName` are gone.
- Namesakes: the Employee API leaves the manager id empty when the name is ambiguous, so LeoHR no longer has its own "exactly one match" check.
- An unresolved role is dropped with a warning (`Email {TemplateId} for employee {EmployeeId} is sent without {Role}`). If no To recipient is left, the send fails with 400.

Why: with name matching, a renamed manager or a namesake made emails go out without them.
