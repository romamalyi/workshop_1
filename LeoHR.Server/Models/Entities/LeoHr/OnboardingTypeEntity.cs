using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class OnboardingTypeEntity : BaseAuditableEntity
{
    public const int NameMaxLength = 100;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }

    /// <summary>The saved selection. A deactivated template stays here and is only ignored while inactive.</summary>
    public ICollection<OnboardingTypeTemplateEntity> Templates { get; set; } = [];
}
