using LeoHR.Server.Features.Promotions.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.PromotionCriteria;
using LeoHR.Server.Shared.Promotions;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Promotions;

public class PromotionKnowledgeEvaluationService(LeoHrDbContext db, PromotionCriteriaTicksService criteriaTicks)
    : IPromotionKnowledgeEvaluation
{
    public async Task<PromotionKeInfo?> GetInfoAsync(int promotionId)
    {
        PromotionEntity? promotion = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == promotionId);
        if (promotion is null) return null;

        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        bool requiresKe = criteria is not null
            && PromotionRules.RequiredCriteriaLabels(criteria).Contains(CriteriaLabels.KeRequired);
        bool keTicked = promotion.ApprovedCriteriaLabels.Contains(CriteriaLabels.KeRequired);
        return new PromotionKeInfo(
            promotion.EmployeeId, promotion.PromotionHolderId, promotion.Status == PromotionStatus.Sent, requiresKe, keTicked);
    }

    public async Task TickKnowledgeEvaluationAsync(int promotionId)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null || promotion.Status == PromotionStatus.Sent) return;

        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        if (criteria is null || !PromotionRules.RequiredCriteriaLabels(criteria).Contains(CriteriaLabels.KeRequired)
            || promotion.ApprovedCriteriaLabels.Contains(CriteriaLabels.KeRequired))
        {
            return;
        }

        promotion.ApprovedCriteriaLabels = [.. promotion.ApprovedCriteriaLabels, CriteriaLabels.KeRequired];
        await criteriaTicks.SyncScore(promotion);
        await db.SaveChangesAsync();
    }
}
