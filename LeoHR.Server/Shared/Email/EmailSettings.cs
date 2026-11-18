using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Shared.Email;

public record EmailSettings
{
    [Required]
    public string From { get; init; } = string.Empty;

    // Domains the SMTP relay may send as; any other sender falls back to From.
    public string[] SenderDomains { get; init; } = [];

    [Required]
    public SmtpSettings SmtpSettings { get; init; } = new();

    public SmtpCredentials? Credentials { get; init; }

    // Only addresses in the email_whitelist table receive mail. On unless an environment opts out (Production),
    // so a forgotten setting can't mail real people.
    public bool WhitelistEnabled { get; init; } = true;

    [Required]
    public string AppBaseUrl { get; init; } = string.Empty;

    public string PromotionUrl(int promotionId)
    {
        return $"{AppBaseUrl.TrimEnd('/')}/promotions/{promotionId}";
    }
}

public record SmtpSettings
{
    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool EnableSsl { get; init; } = true;
}

public record SmtpCredentials
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
