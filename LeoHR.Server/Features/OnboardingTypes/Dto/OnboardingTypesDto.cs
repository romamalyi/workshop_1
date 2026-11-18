using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.OnboardingTypes.Dto;

/// <summary>The counts are computed on every read, never stored.</summary>
public record OnboardingTypeDto(
    int Id,
    string Name,
    string? Description,
    bool IsArchived,
    int EnabledTemplateCount,
    int FlowCount,
    DateTime? LastEditedAt
);

public record OnboardingTypesDto(
    int LibraryTemplateCount,
    List<OnboardingTypeDto> Types
);

public record SetTypeArchivedRequest(bool IsArchived);

public record TypeTemplateSelectionDto(int TemplateId, bool IsEnabled);

public record OnboardingTypeDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsArchived,
    List<TypeTemplateSelectionDto> Templates
);

public record SaveTypeRequest(
    [Required][MaxLength(OnboardingTypeEntity.NameMaxLength)] string Name,
    [MaxLength(200)] string? Description,
    bool IsArchived,
    [Required] List<TypeTemplateSelectionDto> Templates
);

public record TypeLibraryFieldDto(TemplateFieldType FieldType, int ChecklistItemCount);

public record TypeLibraryTemplateDto(
    int Id,
    string Title,
    string? Description,
    OnboardingPhase Phase,
    string? ResponsibleRoleName,
    int DueOffsetDays,
    DueDirection DueDirection,
    DueAnchor DueAnchor,
    TemplateAppliesTo AppliesTo,
    List<Seniority> SuggestedFor,
    bool IsActive,
    List<TypeLibraryFieldDto> Fields
);
