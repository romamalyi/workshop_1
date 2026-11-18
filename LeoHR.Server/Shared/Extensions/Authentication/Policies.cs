using LeoHR.Server.Shared.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Shared.Extensions.Authentication;

public static class ClaimType
{
    public const string LeoHRManage = nameof(LeoHRManage);
    public const string LeoHROnboarding = nameof(LeoHROnboarding);
}

public static class Scopes
{
    public const string Integrations = "leohr.integrations";
}
[Flags]
internal enum ClaimValue
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write,
}

public static class Policies
{
    public static AuthorizationPolicy Default { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.GetUserId() is not null)
                                        .Build();

    public static AuthorizationPolicy Integrations { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireClaim("scope", Scopes.Integrations)
                                        .Build();

    public static AuthorizationPolicy Manage { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.CanWriteManage())
                                        .Build();

    public static AuthorizationPolicy ManageRead { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.CanReadManage())
                                        .Build();

    public static AuthorizationPolicy ManageAdmin { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.CanManageAdmin())
                                        .Build();

    // Assignees without the claim pass too; the service limits them to their own tasks.
    public static AuthorizationPolicy OnboardingTasks { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(async context => context.User.CanReadOnboarding() || await HasAssignedTasks(context))
                                        .Build();

    public static AuthorizationPolicy OnboardingWrite { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.CanWriteOnboarding())
                                        .Build();

    public static AuthorizationPolicy OnboardingAdmin { get; } =
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                        .RequireAssertion(context => context.User.IsOnboardingAdmin())
                                        .Build();

    private static async Task<bool> HasAssignedTasks(AuthorizationHandlerContext context)
    {
        string? userId = context.User.GetUserId();
        if (userId is null || context.Resource is not HttpContext httpContext)
        {
            return false;
        }

        LeoHrDbContext db = httpContext.RequestServices.GetRequiredService<LeoHrDbContext>();
        return await db.OnboardingFlowTasks.AnyAsync(t => t.AssigneeId == userId);
    }
}
