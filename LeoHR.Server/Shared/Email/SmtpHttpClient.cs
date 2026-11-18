using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

using FluentResults;

using LeoHR.Server.Shared.Results.Errors;

using Microsoft.Extensions.Options;

namespace LeoHR.Server.Shared.Email;

public class SmtpHttpClient(
    IOptions<EmailSettings> settings,
    IEmailWhitelist whitelist,
    ILogger<SmtpHttpClient> logger) : ISmtpHttpClient
{
    private readonly EmailSettings emailSettings = settings.Value;

    public async Task<Result> SendAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        bool isHtml = false,
        IEnumerable<string>? cc = null,
        EmailSender? from = null,
        CancellationToken cancellationToken = default)
    {
        string[] to = recipients?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToArray() ?? [];

        if (to.Length == 0)
            return Result.Fail(new BusinessRuleError("At least one recipient is required.", nameof(recipients)));

        string[] copies = cc?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Except(to, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (emailSettings.WhitelistEnabled)
        {
            IReadOnlySet<string> allowed = await whitelist.GetAllowedAsync(cancellationToken);
            string[] filtered = [.. to.Where(allowed.Contains)];
            string[] filteredCopies = [.. copies.Where(allowed.Contains)];

            string[] dropped = [.. to.Concat(copies).Except(filtered.Concat(filteredCopies), StringComparer.OrdinalIgnoreCase)];
            if (dropped.Length > 0)
            {
                logger.LogInformation(
                    "Email whitelist active — dropping non-whitelisted recipients {Dropped} for subject {Subject}",
                    string.Join(", ", dropped), subject);
            }

            // CC alone is never sent: the body is written for the To recipient.
            if (filtered.Length == 0)
            {
                logger.LogInformation(
                    "Email whitelist active — no whitelisted To recipients for subject {Subject}; skipping send.",
                    subject);
                return Result.Ok();
            }

            to = filtered;
            copies = filteredCopies;
        }

        try
        {
            using var smtpClient = new SmtpClient(emailSettings.SmtpSettings.Host, emailSettings.SmtpSettings.Port)
            {
                EnableSsl = emailSettings.SmtpSettings.EnableSsl
            };

            if (emailSettings.Credentials is not null)
            {
                smtpClient.Credentials = new NetworkCredential(
                    emailSettings.Credentials.Username,
                    emailSettings.Credentials.Password);
            }

            using var mailMessage = new MailMessage
            {
                From = SenderAddress(from),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (string? recipient in to)
                mailMessage.To.Add(recipient);

            foreach (string copy in copies)
                mailMessage.CC.Add(copy);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipients} (+{CcCount} in CC) with subject {Subject}",
                string.Join(", ", to), copies.Length, subject);
            Error error = IsMailServerFailure(ex)
                ? new ServiceUnavailableError("The email server is unavailable. Try again later.")
                : new Error("Failed to send email.");
            return Result.Fail(error.CausedBy(ex));
        }
    }

    // Only failures a later retry can fix: the relay is unreachable, busy or closing. A rejected recipient, message
    // or login is a real error and must not read as "try again later".
    private static readonly HashSet<SmtpStatusCode> transientStatusCodes =
    [
        SmtpStatusCode.GeneralFailure,
        SmtpStatusCode.ServiceClosingTransmissionChannel,
        SmtpStatusCode.ServiceNotAvailable,
        SmtpStatusCode.MailboxBusy,
        SmtpStatusCode.LocalErrorInProcessing,
        SmtpStatusCode.InsufficientStorage,
    ];

    private static bool IsMailServerFailure(Exception ex)
    {
        return ex switch
        {
            SmtpFailedRecipientException => false,
            SmtpException smtp => transientStatusCodes.Contains(smtp.StatusCode),
            _ => ex is IOException or SocketException,
        };
    }

    // The relay delivers another domain's sender as spoofed or rejects it, so those fall back to From.
    private MailAddress SenderAddress(EmailSender? from)
    {
        if (from is null)
            return new MailAddress(emailSettings.From);

        if (MailAddress.TryCreate(from.Address, from.DisplayName, out MailAddress? address)
            && emailSettings.SenderDomains.Contains(address.Host, StringComparer.OrdinalIgnoreCase))
        {
            return address;
        }

        logger.LogWarning("Sender address is not on an allowed domain; sending from the default address");
        return new MailAddress(emailSettings.From);
    }
}
