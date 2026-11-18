namespace LeoHR.Server.Shared.Employees;

/// <summary>
/// Read access to the Employee API (<c>api.employee.leobit.co</c>). Replaces the former direct
/// <c>TimeTrackingDbContext</c> connection. The full employee list is cached; history is fetched
/// per user.
/// </summary>
public interface IEmployeeApiClient
{
    /// <summary>
    /// Every employee (active and inactive), from a cached <c>GET /employee</c>. On a fetch failure
    /// the last successfully cached list is served if one exists (stale, up to 12h); only a cold
    /// failure with no cached data throws.
    /// </summary>
    Task<IReadOnlyList<ApiEmployee>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Re-fetches the employee list into the cache even when the cached copy is still valid.</summary>
    /// <remarks>Throws on failure and leaves the current cached list in place.</remarks>
    Task RefreshAllAsync(CancellationToken ct = default);

    /// <summary>
    /// One employee by id, read from the same cached list as <see cref="GetAllAsync"/> (no extra
    /// request). Null when the id is unknown or the API is unavailable with no cached data.
    /// </summary>
    Task<ApiEmployee?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Raw history snapshots for one employee, oldest first, from
    /// <c>GET /employeehistory/user/{userId}</c>, cached per user. Empty when the employee has no
    /// history, when that endpoint is not yet deployed (404), or on a transient upstream error —
    /// never throws.
    /// </summary>
    Task<IReadOnlyList<EmployeeHistorySnapshot>> GetHistoryAsync(string userId, CancellationToken ct = default);
}

/// <summary>An employee as consumed inside LeoHR. <see cref="FullName"/> comes straight from the API;
/// <see cref="FirstName"/>/<see cref="LastName"/> are split on the first space.</summary>
public sealed record ApiEmployee(
    string Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    bool Active,
    string Employment,
    string? Profile,
    string? Seniority,
    string? SenioritySubLevel,
    string? Location,
    string? Position,
    string? Manager,
    string? ManagerId,
    string? HrManager,
    string? HrManagerId,
    string? Mentor,
    string? EnglishLevel,
    DateOnly? Birthday,
    DateOnly? StartDate,
    DateOnly? CareerStart,
    string? KeInfo,
    int? Level,
    IReadOnlyList<string> Projects);
