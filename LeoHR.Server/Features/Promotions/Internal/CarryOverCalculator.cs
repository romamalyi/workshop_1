using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class CarryOverCalculator
{
    /// <param name="validApprovedActivities">
    /// The previous promotion's approved activities already filtered to those valid within its
    /// window (see <see cref="PromotionRules.GetValidApprovedActivities"/>). Passing the same list
    /// used to compute the surplus keeps the carried amount consistent with it.
    /// </param>
    public static List<(EmployeeActivityEntity Source, int Points)> SelectCarryOver(
        int surplus,
        IEnumerable<EmployeeActivityEntity> validApprovedActivities,
        IReadOnlyCollection<int> requiredActivityIds)
    {
        IOrderedEnumerable<EmployeeActivityEntity> candidates = validApprovedActivities
            .Where(a => a.Activity.IsActive
                     && (a.PointsSnapshot ?? 0) > 0
                     && !PromotionRules.IsRequiredActivity(a, requiredActivityIds))
            .OrderByDescending(a => PromotionRules.LifetimeRank(a.Activity.WithinPeriod))
            .ThenByDescending(a => a.PointsSnapshot ?? 0);

        var carried = new List<(EmployeeActivityEntity Source, int Points)>();
        int accumulated = 0;
        foreach (EmployeeActivityEntity src in candidates)
        {
            int remaining = surplus - accumulated;
            if (remaining <= 0) break;

            int points = Math.Min(src.PointsSnapshot ?? 0, remaining);
            carried.Add((src, points));
            accumulated += points;
        }
        return carried;
    }
}
