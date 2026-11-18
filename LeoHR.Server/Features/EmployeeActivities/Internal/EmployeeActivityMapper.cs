using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.EmployeeActivities.Internal;

internal static class EmployeeActivityMapper
{
    public static EmployeeActivityDto MapToDto(EmployeeActivityEntity e)
    {
        return new(e.Id, e.EmployeeId, e.ActivityId,
            e.Status, e.AccomplishedOn, e.Note,
            e.PromotionId, e.PointsSnapshot,
            e.CreatedById, e.ApprovedById, e.ApprovedByName, e.ApprovedAt,
            e.CreatedAt, e.UpdatedAt,
            e.Promotion is null ? null : new ActivityPromotionInfo(e.Promotion.FromRole, e.Promotion.ToRole));
    }
}
