namespace LeoHR.Server.Shared.Employees;

/// Implemented by Features/Employees/EmployeeLevelStartService. Saves the change.
public interface IEmployeeLevelStart
{
    Task SetLevelStartDateAsync(string employeeId, DateTime date);
}
