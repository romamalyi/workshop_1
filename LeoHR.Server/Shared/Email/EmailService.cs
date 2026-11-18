using System.Text;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Shared.Email;

public class EmailService(
    LeoHrDbContext db,
    ISmtpHttpClient smtpClient,
    IEmployeeLookup employeeLookup,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task<Result<EmailRecipients>> ResolveRecipientsAsync(
        string templateId,
        string employeeId,
        IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles = null,
        CancellationToken cancellationToken = default)
    {
        Result<EmailContext> context = await LoadContext(templateId, employeeId, cancellationToken);
        if (context.IsFailed) return Result.Fail(context.Errors);

        return await Resolve(context.Value.Template, context.Value.Employee, extraRoles);
    }

    public async Task<Result> SendForEmployeeAsync(
        string templateId,
        string employeeId,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles = null,
        IReadOnlyCollection<Recipient>? extraCc = null,
        EmailSender? from = null,
        CancellationToken cancellationToken = default)
    {
        Result<EmailContext> context = await LoadContext(templateId, employeeId, cancellationToken);
        if (context.IsFailed) return Result.Fail(context.Errors);
        (EmailTemplateInfo info, EmailTemplateEntity template, EmployeeRef employee) = context.Value;

        EmailRecipients recipients = await Resolve(template, employee, extraRoles);
        foreach (RecipientRole role in recipients.Unresolved)
        {
            logger.LogWarning("Email {TemplateId} for employee {EmployeeId} is sent without {Role}: no active person found",
                templateId, employeeId, role);
        }

        string[] to = [.. recipients.To.Select(r => r.Email)];
        string[] cc = [.. recipients.Cc.Select(r => r.Email)
            .Concat(extraCc?.Select(r => r.Email) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Except(to, StringComparer.OrdinalIgnoreCase)];
        if (to.Length == 0)
        {
            logger.LogWarning("Email {TemplateId} for employee {EmployeeId} not sent: no To recipient resolved",
                templateId, employeeId);
            return Result.Fail(new BusinessRuleError("None of the email's To recipients could be found."));
        }

        string subject = ApplyParameters(template.Subject, parameters);
        string body = ApplyParameters(template.Body, parameters);

        return await smtpClient.SendAsync(to, subject, body, info.IsHtml, cc, from, cancellationToken);
    }

    private async Task<Result<EmailContext>> LoadContext(
        string templateId, string employeeId, CancellationToken cancellationToken)
    {
        EmailTemplateInfo? info = EmailTemplateCatalog.Find(templateId);
        EmailTemplateEntity? template = await db.EmailTemplates.FindAsync([templateId], cancellationToken);
        if (info is null || template is null)
            return Result.Fail(new NotFoundError("Email template", templateId));

        EmployeeRef? employee = await employeeLookup.GetByIdAsync(employeeId);
        if (employee is null)
            return Result.Fail(new NotFoundError("Employee", employeeId));

        return new EmailContext(info, template, employee);
    }

    private async Task<EmailRecipients> Resolve(
        EmailTemplateEntity template, EmployeeRef employee, IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles)
    {
        Dictionary<RecipientRole, Recipient?> resolved = [];
        foreach (RecipientRole role in template.ToRecipients.Concat(template.CcRecipients).Distinct())
            resolved[role] = await ResolveRecipient(role, employee, extraRoles);

        List<EmailRecipient> to = ByAddress(template.ToRecipients, resolved);
        HashSet<string> toAddresses = new(to.Select(r => r.Email), StringComparer.OrdinalIgnoreCase);
        List<EmailRecipient> cc = [.. ByAddress(template.CcRecipients, resolved).Where(r => !toAddresses.Contains(r.Email))];
        List<RecipientRole> unresolved = [.. resolved.Where(r => r.Value is null).Select(r => r.Key)];
        return new EmailRecipients(to, cc, unresolved);
    }

    private async Task<Recipient?> ResolveRecipient(
        RecipientRole role, EmployeeRef employee, IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles)
    {
        Recipient? recipient = role switch
        {
            RecipientRole.Employee => ToRecipient(employee),
            RecipientRole.HrManager => ToRecipient(await FindActive(employee.HrManagerId)),
            RecipientRole.ProjectManager => ToRecipient(await FindActive(employee.ManagerId)),
            RecipientRole.HrManagersManager => ToRecipient(await FindManagersManager(employee.HrManagerId)),
            RecipientRole.ProjectManagersManager => ToRecipient(await FindManagersManager(employee.ManagerId)),
            _ => extraRoles?.GetValueOrDefault(role),
        };
        return string.IsNullOrWhiteSpace(recipient?.Email) ? null : recipient;
    }

    // The Employee API leaves a manager id empty when the name is ambiguous, so a namesake is never emailed.
    private async Task<EmployeeRef?> FindActive(string? employeeId)
    {
        return string.IsNullOrWhiteSpace(employeeId) ? null : await employeeLookup.GetByIdAsync(employeeId);
    }

    private async Task<EmployeeRef?> FindManagersManager(string? managerId)
    {
        EmployeeRef? manager = await FindActive(managerId);
        return manager is null ? null : await FindActive(manager.ManagerId);
    }

    private static Recipient? ToRecipient(EmployeeRef? employee)
    {
        return employee is null ? null : new Recipient($"{employee.FirstName} {employee.LastName}".Trim(), employee.Email);
    }

    private static List<EmailRecipient> ByAddress(
        IEnumerable<RecipientRole> roles, Dictionary<RecipientRole, Recipient?> resolved)
    {
        return [.. roles
            .Where(role => resolved[role] is not null)
            .GroupBy(role => resolved[role]!.Email, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                Recipient first = resolved[group.First()]!;
                return new EmailRecipient(first.Name, first.Email, [.. group]);
            })];
    }

    private static string ApplyParameters(string text, IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return text;

        var builder = new StringBuilder(text);
        foreach ((string? key, string? value) in parameters)
            builder.Replace($"{{{key}}}", value);

        return builder.ToString();
    }

    private sealed record EmailContext(EmailTemplateInfo Info, EmailTemplateEntity Template, EmployeeRef Employee);
}
