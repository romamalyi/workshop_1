using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingFlowEntity : BaseAuditableEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public DateOnly StartDate { get; set; }
    public Seniority? Level { get; set; }

    /// <summary>Copied from the onboarding settings at creation, so a later settings change doesn't move this flow.</summary>
    public int ProbationMonths { get; set; }

    /// <summary>Employee API id of the HR person who owns this flow. Every HR-manager-role task resolves to them.</summary>
    public string ResponsiblePersonId { get; set; } = string.Empty;

    public string? ResponsiblePosition { get; set; }

    /// <summary>The type the flow was created from. Null for flows created before types existed.</summary>
    public int? OnboardingTypeId { get; set; }

    public ICollection<OnboardingFlowTaskEntity> Tasks { get; set; } = [];
}
