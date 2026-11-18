using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Features.OnboardingFlows.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingFlows;

public class OnboardingFlowsService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeApiClient employeeApi,
    IOnboardingCandidates candidateLookup,
    ILogger<OnboardingFlowsService> logger) : IOnboardingFlowsService
{
    private const string ResourceName = "Onboarding flow";
    private const string TaskResourceName = OnboardingTaskQueries.TaskResourceName;
    private const int StartDateWindowYears = 1;
    private const string StartDateOutOfRangeMessage = "Start date must be within one year of today.";

    private readonly OnboardingFlowData data = new(db, employeeLookup, employeeApi, logger);
    private readonly OnboardingTaskQueries taskQueries = new(db, employeeLookup, employeeApi, logger);
    private readonly TaskActivityLog activityLog = new(db);

    public async Task<List<OnboardingFlowDto>> GetAll()
    {
        List<OnboardingFlowEntity> flows = await data.QueryWithTasks()
            .AsNoTracking()
            .OrderByDescending(f => f.StartDate)
            .ToListAsync();

        Dictionary<string, string> names = await data.ResolveNames(flows);
        Dictionary<int, OnboardingTypeInfo> types = await data.ResolveTypes(flows);
        FlowWarningContext warningContext = await data.LoadWarningContext();
        HashSet<int> tasksWithFields = await data.ResolveTaskIdsWithFields(flows);
        return [.. flows.Select(f => OnboardingFlowMapper.MapToDto(f, names, warningContext, types, tasksWithFields))];
    }

    public async Task<Result<OnboardingFlowDto>> GetById(int id)
    {
        OnboardingFlowEntity? flow = await data.QueryWithTasks().AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (flow is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        Dictionary<string, string> names = await data.ResolveNames([flow]);
        Dictionary<int, OnboardingTypeInfo> types = await data.ResolveTypes([flow]);
        FlowWarningContext warningContext = await data.LoadWarningContext();
        HashSet<int> tasksWithFields = await data.ResolveTaskIdsWithFields([flow]);
        return Result.Ok(OnboardingFlowMapper.MapToDto(flow, names, warningContext, types, tasksWithFields));
    }

    public async Task<Result> ChangeResponsible(int id, ChangeResponsibleRequest request)
    {
        OnboardingFlowEntity? flow = await db.OnboardingFlows.Include(f => f.Tasks).FirstOrDefaultAsync(f => f.Id == id);
        if (flow is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        if (OnboardingFlowMapper.IsCompleted(flow))
        {
            return Result.Fail(new BusinessRuleError("The responsible person of a completed flow can't be changed."));
        }

        List<ResponsibleCandidateDto> allowed = await candidateLookup.GetResponsibleCandidates();
        ResponsibleCandidateDto? chosen = allowed.FirstOrDefault(c => c.Id == request.ResponsiblePersonId);
        if (chosen is null
            || chosen.Id == flow.ResponsiblePersonId
            || string.Equals(chosen.FullName, flow.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(new BusinessRuleError(
                "Responsible person must hold one of the allowed positions, differ from the current one and cannot be the new hire.",
                nameof(ChangeResponsibleRequest.ResponsiblePersonId)));
        }

        if (request.ReassignNotStartedTasks)
        {
            Dictionary<string, string> roleKeysByPerson = await data.LoadRoleKeysByPerson();
            string? newRoleKey = roleKeysByPerson.GetValueOrDefault(chosen.Id);

            foreach (OnboardingFlowTaskEntity task in flow.Tasks.Where(t =>
                newRoleKey is not null
                && t.ResponsibleRoleKey == newRoleKey
                && t.AssigneeId != chosen.Id
                && t.StartedAt is null
                && t.DoneAt is null))
            {
                activityLog.RecordAssigneeChange(task, chosen.Id);
                task.AssigneeId = chosen.Id;
            }
        }

        flow.ResponsiblePersonId = chosen.Id;
        flow.ResponsiblePosition = chosen.Position;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<OnboardingFlowDto>> CreateFlow(CreateOnboardingFlowRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.StartDate < today.AddYears(-StartDateWindowYears) || request.StartDate > today.AddYears(StartDateWindowYears))
        {
            return Result.Fail(new BusinessRuleError(
                StartDateOutOfRangeMessage, nameof(CreateOnboardingFlowRequest.StartDate)));
        }

        EmployeeRef? responsiblePerson = await employeeLookup.GetByIdAsync(request.ResponsiblePersonId);
        if (responsiblePerson is null)
        {
            return Result.Fail(new BusinessRuleError(
                "Responsible person was not found.", nameof(CreateOnboardingFlowRequest.ResponsiblePersonId)));
        }

        List<ResponsibleCandidateDto> allowedResponsible = await candidateLookup.GetResponsibleCandidates();
        ResponsibleCandidateDto? chosen = allowedResponsible.FirstOrDefault(c => c.Id == request.ResponsiblePersonId);
        bool isNewHire = chosen is not null
            && string.Equals(chosen.FullName, request.FullName.Trim(), StringComparison.OrdinalIgnoreCase);
        if (chosen is null || isNewHire)
        {
            return Result.Fail(new BusinessRuleError(
                "Responsible person must hold one of the allowed positions and cannot be the new hire.", nameof(CreateOnboardingFlowRequest.ResponsiblePersonId)));
        }

        Result<List<OnboardingTaskTemplateEntity>> selected = await data.LoadTemplatesForType(request);
        if (selected.IsFailed)
        {
            return selected.ToResult<OnboardingFlowDto>();
        }

        List<OnboardingTaskTemplateEntity> templates = selected.Value;

        Dictionary<string, string?> roleMappings = await data.LoadAssignableRoleMappings();
        Dictionary<string, string> roleKeysByPerson = await data.LoadRoleKeysByPerson();

        OnboardingSettingsEntity settings = await db.OnboardingSettings.AsNoTracking().SingleOrDefaultAsync()
            ?? new OnboardingSettingsEntity();

        var flow = new OnboardingFlowEntity
        {
            FullName = request.FullName.Trim(),
            Position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim(),
            EmploymentType = request.EmploymentType,
            StartDate = request.StartDate,
            Level = request.Level,
            ProbationMonths = settings.ProbationMonths,
            ResponsiblePersonId = request.ResponsiblePersonId,
            ResponsiblePosition = chosen.Position,
            OnboardingTypeId = request.OnboardingTypeId,
        };

        Result tasksAdded = FlowTaskGeneration.AddTasks(flow, templates, roleKeysByPerson, roleMappings);
        if (tasksAdded.IsFailed)
        {
            return tasksAdded.ToResult<OnboardingFlowDto>();
        }

        if (flow.Tasks.Count == 0)
        {
            return Result.Fail(new BusinessRuleError(
                "A flow needs at least one task.", nameof(CreateOnboardingFlowRequest.OptionalTemplateIds)));
        }

        db.OnboardingFlows.Add(flow);
        await db.SaveChangesAsync();

        // Re-read so each task carries its milestone, which the new entities only reference by id.
        OnboardingFlowEntity saved = await data.QueryWithTasks().AsNoTracking().FirstAsync(f => f.Id == flow.Id);
        Dictionary<string, string> names = await data.ResolveNames([saved]);
        Dictionary<int, OnboardingTypeInfo> types = await data.ResolveTypes([saved]);
        FlowWarningContext warningContext = await data.LoadWarningContext();
        HashSet<int> tasksWithFields = await data.ResolveTaskIdsWithFields([saved]);
        return Result.Ok(OnboardingFlowMapper.MapToDto(saved, names, warningContext, types, tasksWithFields));
    }

    public async Task<List<UnassignedTaskDto>> GetUnassignedTasks()
    {
        List<OnboardingFlowTaskEntity> tasks = await db.OnboardingFlowTasks
            .AsNoTracking()
            .Include(t => t.Flow)
            .Where(t => t.AssigneeId == null && t.DoneAt == null)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Flow.FullName)
            .ToListAsync();

        return [.. tasks.Select(t => new UnassignedTaskDto(
            TaskId: t.Id,
            Title: t.Title,
            Phase: t.Phase,
            ResponsibleRoleKey: t.ResponsibleRoleKey,
            ResponsibleRoleName: OnboardingRoles.Find(t.ResponsibleRoleKey)?.ExactTitle,
            DueDate: t.DueDate,
            FlowId: t.FlowId,
            FlowFullName: t.Flow.FullName))];
    }

    public async Task<Result> AssignTask(int taskId, AssignTaskRequest request, ClaimsPrincipal user)
    {
        OnboardingFlowTaskEntity? task = await db.OnboardingFlowTasks
            .Include(t => t.Flow)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
        {
            return Result.Fail(new NotFoundError(TaskResourceName, taskId));
        }

        if (task.DoneAt is not null)
        {
            return Result.Fail(new BusinessRuleError("Completed tasks can't be reassigned."));
        }

        if (!TaskAccess.CanChangeAssignee(task, user))
        {
            return Result.Fail(new ForbiddenError("Only the flow's responsible person or an Admin can change the assignee."));
        }

        if (task.AssigneeId == request.AssigneeId)
        {
            return Result.Ok();
        }

        List<EmployeeCandidateDto> candidates = await candidateLookup.GetAssigneeCandidates(task.ResponsibleRoleKey);
        if (!candidates.Any(c => c.Id == request.AssigneeId))
        {
            return Result.Fail(new BusinessRuleError(
                "The assignee must hold the task's role.", nameof(AssignTaskRequest.AssigneeId)));
        }

        activityLog.RecordAssigneeChange(task, request.AssigneeId);
        task.AssigneeId = request.AssigneeId;
        task.StartedAt = null;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<bool> HasAssignedTasks(ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        return userId is not null && await db.OnboardingFlowTasks.AnyAsync(t => t.AssigneeId == userId);
    }

    public async Task<Result<List<EmployeeCandidateDto>>> GetPersonFieldCandidates(string? restrictionKey, ClaimsPrincipal user)
    {
        // Without the claim, the list is only needed to fill a person field on one's own unfinished task.
        string? userId = user.GetUserId();
        bool canPick = user.CanReadOnboarding()
            || (userId is not null && await db.OnboardingFlowTasks.AnyAsync(t =>
                t.AssigneeId == userId
                && t.DoneAt == null
                && t.Fields.Any(f => f.FieldType == TemplateFieldType.EmployeePicker)));
        if (!canPick)
        {
            return Result.Fail(new ForbiddenError("You have no task that needs a person picked."));
        }

        return Result.Ok(await candidateLookup.GetPersonFieldCandidates(restrictionKey));
    }

    public async Task<PagedResult<OnboardingTaskFlowDto>> GetMyTasks(OnboardingTaskFilterModel filter, ClaimsPrincipal user)
    {
        IQueryable<OnboardingFlowTaskEntity>? visible = await taskQueries.QueryVisibleTasks(filter, user);
        if (visible is null)
        {
            return new PagedResult<OnboardingTaskFlowDto>([], 0);
        }

        string? userId = user.GetUserId();
        IQueryable<OnboardingFlowTaskEntity> query = filter.Scope == OnboardingTaskScope.Mine
            ? visible.Where(t => userId != null && t.AssigneeId == userId)
            : visible;

        return await taskQueries.LoadPage(query, filter, user);
    }

    public async Task<OnboardingTasksSummaryDto> GetMyTasksSummary(OnboardingTaskFilterModel filter, ClaimsPrincipal user)
    {
        IQueryable<OnboardingFlowTaskEntity>? visible = await taskQueries.QueryVisibleTasks(filter, user);
        if (visible is null)
        {
            return new OnboardingTasksSummaryDto(0, 0, 0, 0, 0, 0, 0);
        }

        string? userId = user.GetUserId();
        var rows = await visible
            .Select(t => new { t.FlowId, t.DueDate, IsDone = t.DoneAt != null, IsMine = userId != null && t.AssigneeId == userId })
            .ToListAsync();

        var mine = rows.Where(r => r.IsMine).ToList();
        var scoped = filter.Scope == OnboardingTaskScope.Mine ? mine : rows;
        DateOnly today = OnboardingTaskQueries.TodayFor(filter);
        return new OnboardingTasksSummaryDto(
            TaskCount: scoped.Count,
            FlowCount: scoped.Select(r => r.FlowId).Distinct().Count(),
            MineCount: scoped.Count(r => r.IsMine),
            OverdueCount: scoped.Count(r => !r.IsDone && r.DueDate < today),
            DueTodayCount: scoped.Count(r => !r.IsDone && r.DueDate == today),
            AllScopeCount: rows.Count,
            MineScopeCount: mine.Count);
    }

    public async Task<Result<OnboardingTaskDetailDto>> GetTask(int taskId, ClaimsPrincipal user)
    {
        OnboardingFlowTaskEntity? task = await db.OnboardingFlowTasks
            .AsNoTracking()
            .Include(t => t.Flow)
            .Include(t => t.Milestone)
            .Include(t => t.Fields.OrderBy(f => f.SortOrder))
                .ThenInclude(f => f.CheckedItems)
            .Include(t => t.Activities.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id))
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
        {
            return Result.Fail(new NotFoundError(TaskResourceName, taskId));
        }

        if (!TaskAccess.CanAccess(task, user))
        {
            return Result.Fail(new ForbiddenError("You don't have access to this task."));
        }

        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields = await data.LoadTemplateFields(task.Fields);
        Dictionary<string, string> names = await data.ResolveEmployeeNames(task.Fields
            .Select(f => f.PersonId)
            .Append(task.AssigneeId)
            .Concat(task.Activities.SelectMany(a => new[] { a.FromAssigneeId, a.ToAssigneeId, a.CreatedById }))
            .OfType<string>());

        return Result.Ok(OnboardingFlowMapper.MapTaskDetail(task, user, templateFields, names));
    }

    public async Task<Result> UpdateTaskFields(int taskId, UpdateTaskFieldsRequest request, ClaimsPrincipal user)
    {
        Result<OnboardingFlowTaskEntity> loaded = await taskQueries.LoadTaskForEdit(taskId, user);
        if (loaded.IsFailed)
        {
            return loaded.ToResult();
        }

        OnboardingFlowTaskEntity task = loaded.Value;
        if (task.DoneAt is not null)
        {
            return Result.Fail(new BusinessRuleError("Reopen the task to change its fields."));
        }

        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields = await data.LoadTemplateFields(task.Fields);
        OnboardingFlowTaskActivityEntity? latest = await db.OnboardingFlowTaskActivities
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        foreach (TaskFieldValueInput input in request.Fields)
        {
            OnboardingFlowTaskFieldEntity? field = task.Fields.FirstOrDefault(f => f.Id == input.FieldId);
            if (field is null)
            {
                return Result.Fail(new NotFoundError("Task field", input.FieldId));
            }

            Result<List<TaskFieldChange>> applied = await TaskFieldValues.Apply(field, input, templateFields, employeeApi);
            if (applied.IsFailed)
            {
                return applied.ToResult();
            }

            foreach (TaskFieldChange change in applied.Value)
            {
                if (!TaskActivityLog.ContinuesFieldEdit(latest, change, field, user))
                {
                    activityLog.RecordActivity(task, change.Kind, field.Label, change.ChecklistItemText);
                }
            }
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> StartTask(int taskId, ClaimsPrincipal user)
    {
        Result<OnboardingFlowTaskEntity> loaded = await taskQueries.LoadTaskForEdit(taskId, user);
        if (loaded.IsFailed)
        {
            return loaded.ToResult();
        }

        OnboardingFlowTaskEntity task = loaded.Value;
        if (task.DoneAt is not null)
        {
            return Result.Fail(new BusinessRuleError("A completed task can't be started."));
        }

        if (task.StartedAt is null)
        {
            task.StartedAt = DateTime.UtcNow;
            activityLog.RecordActivity(task, OnboardingTaskActivityKind.Started);
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UndoStartTask(int taskId, ClaimsPrincipal user)
    {
        Result<OnboardingFlowTaskEntity> loaded = await taskQueries.LoadTaskForEdit(taskId, user);
        if (loaded.IsFailed)
        {
            return loaded.ToResult();
        }

        OnboardingFlowTaskEntity task = loaded.Value;
        if (task.DoneAt is not null)
        {
            return Result.Fail(new BusinessRuleError("A completed task can't be un-started."));
        }

        if (task.StartedAt is not null)
        {
            task.StartedAt = null;
            activityLog.RecordActivity(task, OnboardingTaskActivityKind.StartUndone);
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> CompleteTask(int taskId, ClaimsPrincipal user)
    {
        Result<OnboardingFlowTaskEntity> loaded = await taskQueries.LoadTaskForEdit(taskId, user);
        if (loaded.IsFailed)
        {
            return loaded.ToResult();
        }

        OnboardingFlowTaskEntity task = loaded.Value;
        if (task.DoneAt is not null)
        {
            return Result.Ok();
        }

        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields = await data.LoadTemplateFields(task.Fields);
        if (!task.Fields.All(f => TaskFieldValues.IsFilled(f, templateFields.GetValueOrDefault(f.TemplateFieldId ?? 0))))
        {
            return Result.Fail(new BusinessRuleError("Fill every field and tick every checklist item to complete this task."));
        }

        task.DoneAt = DateTime.UtcNow;
        activityLog.RecordActivity(task, OnboardingTaskActivityKind.Completed);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> ReopenTask(int taskId, ClaimsPrincipal user)
    {
        Result<OnboardingFlowTaskEntity> loaded = await taskQueries.LoadTaskForEdit(taskId, user);
        if (loaded.IsFailed)
        {
            return loaded.ToResult();
        }

        OnboardingFlowTaskEntity task = loaded.Value;
        if (task.DoneAt is null)
        {
            return Result.Fail(new BusinessRuleError("Only a completed task can be reopened."));
        }

        task.DoneAt = null;
        task.StartedAt ??= DateTime.UtcNow;
        activityLog.RecordActivity(task, OnboardingTaskActivityKind.Reopened);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<List<RecheckAssignmentDto>> PreviewRecheck()
    {
        List<RecheckMatch> matches = await data.FindRecheckMatches(db.OnboardingFlowTasks.AsNoTracking());
        Dictionary<string, string> names = await data.ResolveEmployeeNames(matches.Select(m => m.AssigneeId));

        return [.. matches.Select(m => new RecheckAssignmentDto(
            TaskId: m.Task.Id,
            ResponsibleRoleKey: m.Task.ResponsibleRoleKey,
            ResponsibleRoleName: OnboardingRoles.Find(m.Task.ResponsibleRoleKey)?.ExactTitle,
            AssigneeId: m.AssigneeId,
            AssigneeName: names.GetValueOrDefault(m.AssigneeId)))];
    }

    public async Task<ApplyRecheckResultDto> ApplyRecheck(ApplyRecheckRequest request)
    {
        var confirmed = request.Assignments
            .DistinctBy(a => a.TaskId)
            .ToDictionary(a => a.TaskId, a => a.AssigneeId);
        List<int> taskIds = [.. confirmed.Keys];

        List<RecheckMatch> matches = await data.FindRecheckMatches(db.OnboardingFlowTasks.Where(t => taskIds.Contains(t.Id)));

        // Only the pairs HR confirmed are applied; a mapping changed since the preview leaves its task listed.
        int assignedCount = 0;
        foreach (RecheckMatch match in matches.Where(m => confirmed[m.Task.Id] == m.AssigneeId))
        {
            activityLog.RecordAssigneeChange(match.Task, match.AssigneeId);
            match.Task.AssigneeId = match.AssigneeId;
            assignedCount++;
        }

        await db.SaveChangesAsync();
        return new ApplyRecheckResultDto(assignedCount);
    }
}
