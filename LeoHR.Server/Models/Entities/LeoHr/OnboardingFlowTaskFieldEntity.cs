namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingFlowTaskFieldEntity
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    public OnboardingFlowTaskEntity Task { get; set; } = null!;

    public int SortOrder { get; set; }
    public TemplateFieldType FieldType { get; set; }
    public string Label { get; set; } = string.Empty;

    public int? TemplateFieldId { get; set; }

    public string? NoteValue { get; set; }
    public string? PersonId { get; set; }

    public ICollection<OnboardingFlowTaskCheckedItemEntity> CheckedItems { get; set; } = [];
}
