namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingFlowTaskCheckedItemEntity
{
    public int TaskFieldId { get; set; }
    public OnboardingFlowTaskFieldEntity TaskField { get; set; } = null!;

    public int ChecklistItemId { get; set; }
    public OnboardingTaskTemplateChecklistItemEntity ChecklistItem { get; set; } = null!;
}
