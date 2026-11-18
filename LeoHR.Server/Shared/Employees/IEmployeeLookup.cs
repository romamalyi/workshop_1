namespace LeoHR.Server.Shared.Employees;

/// Implemented by Shared/Employees/EmployeeLookup.
public interface IEmployeeLookup
{
    Task<List<EmployeeRef>> GetAllActiveAsync();
    Task<EmployeeRef?> GetByIdAsync(string id);
    Task<EmployeeRef?> GetByCompanyIdAsync(string companyId);
    Task<List<EmployeeRef>> GetByIdsAsync(IEnumerable<string> ids);
    // Includes former employees, so names recorded in history still resolve.
    Task<Dictionary<string, string>> GetNamesByIdsAsync(IEnumerable<string> ids);
    Task<List<string>> SearchIdsByNameAsync(string searchTerm);
    // Every employee the Employee API returns, former ones included; an absent id is unknown, not a leaver.
    Task<Dictionary<string, bool>> GetActiveFlagsByIdAsync();
}
