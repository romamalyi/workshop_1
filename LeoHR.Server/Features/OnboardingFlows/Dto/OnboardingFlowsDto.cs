using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.OnboardingFlows.Dto;

/// <summary>Computed from the flow's tasks on every read, never stored.</summary>
public enum OnboardingFlowStatus
{
    Created,
    CreatedUnresolved,
    Completed,
}

/// <summary>Derived from the task's StartedAt/DoneAt timestamps, never stored.</summary>
public enum OnboardingTaskStatus
{
    NotStarted,
    InProgress,
    Done,
}

public record OnboardingFlowTaskDto(
    int Id,
    string Title,
    string? Description,
    OnboardingPhase Phase,
    string MilestoneName,
    string? ResponsibleRoleKey,
    string? ResponsibleRoleName,
    string? AssigneeId,
    string? AssigneeName,
    DateOnly DueDate,
    int DueOffsetDays,
    DueDirection DueDirection,
    DueAnchor DueAnchor,
    OnboardingTaskStatus Status,
    bool HasFields
);

public enum FlowWarningKind
{
    ResponsiblePositionNotAllowed,
    ResponsiblePositionChanged,
    ResponsibleInactive,
    RoleDefaultInactive,
}

public record FlowWarningDto(FlowWarningKind Kind, List<string> RoleNames);

public record ChangeResponsibleRequest(
    [Required][MaxLength(100)] string ResponsiblePersonId,
    bool ReassignNotStartedTasks
);

public record OnboardingFlowDto(
    int Id,
    string FullName,
    string? Position,
    EmploymentType EmploymentType,
    DateOnly StartDate,
    Seniority? Level,
    int? OnboardingTypeId,
    string? OnboardingTypeName,
    bool OnboardingTypeIsArchived,
    string ResponsiblePersonId,
    string? ResponsiblePersonName,
    string ResponsiblePersonRoleName,
    List<FlowWarningDto> Warnings,
    string? CreatedById,
    string? CreatedByName,
    OnboardingFlowStatus Status,
    int UnassignedTaskCount,
    DateTime? CompletedAt,
    DateOnly ProbationReviewDate,
    List<OnboardingFlowTaskDto> Tasks
);

public record CreateOnboardingFlowRequest(
    [Required][MaxLength(200)] string FullName,
    [MaxLength(200)] string? Position,
    [Required] EmploymentType EmploymentType,
    [Required] DateOnly StartDate,
    Seniority? Level,
    [Required][MaxLength(100)] string ResponsiblePersonId,
    [Required][Range(1, int.MaxValue)] int OnboardingTypeId,
    List<int>? OptionalTemplateIds
);

public record UnassignedTaskDto(
    int TaskId,
    string Title,
    OnboardingPhase Phase,
    string? ResponsibleRoleKey,
    string? ResponsibleRoleName,
    DateOnly DueDate,
    int FlowId,
    string FlowFullName
);

public record AssignTaskRequest(
    [Required][MaxLength(100)] string AssigneeId
);

public record RecheckAssignmentDto(
    int TaskId,
    string? ResponsibleRoleKey,
    string? ResponsibleRoleName,
    string AssigneeId,
    string? AssigneeName
);

public record RecheckAssignmentInput(
    [Range(1, int.MaxValue)] int TaskId,
    [Required][MaxLength(100)] string AssigneeId
);

public record ApplyRecheckRequest(
    [Required][MinLength(1)] List<RecheckAssignmentInput> Assignments
);

public record ApplyRecheckResultDto(int AssignedCount);

internal sealed record OnboardingTypeInfo(string Name, bool IsArchived);
