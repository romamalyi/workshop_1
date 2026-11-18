using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Shared.Models;

public record AddAuthenticationArgs
{
    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string ApiName { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; }
}
