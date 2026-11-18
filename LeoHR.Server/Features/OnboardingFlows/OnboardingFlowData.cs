using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingFlows;

internal sealed class OnboardingFlowData(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeApiClient employeeApi,
    ILogger logger)
{
    public IQueryable<OnboardingFlowEntity> QueryWithTasks()
    {
        return db.OnboardingFlows.Include(f => f.Tasks).ThenInclude(t => t.Milestone);
    }

    public async Task<Result<List<OnboardingTaskTemplateEntity>>> LoadTemplatesForType(CreateOnboardingFlowRequest request)
    {
        OnboardingTypeEntity? type = await db.OnboardingTypes
            .AsNoTracking()
            .Include(t => t.Templates)
            .FirstOrDefaultAsync(t => t.Id == request.OnboardingTypeId);
        if (type is null)
        {
            return Result.Fail(new BusinessRuleError(
                "Onboarding type was not found.", nameof(CreateOnboardingFlowRequest.OnboardingTypeId)));
        }

        if (type.IsArchived)
        {
            return Result.Fail(new BusinessRuleError(
                "An archived onboarding type can't be used for a new flow.", nameof(CreateOnboardingFlowRequest.OnboardingTypeId)));
        }

        List<int> enabledIds = [.. type.Templates.Where(t => t.IsEnabled).Select(t => t.TemplateId)];
        List<OnboardingTaskTemplateEntity> available = await db.OnboardingTaskTemplates
            .AsNoTracking()
            .Include(t => t.Fields)
            .Where(t => t.IsActive && enabledIds.Contains(t.Id))
            .ToListAsync();

        HashSet<int> optionalIds = [.. request.OptionalTemplateIds ?? []];
        IEnumerable<int> availableOptionalIds = available.Where(t => t.AppliesTo == TemplateAppliesTo.Optional).Select(t => t.Id);
        if (!optionalIds.IsSubsetOf(availableOptionalIds))
        {
            return Result.Fail(new BusinessRuleError(
                "Optional tasks must be optional templates of the selected type.", nameof(CreateOnboardingFlowRequest.OptionalTemplateIds)));
        }

        return Result.Ok(available
            .Where(t => t.AppliesTo == TemplateAppliesTo.AllHires || optionalIds.Contains(t.Id))
            .ToList());
    }

    public async Task<Dictionary<string, string?>> LoadRoleMappings()
    {
        return await db.ResponsibleRoleMappings
            .AsNoTracking()
            .ToDictionaryAsync(m => m.RoleKey, m => m.DefaultEmployeeId);
    }

    // A default who has left or is no longer in the directory counts as no default, so the task stays unassigned.
    public async Task<Dictionary<string, string?>> LoadAssignableRoleMappings()
    {
        Dictionary<string, string?> mappings = await LoadRoleMappings();
        HashSet<string> activeIds = [.. (await employeeApi.GetAllAsync()).Where(e => e.Active).Select(e => e.Id)];
        return mappings
            .Where(m => m.Value is { } id && activeIds.Contains(id))
            .ToDictionary(m => m.Key, m => m.Value);
    }

    public async Task<Dictionary<string, string>> LoadRoleKeysByPerson()
    {
        IReadOnlyList<ApiEmployee> employees = await employeeApi.GetAllAsync();
        return employees
            .Select(e => (e.Id, Role: OnboardingRoles.MappableRoles.FirstOrDefault(r => PersonRestrictions.Matches(e, r.ExactTitle))))
            .Where(x => x.Role is not null)
            .ToDictionary(x => x.Id, x => x.Role!.Key);
    }

    public async Task<Dictionary<int, OnboardingTaskTemplateFieldEntity>> LoadTemplateFields(IEnumerable<OnboardingFlowTaskFieldEntity> fields)
    {
        List<int> ids = [.. fields.Select(f => f.TemplateFieldId).OfType<int>()];
        return await db.OnboardingTaskTemplateFields
            .AsNoTracking()
            .Include(f => f.ChecklistItems)
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id);
    }

    public async Task<Dictionary<int, OnboardingTypeInfo>> ResolveTypes(IEnumerable<OnboardingFlowEntity> flows)
    {
        List<int> typeIds = [.. flows.Select(f => f.OnboardingTypeId).OfType<int>().Distinct()];
        return await db.OnboardingTypes
            .AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new OnboardingTypeInfo(t.Name, t.IsArchived));
    }

    public async Task<HashSet<int>> ResolveTaskIdsWithFields(IEnumerable<OnboardingFlowEntity> flows)
    {
        List<int> flowIds = [.. flows.Select(f => f.Id)];
        return [.. await db.OnboardingFlowTaskFields
            .AsNoTracking()
            .Where(f => flowIds.Contains(f.Task.FlowId))
            .Select(f => f.TaskId)
            .Distinct()
            .ToListAsync()];
    }

    public async Task<Dictionary<string, string>> ResolveNames(IEnumerable<OnboardingFlowEntity> flows)
    {
        List<OnboardingFlowEntity> flowList = [.. flows];
        return await ResolveEmployeeNames(
        [
            .. flowList.Select(f => f.ResponsiblePersonId),
            .. flowList.SelectMany(f => f.Tasks).Select(t => t.AssigneeId).OfType<string>(),
            .. flowList.Select(f => f.CreatedById).OfType<string>(),
        ]);
    }

    public async Task<Dictionary<string, string>> ResolveEmployeeNames(IEnumerable<string> ids)
    {
        List<string> distinctIds = [.. ids.Distinct()];
        if (distinctIds.Count == 0)
        {
            return [];
        }

        try
        {
            List<EmployeeRef> employees = await employeeLookup.GetByIdsAsync(distinctIds);
            return employees.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not resolve onboarding employee names; showing records without them");
            return [];
        }
    }

    public async Task<FlowWarningContext> LoadWarningContext()
    {
        try
        {
            IReadOnlyList<ApiEmployee> employees = await employeeApi.GetAllAsync();
            var byId = employees.ToDictionary(e => e.Id);
            List<string> allowedPositions = await db.ResponsiblePositions.AsNoTracking().Select(p => p.Position).ToListAsync();

            Dictionary<string, string?> mappings = await LoadRoleMappings();
            List<string> inactiveDefaultRoles =
            [
                .. OnboardingRoles.MappableRoles
                    .Where(r => mappings.GetValueOrDefault(r.Key) is { } id && !(byId.TryGetValue(id, out ApiEmployee? e) && e.Active))
                    .Select(r => r.ExactTitle)
            ];

            return new FlowWarningContext(byId, allowedPositions, inactiveDefaultRoles);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read the employee directory; showing flows without warnings");
            return new FlowWarningContext(new Dictionary<string, ApiEmployee>(), [], []);
        }
    }

    public async Task<List<RecheckMatch>> FindRecheckMatches(IQueryable<OnboardingFlowTaskEntity> tasks)
    {
        Dictionary<string, string?> roleMappings = await LoadAssignableRoleMappings();
        Dictionary<string, string> roleKeysByPerson = await LoadRoleKeysByPerson();
        List<OnboardingFlowTaskEntity> unassigned = await tasks
            .Include(t => t.Flow)
            .Where(t => t.AssigneeId == null && t.DoneAt == null)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        List<RecheckMatch> matches = [];
        foreach (OnboardingFlowTaskEntity task in unassigned)
        {
            string? assigneeId = FlowTaskGeneration.ResolveAssignee(task.ResponsibleRoleKey, task.Flow.ResponsiblePersonId, roleKeysByPerson, roleMappings);
            if (assigneeId is not null)
            {
                matches.Add(new RecheckMatch(task, assigneeId));
            }
        }

        return matches;
    }
}

internal sealed record RecheckMatch(OnboardingFlowTaskEntity Task, string AssigneeId);
