using FluentResults;

using LeoHR.Server.Features.Employees.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Models.Filter;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Employees;

public interface IEmployeesService
{
    Task<List<EmployeeDto>> GetActive();
    Task<PagedResult<EmployeeDto>> Search(EmployeeFilterModel filter);
    Task<EmployeeFilterOptionsDto> GetFilterOptions();
    Task<EmployeeDto?> GetById(string id);
    Task<List<EmployeeHistoryEventDto>> GetHistory(string userId, string? viewerId);
    Task<List<EmployeeProjectsDto>> GetProjectsForUsers(IList<string> userIds);
    Task<List<EmployeeDto>> GetByIds(IList<string> ids);
    Task<Result<List<string>>> GetLevels();
    Task<Result> UpdatePdpUrl(string employeeId, string? pdpUrl);
}

public class EmployeesService(IEmployeeApiClient api, LeoHrDbContext leoHrDb) : IEmployeesService
{
    public async Task<List<EmployeeDto>> GetActive()
    {
        IReadOnlyList<ApiEmployee> all = await api.GetAllAsync();
        return [.. all
            .Where(e => e.Active)
            .OrderBy(e => e.FullName, StringComparer.InvariantCultureIgnoreCase)
            .Select(e => ToDto(e, levelStartDate: null))];
    }

    public async Task<PagedResult<EmployeeDto>> Search(EmployeeFilterModel filter)
    {
        IReadOnlyList<ApiEmployee> all = await api.GetAllAsync();

        IEnumerable<ApiEmployee> matching = all.Where(e => e.Active);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim();
            matching = matching.Where(e => e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Seniorities is { Count: > 0 } seniorities)
            matching = matching.Where(e => e.Seniority != null && seniorities.Contains(e.Seniority));

        if (filter.Profiles is { Count: > 0 } profiles)
            matching = matching.Where(e => e.Profile != null && profiles.Contains(e.Profile));

        if (filter.Projects is { Count: > 0 } projects)
            matching = matching.Where(e => e.Projects.Any(projects.Contains));

        if (filter.HrManagers is { Count: > 0 } hrManagers)
            matching = matching.Where(e => e.HrManager != null && hrManagers.Contains(e.HrManager));

        List<ApiEmployee> ordered = [.. matching.OrderBy(e => e.FullName, StringComparer.InvariantCultureIgnoreCase)];

        // pageSize absent/non-positive means "no pagination requested" (return everything); once a
        // pageSize is given, an absent/non-positive pageNumber defaults to page 1 rather than also
        // falling back to the unpaged list.
        int pageSize = filter.PageSize ?? 0;
        int pageNumber = filter.PageNumber is > 0 ? filter.PageNumber.Value : 1;
        IEnumerable<ApiEmployee> page = pageSize > 0
            ? ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            : ordered;

        List<ApiEmployee> pageList = [.. page];
        List<string> pageIds = [.. pageList.Select(e => e.Id)];
        Dictionary<string, EmployeeExtensionEntity> extensions = await leoHrDb.EmployeeExtensions
            .Where(x => pageIds.Contains(x.EmployeeId))
            .ToDictionaryAsync(x => x.EmployeeId);

        List<EmployeeDto> items = [.. pageList.Select(e =>
        {
            extensions.TryGetValue(e.Id, out EmployeeExtensionEntity? ext);
            return ToDto(e, ext?.LevelStartDate, ext?.PdpUrl);
        })];
        return new PagedResult<EmployeeDto>(items, ordered.Count);
    }

    public async Task<EmployeeFilterOptionsDto> GetFilterOptions()
    {
        IReadOnlyList<ApiEmployee> active = [.. (await api.GetAllAsync()).Where(e => e.Active)];

        return new EmployeeFilterOptionsDto(
            DistinctSorted(active.Select(e => e.Seniority)),
            DistinctSorted(active.Select(e => e.Profile)),
            DistinctSorted(active.SelectMany(e => e.Projects)),
            DistinctSorted(active.Select(e => e.HrManager)));
    }

    public async Task<EmployeeDto?> GetById(string id)
    {
        ApiEmployee? emp = await api.GetByIdAsync(id);
        if (emp is null || !emp.Active) return null;

        EmployeeExtensionEntity? extension = await leoHrDb.EmployeeExtensions.FindAsync(id);
        return ToDto(emp, extension?.LevelStartDate, extension?.PdpUrl);
    }

