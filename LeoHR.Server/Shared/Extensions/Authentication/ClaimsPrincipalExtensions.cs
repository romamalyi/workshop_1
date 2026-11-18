using System.Security.Claims;

namespace LeoHR.Server.Shared.Extensions.Authentication;

public static class ClaimsPrincipalExtensions
{
    private static readonly ClaimValue[] anyAccess = [ClaimValue.Read, ClaimValue.Write, ClaimValue.ReadWrite];
    private static readonly ClaimValue[] writeAccess = [ClaimValue.Write, ClaimValue.ReadWrite];

    public static bool CanReadManage(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHRManage, anyAccess);
    }

    public static bool CanWriteManage(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHRManage, writeAccess);
    }

    public static bool CanManageAdmin(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHRManage, [ClaimValue.ReadWrite]);
    }

    public static bool CanReadOnboarding(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHROnboarding, anyAccess);
    }

    public static bool CanWriteOnboarding(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHROnboarding, writeAccess);
    }

    public static bool IsOnboardingAdmin(this ClaimsPrincipal user)
    {
        return user.HasAccess(ClaimType.LeoHROnboarding, [ClaimValue.ReadWrite]);
    }

    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // IdentityServer puts the id of the requesting app in every access token, not only client-credentials ones.
    public static string? GetClientId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("client_id");
    }

    public static string? GetUserFullName(this ClaimsPrincipal user)
    {
        string? firstName = user.FindFirstValue(ClaimTypes.GivenName) ?? user.FindFirstValue("given_name");
        string? lastName = user.FindFirstValue(ClaimTypes.Surname) ?? user.FindFirstValue("family_name");
        string fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static bool HasAccess(this ClaimsPrincipal user, string claimType, ClaimValue[] levels)
    {
        return user.HasClaim(c => c.Type == claimType && levels.Any(level => c.Value == $"{level:D}"));
    }
}
