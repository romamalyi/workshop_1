using LeoHR.Server.Shared.Email;

namespace LeoHR.Server.Features.EmailTemplates;

public record ChecklistItemDto(string Id, string Label);

public record EmailTemplateResponse(
    string Id,
    string Label,
    string Subject,
    string Body,
    List<ChecklistItemDto> Checklist
);

public record EmailTemplateSettingsResponse(
    string Id,
    string Label,
    string Subject,
    string Body,
    bool IsHtml,
    List<RecipientRole> ToRecipients,
    List<RecipientRole> CcRecipients,
    IReadOnlyList<EmailPlaceholder> Placeholders,
    IReadOnlyList<RecipientRole> AllowedRecipients
);
