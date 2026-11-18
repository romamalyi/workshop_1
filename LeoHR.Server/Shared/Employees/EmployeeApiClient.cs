using LeoHR.Server.Shared.FeatureFlags;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace LeoHR.Server.Shared.Employees;

public sealed class EmployeeApiClient(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<EmployeeApiOptions> options,
    IFeatureManager features,
    ILogger<EmployeeApiClient> logger) : IEmployeeApiClient
{
    public const string HttpClientName = "EmployeeApi";
    private const string AllEmployeesCacheKey = "employee-api:all";
    private const string AllEmployeesStaleKey = "employee-api:all:stale";
    private static readonly TimeSpan staleRetention = TimeSpan.FromHours(12);

    private static readonly SemaphoreSlim cacheLock = new(1, 1);
    private readonly EmployeeApiOptions apiOptions = options.Value;
    private readonly HttpClient http = httpClientFactory.CreateClient(HttpClientName);
    private Task<IReadOnlyList<ApiEmployee>>? requestEmployees;

    public async Task<IReadOnlyList<ApiEmployee>> GetAllAsync(CancellationToken ct = default)
    {
        if (!await features.IsEnabledAsync(FeatureFlagNames.EmployeeDataCache))
            return await (requestEmployees ??= FetchAllAsync(ct));

        if (cache.TryGetValue(AllEmployeesCacheKey, out IReadOnlyList<ApiEmployee>? cached) && cached is not null)
            return cached;

        await cacheLock.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(AllEmployeesCacheKey, out cached) && cached is not null)
                return cached;

            try
            {
                return Store(await FetchAllAsync(ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Serve the last good list rather than failing every consumer at once on a transient blip.
                if (cache.TryGetValue(AllEmployeesStaleKey, out IReadOnlyList<ApiEmployee>? stale) && stale is not null)
                {
                    logger.LogWarning(ex, "Employee API fetch failed; serving {Count} stale employees", stale.Count);
                    return stale;
                }

                logger.LogError(ex, "Employee API fetch failed and no cached data is available");
                throw;
            }
        }
        finally
        {
            cacheLock.Release();
        }
    }

    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        await cacheLock.WaitAsync(ct);
        try
        {
            Store(await FetchAllAsync(ct));
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private async Task<IReadOnlyList<ApiEmployee>> FetchAllAsync(CancellationToken ct)
    {
        List<EmployeeJson> raw = await http.GetFromJsonAsync<List<EmployeeJson>>("/employee", ct) ?? [];
        return [.. raw.Where(e => !string.IsNullOrEmpty(e.Id)).Select(Map)];
    }

    private IReadOnlyList<ApiEmployee> Store(IReadOnlyList<ApiEmployee> employees)
    {
        cache.Set(AllEmployeesCacheKey, employees, TimeSpan.FromMinutes(Math.Max(1, apiOptions.CacheExpirationMinutes)));
        cache.Set(AllEmployeesStaleKey, employees, staleRetention);
        logger.LogInformation("Cached {Count} employees from the Employee API", employees.Count);
        return employees;
    }

    public async Task<ApiEmployee?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            return (await GetAllAsync(ct)).FirstOrDefault(e => e.Id == id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An unknown id and an unreachable API are the same to callers here — both yield null.
            logger.LogWarning(ex, "Employee API lookup for {EmployeeId} failed with no cached data", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<EmployeeHistorySnapshot>> GetHistoryAsync(string userId, CancellationToken ct = default)
    {
        string cacheKey = $"employee-api:history:{userId}";
        bool cacheEnabled = await features.IsEnabledAsync(FeatureFlagNames.EmployeeDataCache);
        if (cacheEnabled && cache.TryGetValue(cacheKey, out IReadOnlyList<EmployeeHistorySnapshot>? cached) && cached is not null)
            return cached;

        try
        {
            List<HistoryJson> raw = await http
                .GetFromJsonAsync<List<HistoryJson>>($"/employeehistory/user/{Uri.EscapeDataString(userId)}", ct) ?? [];
            IReadOnlyList<EmployeeHistorySnapshot> snapshots = [.. raw.OrderBy(h => h.Timestamp).Select(Map)];
            if (cacheEnabled)
                cache.Set(cacheKey, snapshots, TimeSpan.FromMinutes(Math.Max(1, apiOptions.CacheExpirationMinutes)));
            return snapshots;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // History is display-only — degrade to empty (also covers a 404 before the endpoint ships).
            // Not cached, so it retries next call.
            logger.LogWarning(ex, "Employee API per-user history fetch failed for {UserId}; treating as no history", userId);
            return [];
        }
    }

    private static ApiEmployee Map(EmployeeJson e)
    {
        (string first, string last) = SplitName(e.FullName ?? string.Empty);
        return new ApiEmployee(
            Id: e.Id!,
            FullName: e.FullName ?? string.Empty,
            FirstName: first,
            LastName: last,
            Email: string.IsNullOrWhiteSpace(e.Email) ? e.Id!.ToLowerInvariant() + "@leobit.com" : e.Email,
            Active: e.Active,
            Employment: e.Employment ?? string.Empty,
            Profile: e.Profile,
            Seniority: e.Seniority,
            SenioritySubLevel: e.SenioritySublevel,
            Location: e.Location,
            Position: e.Position,
            Manager: e.Manager,
            ManagerId: e.ManagerId,
            HrManager: e.HrManager,
            HrManagerId: e.HrManagerId,
            Mentor: e.Mentor,
            EnglishLevel: e.EnglishLevel,
            Birthday: ToDateOnly(e.Birthday),
            StartDate: ToDateOnly(e.StartDate),
            CareerStart: ToDateOnly(e.CareerStart),
            KeInfo: e.KeInfo,
            Level: e.Level,
            Projects: e.Projects ?? []);
    }

    private static EmployeeHistorySnapshot Map(HistoryJson h)
    {
        return new EmployeeHistorySnapshot
        {
            Timestamp = h.Timestamp,
            Active = h.Active,
            Employment = h.Employment,
            Location = h.Location,
            TerminationDate = ToDateOnly(h.TerminationDate),
            Seniority = h.Seniority,
            SenioritySubLevel = h.SenioritySubLevel,
            Profile = h.Profile,
            Position = h.Position,
            Manager = h.Manager,
            HrManager = h.HrManager,
            Mentor = h.Mentor,
            EnglishLevel = h.EnglishLevel,
            Level = h.Level,
            StartDate = ToDateOnly(h.StartDate),
            FullTimeStartDate = ToDateOnly(h.FullTimeStartDate),
        };
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        return value is { } v ? DateOnly.FromDateTime(v) : null;
    }

    private static (string First, string Last) SplitName(string fullName)
    {
        int i = fullName.IndexOf(' ');
        return i >= 0 ? (fullName[..i], fullName[(i + 1)..]) : (fullName, string.Empty);
    }

    private sealed record EmployeeJson(
        string? Id,
        string? FullName,
        string? Email,
        bool Active,
        string? Employment,
        string? Profile,
        string? Seniority,
        string? SenioritySublevel,
        string? Location,
        string? Position,
        string? Manager,
        string? ManagerId,
        string? HrManager,
        string? HrManagerId,
        string? Mentor,
        string? EnglishLevel,
        DateTime? Birthday,
        DateTime? StartDate,
        DateTime? CareerStart,
        string? KeInfo,
        int? Level,
        string[]? Projects);

    private sealed record HistoryJson(
        DateTime Timestamp,
        bool Active,
        string? Employment,
        string? Location,
        DateTime? TerminationDate,
        string? Seniority,
        string? SenioritySubLevel,
        string? Profile,
        string? Position,
        string? Manager,
        string? HrManager,
        string? Mentor,
        string? EnglishLevel,
        int? Level,
        DateTime? StartDate,
        DateTime? FullTimeStartDate);
}
