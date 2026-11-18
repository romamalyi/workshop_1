namespace LeoHR.Server.Shared.Employees;

public static class EmployeeRefExtensions
{
    // No HR managers given means no HR filter.
    public static IEnumerable<EmployeeRef> WithHrManagers(this IEnumerable<EmployeeRef> employees, List<string>? hrManagers)
    {
        return hrManagers is { Count: > 0 }
            ? employees.Where(e => e.HrManager != null && hrManagers.Contains(e.HrManager))
            : employees;
    }
}
