using LeoHR.Server.Shared.Email;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class EmailTemplateEntity
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<RecipientRole> ToRecipients { get; set; } = [];
    public List<RecipientRole> CcRecipients { get; set; } = [];

    //Stored as JSONB
    public List<EmailTemplateChecklistItem> Checklist { get; set; } = [];
}
