using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTaskTemplateEntity : BaseAuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public OnboardingPhase Phase { get; set; }

    public int MilestoneId { get; set; }
    public OnboardingMilestoneEntity Milestone { get; set; } = null!;
    public string? Description { get; set; }

    public int DueOffsetDays { get; set; }
    public DueDirection DueDirection { get; set; }
    public DueAnchor DueAnchor { get; set; }

    public TemplateAppliesTo AppliesTo { get; set; }
    public bool IsActive { get; set; } = true;

    public string? ResponsibleRoleKey { get; set; }

    public ICollection<OnboardingTaskTemplateSuggestedLevelEntity> SuggestedLevels { get; set; } = [];
    public ICollection<OnboardingTaskTemplateFieldEntity> Fields { get; set; } = [];
}
