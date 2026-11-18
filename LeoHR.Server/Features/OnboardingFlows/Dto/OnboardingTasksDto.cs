using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.OnboardingFlows.Dto;

public record MyOnboardingTaskDto(
    int Id,
    string Title,
    OnboardingPhase Phase,
    DateOnly DueDate,
    bool IsDone,
    bool IsStarted,
    List<string> FieldLabels,
    string? AssigneeId,
    string? AssigneeName,
    string? ResponsibleRoleName,
    bool IsAssignedToMe,
    int FlowId,
    string FlowFullName,
    string? FlowPosition,
    DateOnly FlowStartDate
);

public record OnboardingTaskFlowDto(int FlowId, List<MyOnboardingTaskDto> Tasks);

public record OnboardingTasksSummaryDto(
    int TaskCount,
    int FlowCount,
    int MineCount,
    int OverdueCount,
    int DueTodayCount,
    int AllScopeCount,
    int MineScopeCount
);

public record TaskChecklistItemDto(int Id, string Text, bool IsChecked);

public record OnboardingTaskFieldDto(
    int Id,
    TemplateFieldType FieldType,
    string Label,
    string? Hint,
    string? PersonRestrictionKey,
    string? PersonRestrictionLabel,
    string? NoteValue,
    string? PersonId,
    string? PersonName,
    List<TaskChecklistItemDto> ChecklistItems
);

public record OnboardingTaskDetailDto(
    int Id,
    string Title,
    string? Description,
    OnboardingPhase Phase,
    string MilestoneName,
    DateOnly DueDate,
    OnboardingTaskStatus Status,
    int FlowId,
    string FlowFullName,
    string? AssigneeId,
    string? AssigneeName,
    string? ResponsibleRoleKey,
    string? ResponsibleRoleName,
    bool IsAssignedToMe,
    bool CanEdit,
    bool CanChangeAssignee,
    List<OnboardingTaskFieldDto> Fields,
    List<TaskActivityDto> Activity
);

public record TaskActivityDto(
    OnboardingTaskActivityKind Kind,
    string? FromName,
    string? ToName,
    string? FieldLabel,
    string? ChecklistItemText,
    DateTime At,
    string? ActorName
);

public record TaskFieldValueInput(
    [Required] int FieldId,
    [MaxLength(4000)] string? NoteValue,
    [MaxLength(100)] string? PersonId,
    List<int>? CheckedItemIds
);

public record UpdateTaskFieldsRequest(
    [Required] List<TaskFieldValueInput> Fields
);
