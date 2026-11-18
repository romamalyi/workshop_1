using System.Security.Claims;
using System.Text.Encodings.Web;

using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace LeoHR.Server.IntegrationTests.Infrastructure;

// Stands in for IdentityServer: the caller is described by a request header, and the app's real policies judge it.
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out StringValues header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var user = TestUser.FromHeader(header.ToString());
        List<Claim> claims = [new("sub", user.Id)];
        if (user.Manage != Access.None)
        {
            claims.Add(new Claim(ClaimType.LeoHRManage, ((int)user.Manage).ToString()));
        }

        if (user.Onboarding != Access.None)
        {
            claims.Add(new Claim(ClaimType.LeoHROnboarding, ((int)user.Onboarding).ToString()));
        }

        if (user.Integrations)
        {
            claims.Add(new Claim("scope", Scopes.Integrations));
        }

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

// Mirrors the claim values IdentityServer issues for LeoHRManage / LeoHROnboarding.
public enum Access
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = 3,
}

public sealed record TestUser(
    string Id, Access Manage = Access.None, Access Onboarding = Access.None, bool Integrations = false)
{
    public static TestUser HrAdmin(string id)
    {
        return new(id, Manage: Access.ReadWrite);
    }

    public static TestUser OnboardingAdmin(string id)
    {
        return new(id, Onboarding: Access.ReadWrite);
    }

    public static TestUser OnboardingWriter(string id)
    {
        return new(id, Onboarding: Access.Write);
    }

    // A service client such as the English app: it holds the integrations scope, not a person's claims.
    public static TestUser IntegrationClient(string id)
    {
        return new(id, Integrations: true);
    }

    public static TestUser Employee(string id)
    {
        return new(id);
    }

    public string ToHeader()
    {
        return $"{Id};{(int)Manage};{(int)Onboarding};{Integrations}";
    }

    public static TestUser FromHeader(string header)
    {
        string[] parts = header.Split(';');
        return new TestUser(parts[0], (Access)int.Parse(parts[1]), (Access)int.Parse(parts[2]), bool.Parse(parts[3]));
    }
}
