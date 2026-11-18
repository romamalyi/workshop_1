using FluentResults;

namespace LeoHR.Server.Shared.Email;

public interface IEmailService
{
    // The recipients SendForEmployeeAsync would use, before any extra CC.
    Task<Result<EmailRecipients>> ResolveRecipientsAsync(
        string templateId,
        string employeeId,
        IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles = null,
        CancellationToken cancellationToken = default);

    // Skips roles that don't resolve to exactly one address; fails only when To ends up empty.
    Task<Result> SendForEmployeeAsync(
        string templateId,
        string employeeId,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<RecipientRole, Recipient>? extraRoles = null,
        IReadOnlyCollection<Recipient>? extraCc = null,
        EmailSender? from = null,
        CancellationToken cancellationToken = default);
}
