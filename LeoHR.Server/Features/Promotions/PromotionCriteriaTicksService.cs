using LeoHR.Server.Features.Promotions.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.PromotionCriteria;
using LeoHR.Server.Shared.Promotions;

namespace LeoHR.Server.Features.Promotions;

public class PromotionCriteriaTicksService(LeoHrDbContext db) : IPromotionCriteriaTicks
{
    public async Task SyncScoreAsync(int promotionId)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return;

        await SyncScore(promotion);
        await db.SaveChangesAsync();
    }

    // Approved points affect only the score, so the English level and its Employee API call are left out.
    public Task<bool> SyncScore(PromotionEntity promotion)
    {
        return SyncAutoCriteria(promotion, emp: null);
    }

    // Returns false when the promotion is sent or its level has no criteria, so there is nothing to sync.
    public async Task<bool> SyncAutoCriteria(PromotionEntity promotion, EmployeeRef? emp)
    {
        if (promotion.Status == PromotionStatus.Sent) return false;

        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        if (criteria is null) return false;

        if (promotion.CriteriaOverrides.Any(o => o.Label == CriteriaLabels.ActivityScore))
            promotion.CriteriaOverrides = [.. promotion.CriteriaOverrides.Where(o => o.Label != CriteriaLabels.ActivityScore)];

        int approvedPoints = await ApprovedPoints(promotion);
        EnglishLevel? englishLevel = PromotionMapper.ResolveEnglishLevelEnum(emp);

        promotion.ApprovedCriteriaLabels =
            [.. PromotionRules.EvaluateCriteria(promotion.ApprovedCriteriaLabels, criteria, approvedPoints, englishLevel)];

        PromotionRules.SyncStatus(promotion, criteria);
        return true;
    }

    private async Task<int> ApprovedPoints(PromotionEntity promotion)
    {
        DateTime refDate = promotion.ReferenceDate ?? DateTime.UtcNow;
        List<EmployeeActivityEntity> approved = await db.EmployeeActivities.ApprovedInPromotionAsync(promotion.Id);
        return PromotionRules.GetValidApprovedActivities(approved, refDate, promotion.LevelStartDate)
            .Sum(a => a.PointsSnapshot ?? 0);
    }
}
