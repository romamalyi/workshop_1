using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.EmailTemplates;

public interface IEmailTemplatesService
{
    Task<EmailTemplateResponse?> GetById(string id);
    Task<List<EmailTemplateSettingsResponse>> GetByCategory(EmailTemplateCategory category);
    Task<Result> Update(string id, UpdateEmailTemplateRequest request);
}

public class EmailTemplatesService(LeoHrDbContext db) : IEmailTemplatesService
{
    public async Task<EmailTemplateResponse?> GetById(string id)
    {
        EmailTemplateEntity? template = await db.EmailTemplates.FindAsync(id);
        if (template is null) return null;
        return new EmailTemplateResponse(
            template.Id,
            template.Label,
            template.Subject,
            template.Body,
            [.. template.Checklist.Select(c => new ChecklistItemDto(c.Id, c.Label))]);
    }

    public async Task<List<EmailTemplateSettingsResponse>> GetByCategory(EmailTemplateCategory category)
    {
        List<EmailTemplateInfo> infos = [.. EmailTemplateCatalog.All.Where(t => t.Category == category)];
        List<string> ids = [.. infos.Select(t => t.Id)];
        Dictionary<string, EmailTemplateEntity> templates = await db.EmailTemplates
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        return [.. infos
            .Where(info => templates.ContainsKey(info.Id))
            .Select(info => ToSettingsResponse(templates[info.Id], info))];
    }

    public async Task<Result> Update(string id, UpdateEmailTemplateRequest request)
    {
        EmailTemplateEntity? template = await db.EmailTemplates.FindAsync(id);
        if (template is null) return Result.Fail(new NotFoundError("Email template", id));

        if (request.ToRecipients is not null || request.CcRecipients is not null)
        {
            List<RecipientRole> to = request.ToRecipients ?? template.ToRecipients;
            List<RecipientRole> cc = request.CcRecipients ?? template.CcRecipients;
            Result valid = ValidateRecipients(EmailTemplateCatalog.Find(id), to, cc);
            if (valid.IsFailed) return valid;

            template.ToRecipients = [.. to.Distinct()];
            template.CcRecipients = [.. cc.Distinct()];
        }

        template.Subject = request.Subject;
        template.Body = request.Body;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    private static Result ValidateRecipients(EmailTemplateInfo? info, List<RecipientRole> to, List<RecipientRole> cc)
    {
        if (info is null)
            return Result.Fail(new BusinessRuleError("This email has no configurable recipients."));
        if (to.Count == 0)
            return Result.Fail(new BusinessRuleError("Choose at least one To recipient.", nameof(UpdateEmailTemplateRequest.ToRecipients)));
        if (to.Concat(cc).Any(role => !info.AllowedRecipients.Contains(role)))
            return Result.Fail(new BusinessRuleError("A recipient isn't allowed for this email.", nameof(UpdateEmailTemplateRequest.ToRecipients)));
        if (to.Intersect(cc).Any())
            return Result.Fail(new BusinessRuleError("A recipient can't be in both To and CC.", nameof(UpdateEmailTemplateRequest.CcRecipients)));

        return Result.Ok();
    }

    private static EmailTemplateSettingsResponse ToSettingsResponse(EmailTemplateEntity template, EmailTemplateInfo info)
    {
        return new EmailTemplateSettingsResponse(
            template.Id,
            template.Label,
            template.Subject,
            template.Body,
            info.IsHtml,
            template.ToRecipients,
            template.CcRecipients,
            info.Placeholders,
            info.AllowedRecipients);
    }
}
