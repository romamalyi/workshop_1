using System.Security.Claims;

using LeoHR.Server.Shared.Extensions.Authentication;

namespace LeoHR.Server.Shared.Employees;

public static class ManagementLineExtensions
{
    public static async Task<bool> IsInManagementLineAsync(
        this IEmployeeLookup employeeLookup, string employeeId, ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return false;
        if (employeeId == userId) return true;

        EmployeeRef? employee = await employeeLookup.GetByIdAsync(employeeId);
        if (employee?.ManagerId is not { Length: > 0 } managerId) return false;
        if (managerId == userId) return true;

        EmployeeRef? manager = await employeeLookup.GetByIdAsync(managerId);
        return manager?.ManagerId == userId;
    }
}
