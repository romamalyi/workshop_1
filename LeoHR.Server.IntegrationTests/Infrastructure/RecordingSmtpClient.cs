using System.Collections.Concurrent;

using FluentResults;

using LeoHR.Server.Shared.Email;

namespace LeoHR.Server.IntegrationTests.Infrastructure;

// Keeps every email in memory, so a test run can never reach a real mail server.
public sealed class RecordingSmtpClient : ISmtpHttpClient
{
    public ConcurrentQueue<SentEmail> Sent { get; } = new();

    // Widens the window between a send's start and its record, so overlapping requests really overlap.
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public async Task<Result> SendAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        bool isHtml = false,
        IEnumerable<string>? cc = null,
        EmailSender? from = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        Sent.Enqueue(new SentEmail([.. recipients], subject, [.. cc ?? []], body));
        return Result.Ok();
    }
}

public sealed record SentEmail(List<string> Recipients, string Subject, List<string> Cc, string Body);
