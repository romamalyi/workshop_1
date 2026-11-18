namespace LeoHR.Server.Shared.Employees;

/// <summary>Binds the <c>EmployeeApi</c> configuration section.</summary>
public sealed class EmployeeApiOptions
{
    public const string SectionName = "EmployeeApi";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int CacheExpirationMinutes { get; init; } = 60;
    public EmployeeApiAuthOptions Auth { get; init; } = new();
}

public sealed class EmployeeApiAuthOptions
{
    public string TokenEndpoint { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Lives in <c>secrets.{Environment}.json</c>, never committed.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    public string Scope { get; init; } = "employee";
}
