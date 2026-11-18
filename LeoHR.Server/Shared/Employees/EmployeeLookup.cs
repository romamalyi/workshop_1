namespace LeoHR.Server.Shared.Employees;

public class EmployeeLookup(IEmployeeApiClient api) : IEmployeeLookup
{
    public async Task<List<EmployeeRef>> GetAllActiveAsync()
    {
        return [.. (await api.GetAllAsync()).Where(e => e.Active).Select(ToRef)];
    }

    public async Task<EmployeeRef?> GetByIdAsync(string id)
    {
        ApiEmployee? e = await api.GetByIdAsync(id);
        return e is null || !e.Active ? null : ToRef(e);
    }

    // The company ID is the local part of the work email.
    public async Task<EmployeeRef?> GetByCompanyIdAsync(string companyId)
    {
        string emailPrefix = $"{companyId}@";
        ApiEmployee? e = (await api.GetAllAsync())
            .FirstOrDefault(e => e.Active && e.Email.StartsWith(emailPrefix, StringComparison.OrdinalIgnoreCase));
        return e is null ? null : ToRef(e);
    }

    public async Task<List<EmployeeRef>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idSet = ids.ToHashSet();
        return [.. (await api.GetAllAsync()).Where(e => e.Active && idSet.Contains(e.Id)).Select(ToRef)];
    }

    public async Task<Dictionary<string, string>> GetNamesByIdsAsync(IEnumerable<string> ids)
    {
        var idSet = ids.ToHashSet();
        return (await api.GetAllAsync()).Where(e => idSet.Contains(e.Id)).ToDictionary(e => e.Id, e => e.FullName);
    }

    public async Task<List<string>> SearchIdsByNameAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return [];
        string search = searchTerm.Trim().ToLowerInvariant();
        return [.. (await api.GetAllAsync())
            .Where(e => e.Active && e.FullName.ToLowerInvariant().Contains(search))
            .Select(e => e.Id)];
    }

    public async Task<Dictionary<string, bool>> GetActiveFlagsByIdAsync()
    {
        return (await api.GetAllAsync()).ToDictionary(e => e.Id, e => e.Active);
    }

    private static EmployeeRef ToRef(ApiEmployee e)
    {
        return new EmployeeRef(e.Id, e.FirstName, e.LastName, e.Email, e.EnglishLevel, e.CareerStart, e.KeInfo,
            e.Manager, e.HrManager, e.Profile, e.Seniority, e.SenioritySubLevel, e.ManagerId, e.HrManagerId);
    }
}
