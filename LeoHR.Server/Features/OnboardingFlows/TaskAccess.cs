using System.Linq.Expressions;
using System.Security.Claims;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Extensions.Authentication;

namespace LeoHR.Server.Features.OnboardingFlows;

internal static class TaskAccess
{
    public static bool SeesAllTasks(ClaimsPrincipal user)
    {
        return user.IsOnboardingAdmin();
    }

    // Only for a caller with an id and without SeesAllTasks; a writer also sees the tasks of the flows they are responsible for.
    public static Expression<Func<OnboardingFlowTaskEntity, bool>> IsVisibleTo(ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        bool canWrite = user.CanWriteOnboarding();
        return t => t.AssigneeId == userId || (canWrite && t.Flow.ResponsiblePersonId == userId);
    }

    public static bool CanAccess(OnboardingFlowTaskEntity task, ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        return SeesAllTasks(user) || (userId is not null && IsVisibleTo(user).Compile()(task));
    }

    public static bool IsAdminOrAssignee(OnboardingFlowTaskEntity task, ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        return user.IsOnboardingAdmin() || (userId is not null && task.AssigneeId == userId);
    }

    public static bool CanEdit(OnboardingFlowTaskEntity task, ClaimsPrincipal user)
    {
        return task.AssigneeId is not null && IsAdminOrAssignee(task, user);
    }

    // Any Onboarding writer may fill an empty assignee; replacing one is reserved to the flow's responsible person and Admins.
    public static bool CanChangeAssignee(OnboardingFlowTaskEntity task, ClaimsPrincipal user)
    {
        if (!user.CanWriteOnboarding())
        {
            return false;
        }

        string? userId = user.GetUserId();
        return task.AssigneeId is null
            || user.IsOnboardingAdmin()
            || (userId is not null && task.Flow.ResponsiblePersonId == userId);
    }
}
