namespace LeoHR.Server.Shared.Email;

public interface IEmailWhitelist
{
    /// Case-insensitive set of addresses currently allowed to receive mail.
    Task<IReadOnlySet<string>> GetAllowedAsync(CancellationToken cancellationToken = default);
}