    public async Task<List<EmployeeHistoryEventDto>> GetHistory(string userId, string? viewerId)
    {
        // Independent reads (Employee API vs LeoHR DB) — run them together.
        Task<IReadOnlyList<EmployeeHistorySnapshot>> snapshotsTask = api.GetHistoryAsync(userId);
        IReadOnlyList<CompletedPromotion> leoHrPromotions = await GetCompletedPromotions(userId);
        IReadOnlyList<EmployeeHistorySnapshot> snapshots = await snapshotsTask;

        if (snapshots.Count > 0)
            return EmployeeHistoryAnalyzer.Analyze([.. snapshots], leoHrPromotions, await GetHistoryOptions(viewerId));

        // No API history: synthesise "Joined" from StartDate — not CareerStart, which is the start of
        // the person's IT career and predates Leobit by years — then append the LeoHR promotions.
        ApiEmployee? emp = await api.GetByIdAsync(userId);
        List<EmployeeHistoryEventDto> events = [.. leoHrPromotions.Select(EmployeeHistoryAnalyzer.PromotionEvent)];
        if (emp?.StartDate is { } start)
            events.Add(new EmployeeHistoryEventDto("Joined the company", "joined", start.ToDateTime(TimeOnly.MinValue)));
        return [.. events.OrderByDescending(e => e.Date)];
    }

    private async Task<HistoryOptions> GetHistoryOptions(string? viewerId)
    {
        if (viewerId is null) return new HistoryOptions();

        AppSettingsEntity settings = await leoHrDb.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == viewerId) ?? new AppSettingsEntity { UserId = viewerId };
        return new HistoryOptions
        {
            ShowManager = settings.ShowManager,
            ShowMentor = settings.ShowMentor,
            ShowEmployment = settings.ShowEmployment,
        };
    }

    // Same "Sent means completed" rule as PromotionsService.
    private async Task<IReadOnlyList<CompletedPromotion>> GetCompletedPromotions(string employeeId)
    {
        return await leoHrDb.Promotions
            .Where(p => p.EmployeeId == employeeId
                && p.Status == PromotionStatus.Sent)
            .OrderBy(p => p.ReferenceDate ?? p.CreatedAt)
            .Select(p => new CompletedPromotion(p.ReferenceDate ?? p.CreatedAt, p.FromRole, p.ToRole, p.HrManager))
            .ToListAsync();
    }

    public async Task<List<EmployeeProjectsDto>> GetProjectsForUsers(IList<string> userIds)
    {
        IReadOnlyList<ApiEmployee> all = await api.GetAllAsync();
        return [.. all
            .Where(e => userIds.Contains(e.Id) && e.Projects.Count > 0)
            .Select(e => new EmployeeProjectsDto(e.Id, [.. e.Projects]))];
    }

    public async Task<List<EmployeeDto>> GetByIds(IList<string> ids)
    {
        IReadOnlyList<ApiEmployee> all = await api.GetAllAsync();

        Dictionary<string, EmployeeExtensionEntity> extensions = await leoHrDb.EmployeeExtensions
            .Where(x => ids.Contains(x.EmployeeId))
            .ToDictionaryAsync(x => x.EmployeeId);

        return [.. all
            .Where(e => ids.Contains(e.Id) && e.Active)
        .Select(e => {
            extensions.TryGetValue(e.Id, out EmployeeExtensionEntity? ext);
            return ToDto(e, ext?.LevelStartDate, ext?.PdpUrl);
        })];
    }

    public async Task<Result<List<string>>> GetLevels()
    {
        IReadOnlyList<ApiEmployee> all = await api.GetAllAsync();
        List<string> levels = [.. all
            .Select(e => e.SenioritySubLevel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()];
        return Result.Ok(levels);
    }

    public async Task<Result> UpdatePdpUrl(string employeeId, string? pdpUrl)
    {
        EmployeeExtensionEntity extension = await leoHrDb.EmployeeExtensions.GetOrAddAsync(employeeId);
        extension.PdpUrl = string.IsNullOrWhiteSpace(pdpUrl) ? null : pdpUrl.Trim();
        await leoHrDb.SaveChangesAsync();
        return Result.Ok();
    }

    private static EmployeeDto ToDto(ApiEmployee e, DateTime? levelStartDate, string? pdpUrl = null)
    {
        // No promotion completed in LeoHR yet: the current level started when the employee joined.
        levelStartDate ??= e.StartDate?.ToDateTime(TimeOnly.MinValue);
        return new EmployeeDto(e.Id, e.FirstName, e.LastName, e.Email, e.Profile, e.Seniority, e.SenioritySubLevel,
            e.Location, e.Employment, e.EnglishLevel, e.Birthday, e.StartDate, e.CareerStart, e.Manager, e.HrManager,
            levelStartDate, [.. e.Projects], pdpUrl);
    }

    private static List<string> DistinctSorted(IEnumerable<string?> values)
    {
        return [.. values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).Distinct().OrderBy(v => v)];
    }
}
