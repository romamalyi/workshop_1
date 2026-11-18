namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingFlowTaskEntity
{
    public int Id { get; set; }
    public int FlowId { get; set; }
    public OnboardingFlowEntity Flow { get; set; } = null!;

    /// <summary>The template this task was generated from, kept only for traceability — later template edits
    /// must not change already-generated tasks, so the due-rule and fields below are snapshots, not live references.</summary>
    public int? TemplateId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OnboardingPhase Phase { get; set; }

    public int MilestoneId { get; set; }
    public OnboardingMilestoneEntity Milestone { get; set; } = null!;
    public string? ResponsibleRoleKey { get; set; }

    public int DueOffsetDays { get; set; }
    public DueDirection DueDirection { get; set; }
    public DueAnchor DueAnchor { get; set; }
    public DateOnly DueDate { get; set; }

    public string? AssigneeId { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? DoneAt { get; set; }

    public ICollection<OnboardingFlowTaskFieldEntity> Fields { get; set; } = [];
    public ICollection<OnboardingFlowTaskActivityEntity> Activities { get; set; } = [];
}
