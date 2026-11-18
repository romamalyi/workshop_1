using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.OnboardingTemplates.Dto;

public record TemplateChecklistItemDto(int Id, string Text);

public record TemplateFieldDto(
    int Id,
    TemplateFieldType FieldType,
    string Label,
    string? Hint,
    string? PersonRestrictionKey,
    List<TemplateChecklistItemDto> ChecklistItems
);

public record OnboardingTemplateDto(
    int Id,
    string Title,
    OnboardingPhase Phase,
    int MilestoneId,
    string? ResponsibleRoleKey,
    string? ResponsibleRoleName,
    string? Description,
    int DueOffsetDays,
    DueDirection DueDirection,
    DueAnchor DueAnchor,
    TemplateAppliesTo AppliesTo,
    List<Seniority> SuggestedFor,
    bool IsActive,
    List<TemplateFieldDto> Fields,
    DateTime? UpdatedAt,
    string? UpdatedByName
);

public record SaveChecklistItemInput(
    int? Id,
    [Required][MaxLength(300)] string Text
);

public record SaveFieldInput(
    int? Id,
    [Required] TemplateFieldType FieldType,
    [Required][MaxLength(200)] string Label,
    [MaxLength(300)] string? Hint,
    [MaxLength(200)] string? PersonRestrictionKey,
    List<SaveChecklistItemInput>? ChecklistItems
);

public record SaveTemplateRequest(
    [Required][MaxLength(200)] string Title,
    [Required] OnboardingPhase Phase,
    [Required][Range(1, int.MaxValue)] int MilestoneId,
    [MaxLength(100)] string? ResponsibleRoleKey,
    [MaxLength(2000)] string? Description,
    [Range(0, 365)] int DueOffsetDays,
    [Required] DueDirection DueDirection,
    [Required] DueAnchor DueAnchor,
    [Required] TemplateAppliesTo AppliesTo,
    List<Seniority>? SuggestedFor,
    List<SaveFieldInput>? Fields
);

public record SetTemplateStatusRequest(bool IsActive);

public record MilestoneOptionDto(int Id, string Name);
