using FluentResults;

namespace LeoHR.Server.Shared.Email;

public interface ISmtpHttpClient
{
    Task<Result> SendAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        bool isHtml = false,
        IEnumerable<string>? cc = null,
        EmailSender? from = null,
        CancellationToken cancellationToken = default);
}
