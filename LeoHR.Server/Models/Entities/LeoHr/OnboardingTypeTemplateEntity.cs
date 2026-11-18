namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTypeTemplateEntity
{
    public int TypeId { get; set; }

    public int TemplateId { get; set; }

    public bool IsEnabled { get; set; } = true;
    public OnboardingTaskTemplateEntity Template { get; set; } = null!;
}
