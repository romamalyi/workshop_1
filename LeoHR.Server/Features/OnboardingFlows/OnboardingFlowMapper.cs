using System.Security.Claims;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingFlows;

internal static class OnboardingFlowMapper
{
    public static bool IsCompleted(OnboardingFlowEntity flow)
    {
        return flow.Tasks.Count > 0 && flow.Tasks.All(t => t.DoneAt is not null);
    }

    public static OnboardingTaskStatus GetStatus(OnboardingFlowTaskEntity task)
    {
        if (task.DoneAt is not null)
        {
            return OnboardingTaskStatus.Done;
        }

        return task.StartedAt is null ? OnboardingTaskStatus.NotStarted : OnboardingTaskStatus.InProgress;
    }

    // Falls back to the raw id so a log entry stays readable when the Employee API can't resolve the person.
    public static string? DisplayName(string? employeeId, Dictionary<string, string> names)
    {
        return employeeId is null ? null : names.GetValueOrDefault(employeeId, employeeId);
    }

    public static OnboardingFlowDto MapToDto(
        OnboardingFlowEntity flow,
        Dictionary<string, string> names,
        FlowWarningContext warningContext,
        Dictionary<int, OnboardingTypeInfo> types,
        HashSet<int> taskIdsWithFields)
    {
        OnboardingTypeInfo? type = flow.OnboardingTypeId is int typeId ? types.GetValueOrDefault(typeId) : null;
        int unassignedCount = flow.Tasks.Count(t => t.AssigneeId is null && t.DoneAt is null);
        bool isCompleted = IsCompleted(flow);
        OnboardingFlowStatus status = unassignedCount > 0
            ? OnboardingFlowStatus.CreatedUnresolved
            : isCompleted ? OnboardingFlowStatus.Completed : OnboardingFlowStatus.Created;

        return new OnboardingFlowDto(
            Id: flow.Id,
            FullName: flow.FullName,
            Position: flow.Position,
            EmploymentType: flow.EmploymentType,
            StartDate: flow.StartDate,
            Level: flow.Level,
            OnboardingTypeId: flow.OnboardingTypeId,
            OnboardingTypeName: type?.Name,
            OnboardingTypeIsArchived: type?.IsArchived ?? false,
            ResponsiblePersonId: flow.ResponsiblePersonId,
            ResponsiblePersonName: names.GetValueOrDefault(flow.ResponsiblePersonId),
            ResponsiblePersonRoleName: warningContext.Employees.GetValueOrDefault(flow.ResponsiblePersonId)?.Position?.Trim()
                ?? flow.ResponsiblePosition
                ?? string.Empty,
            Warnings: FlowWarnings.Build(flow, isCompleted, warningContext),
            CreatedById: flow.CreatedById,
            CreatedByName: flow.CreatedById is null ? null : names.GetValueOrDefault(flow.CreatedById),
            Status: status,
            UnassignedTaskCount: unassignedCount,
            CompletedAt: isCompleted ? flow.Tasks.Max(t => t.DoneAt) : null,
            ProbationReviewDate: flow.StartDate.AddMonths(flow.ProbationMonths),
            Tasks:
            [
                .. flow.Tasks
                    .OrderBy(t => t.Phase)
                    .ThenBy(t => t.DueDate)
                    .Select(t => new OnboardingFlowTaskDto(
                        Id: t.Id,
                        Title: t.Title,
                        Description: t.Description,
                        Phase: t.Phase,
                        MilestoneName: t.Milestone.Name,
                        ResponsibleRoleKey: t.ResponsibleRoleKey,
                        ResponsibleRoleName: OnboardingRoles.Find(t.ResponsibleRoleKey)?.ExactTitle,
                        AssigneeId: t.AssigneeId,
                        AssigneeName: t.AssigneeId is null ? null : names.GetValueOrDefault(t.AssigneeId),
                        DueDate: t.DueDate,
                        DueOffsetDays: t.DueOffsetDays,
                        DueDirection: t.DueDirection,
                        DueAnchor: t.DueAnchor,
                        Status: GetStatus(t),
                        HasFields: taskIdsWithFields.Contains(t.Id)))
            ]
        );
    }

    public static OnboardingTaskDetailDto MapTaskDetail(
        OnboardingFlowTaskEntity task,
        ClaimsPrincipal user,
        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields,
        Dictionary<string, string> names)
    {
        string? userId = user.GetUserId();

        return new OnboardingTaskDetailDto(
            Id: task.Id,
            Title: task.Title,
            Description: task.Description,
            Phase: task.Phase,
            MilestoneName: task.Milestone.Name,
            DueDate: task.DueDate,
            Status: GetStatus(task),
            FlowId: task.FlowId,
            FlowFullName: task.Flow.FullName,
            AssigneeId: task.AssigneeId,
            AssigneeName: task.AssigneeId is null ? null : names.GetValueOrDefault(task.AssigneeId),
            ResponsibleRoleKey: task.ResponsibleRoleKey,
            ResponsibleRoleName: OnboardingRoles.Find(task.ResponsibleRoleKey)?.ExactTitle,
            IsAssignedToMe: task.AssigneeId is not null && task.AssigneeId == userId,
            CanEdit: TaskAccess.CanEdit(task, user),
            CanChangeAssignee: task.DoneAt is null && TaskAccess.CanChangeAssignee(task, user),
            Fields: [.. task.Fields.Select(f => MapField(f, templateFields, names))],
            Activity: [.. task.Activities.Select(a => new TaskActivityDto(
                Kind: a.Kind,
                FromName: DisplayName(a.FromAssigneeId, names),
                ToName: DisplayName(a.ToAssigneeId, names),
                FieldLabel: a.FieldLabel,
                ChecklistItemText: a.ChecklistItemText,
                At: a.CreatedAt,
                ActorName: DisplayName(a.CreatedById, names)))]);
    }

    private static OnboardingTaskFieldDto MapField(
        OnboardingFlowTaskFieldEntity field,
        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields,
        Dictionary<string, string> names)
    {
        OnboardingTaskTemplateFieldEntity? templateField = templateFields.GetValueOrDefault(field.TemplateFieldId ?? 0);
        HashSet<int> checkedIds = [.. field.CheckedItems.Select(c => c.ChecklistItemId)];

        return new OnboardingTaskFieldDto(
            Id: field.Id,
            FieldType: field.FieldType,
            Label: field.Label,
            Hint: templateField?.Hint,
            PersonRestrictionKey: templateField?.PersonRestrictionKey,
            PersonRestrictionLabel: templateField?.PersonRestrictionKey,
            NoteValue: field.NoteValue,
            PersonId: field.PersonId,
            PersonName: field.PersonId is null ? null : names.GetValueOrDefault(field.PersonId),
            ChecklistItems: [.. (templateField?.ChecklistItems ?? [])
                .OrderBy(i => i.SortOrder)
                .Select(i => new TaskChecklistItemDto(i.Id, i.Text, checkedIds.Contains(i.Id)))]);
    }
}
