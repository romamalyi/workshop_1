namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTaskTemplateSuggestedLevelEntity
{
    public int Id { get; set; }

    public int TemplateId { get; set; }
    public OnboardingTaskTemplateEntity Template { get; set; } = null!;

    public Seniority Level { get; set; }
}
