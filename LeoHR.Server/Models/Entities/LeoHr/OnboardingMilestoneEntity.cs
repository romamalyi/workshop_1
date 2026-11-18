namespace LeoHR.Server.Models.Entities.LeoHr;

/// <summary>Reference data: the stage of the onboarding process a template (and the tasks made from it) belongs to.</summary>
public class OnboardingMilestoneEntity
{
    public const int NameMaxLength = 100;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
