using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingFlows.Internal;

internal sealed class OnboardingTaskQueries(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeApiClient employeeApi,
    ILogger logger)
{
    public const string TaskResourceName = "Onboarding task";

    private readonly OnboardingFlowData data = new(db, employeeLookup, employeeApi, logger);

    // Applies access, status and search, but not the scope. Null when the caller can see no tasks.
    public async Task<IQueryable<OnboardingFlowTaskEntity>?> QueryVisibleTasks(OnboardingTaskFilterModel filter, ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        bool seesAllTasks = TaskAccess.SeesAllTasks(user);
        if (!seesAllTasks && userId is null)
        {
            return null;
        }

        IQueryable<OnboardingFlowTaskEntity> query = db.OnboardingFlowTasks;
        if (!seesAllTasks)
        {
            query = query.Where(TaskAccess.IsVisibleTo(user));
        }

        DateOnly today = TodayFor(filter);
        query = filter.Status switch
        {
            OnboardingTaskListStatus.Open => query.Where(t => t.DoneAt == null),
            OnboardingTaskListStatus.Overdue => query.Where(t => t.DoneAt == null && t.DueDate < today),
            OnboardingTaskListStatus.Done => query.Where(t => t.DoneAt != null),
            _ => query,
        };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim().ToLower();
            List<string> assigneeIds = await employeeLookup.SearchIdsByNameAsync(term);
            query = query.Where(t => t.Title.ToLower().Contains(term)
                || t.Flow.FullName.ToLower().Contains(term)
                || (t.AssigneeId != null && assigneeIds.Contains(t.AssigneeId)));
        }

        return query;
    }

    public static DateOnly TodayFor(OnboardingTaskFilterModel filter)
    {
        return filter.Today ?? DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public async Task<PagedResult<OnboardingTaskFlowDto>> LoadPage(
        IQueryable<OnboardingFlowTaskEntity> query,
        OnboardingTaskFilterModel filter,
        ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();

        // A page is a set of flows, so one flow's tasks never split across pages.
        IQueryable<OnboardingFlowEntity> flows = db.OnboardingFlows.Where(f => query.Any(t => t.FlowId == f.Id));
        int totalCount = await flows.CountAsync();
        IQueryable<int> flowIdsQuery = flows.OrderBy(f => f.StartDate).ThenBy(f => f.Id).Select(f => f.Id);
        if (filter.PageNumber is > 0 && filter.PageSize is > 0)
        {
            flowIdsQuery = flowIdsQuery.Skip((filter.PageNumber.Value - 1) * filter.PageSize.Value).Take(filter.PageSize.Value);
        }

        List<int> flowIds = await flowIdsQuery.ToListAsync();

        var tasks = await query
            .Where(t => flowIds.Contains(t.FlowId))
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Phase,
                t.DueDate,
                t.DoneAt,
                t.StartedAt,
                t.AssigneeId,
                t.ResponsibleRoleKey,
                t.FlowId,
                FlowFullName = t.Flow.FullName,
                FlowPosition = t.Flow.Position,
                FlowStartDate = t.Flow.StartDate,
                FieldLabels = t.Fields.OrderBy(f => f.SortOrder).Select(f => f.Label).ToList(),
            })
            .ToListAsync();

        // Only a writer sees other assignees, so only they need the names.
        Dictionary<string, string> names = user.CanWriteOnboarding()
            ? await data.ResolveEmployeeNames(tasks.Select(t => t.AssigneeId).OfType<string>())
            : [];

        ILookup<int, MyOnboardingTaskDto> byFlow = tasks.Select(t => new MyOnboardingTaskDto(
            Id: t.Id,
            Title: t.Title,
            Phase: t.Phase,
            DueDate: t.DueDate,
            IsDone: t.DoneAt is not null,
            IsStarted: t.StartedAt is not null,
            FieldLabels: t.FieldLabels,
            AssigneeId: t.AssigneeId,
            AssigneeName: t.AssigneeId is null ? null : names.GetValueOrDefault(t.AssigneeId),
            ResponsibleRoleName: OnboardingRoles.Find(t.ResponsibleRoleKey)?.ExactTitle,
            IsAssignedToMe: t.AssigneeId is not null && t.AssigneeId == userId,
            FlowId: t.FlowId,
            FlowFullName: t.FlowFullName,
            FlowPosition: t.FlowPosition,
            FlowStartDate: t.FlowStartDate)).ToLookup(t => t.FlowId);

        return new PagedResult<OnboardingTaskFlowDto>(
            [.. flowIds.Select(id => new OnboardingTaskFlowDto(id, [.. byFlow[id]]))],
            totalCount);
    }

    public async Task<Result<OnboardingFlowTaskEntity>> LoadTaskForEdit(int taskId, ClaimsPrincipal user)
    {
        OnboardingFlowTaskEntity? task = await db.OnboardingFlowTasks
            .Include(t => t.Fields)
                .ThenInclude(f => f.CheckedItems)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
        {
            return Result.Fail(new NotFoundError(TaskResourceName, taskId));
        }

        if (!TaskAccess.IsAdminOrAssignee(task, user))
        {
            return Result.Fail(new ForbiddenError("Only the task's assignee or an Admin can update this task."));
        }

        if (task.AssigneeId is null)
        {
            return Result.Fail(new BusinessRuleError("An owner must be assigned to this task first."));
        }

        return Result.Ok(task);
    }
}
