using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

/// <summary>One entry of a task's activity log; CreatedAt/CreatedById record when and by whom it happened.</summary>
public class OnboardingFlowTaskActivityEntity : BaseAuditableEntity
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public OnboardingFlowTaskEntity Task { get; set; } = null!;

    public OnboardingTaskActivityKind Kind { get; set; }

    /// <summary>Null when the task had no assignee before.</summary>
    public string? FromAssigneeId { get; set; }
    public string? ToAssigneeId { get; set; }

    /// <summary>Copied labels, never a field's value: field values hold employee personal data.</summary>
    public string? FieldLabel { get; set; }
    public string? ChecklistItemText { get; set; }
}
