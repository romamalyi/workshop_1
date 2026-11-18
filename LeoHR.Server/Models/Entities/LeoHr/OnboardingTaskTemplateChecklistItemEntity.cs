namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTaskTemplateChecklistItemEntity
{
    public int Id { get; set; }

    public int FieldId { get; set; }
    public OnboardingTaskTemplateFieldEntity Field { get; set; } = null!;

    public int SortOrder { get; set; }
    public string Text { get; set; } = string.Empty;
}
