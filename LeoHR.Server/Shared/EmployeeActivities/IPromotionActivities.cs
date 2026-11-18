using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Shared.EmployeeActivities;

/// Implemented by Features/EmployeeActivities/PromotionActivitiesService. Each method saves its own change.
public interface IPromotionActivities
{
    Task<List<EmployeeActivityEntity>> AddPlannedAsync(
        PromotionEntity promotion,
        IEnumerable<ActivityEntity> activities,
        string? note);
    Task<Result> RemovePlannedAsync(int promotionId, int employeeActivityId, ClaimsPrincipal user);
    Task<Result> ApproveAsync(
        string employeeId,
        int employeeActivityId,
        DateOnly accomplishedOn,
        string? approvedById,
        string? approvedByName);
    Task<Result> RevertApprovalAsync(int promotionId, int employeeActivityId);
    Task AddCarryOverAsync(
        PromotionEntity toPromotion,
        int fromPromotionId,
        IEnumerable<(EmployeeActivityEntity Source, int Points)> carried);
    Task AdoptOrphanedAsync(PromotionEntity promotion);
    Task DeleteAllForPromotionsAsync(IReadOnlyCollection<int> promotionIds);
}
