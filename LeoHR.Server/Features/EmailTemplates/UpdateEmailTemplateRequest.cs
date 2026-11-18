using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Shared.Email;

namespace LeoHR.Server.Features.EmailTemplates;

public record UpdateEmailTemplateRequest(
    [Required, MaxLength(300)] string Subject,
    [Required] string Body,
    List<RecipientRole>? ToRecipients = null,
    List<RecipientRole>? CcRecipients = null
);
