namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTaskTemplateFieldEntity
{
    public int Id { get; set; }

    public int TemplateId { get; set; }
    public OnboardingTaskTemplateEntity Template { get; set; } = null!;

    public int SortOrder { get; set; }
    public TemplateFieldType FieldType { get; set; }
    public string Label { get; set; } = string.Empty;

    public string? Hint { get; set; }

    // Key from PersonRestrictions; null = anyone. Only used by EmployeePicker fields.
    public string? PersonRestrictionKey { get; set; }

    public ICollection<OnboardingTaskTemplateChecklistItemEntity> ChecklistItems { get; set; } = [];
}
