using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Shared.Promotions;

/// Implemented by Features/Promotions/PromotionCriteriaHistoryService.
public interface IPromotionCriteriaHistory
{
    /// Links the promotions that read this criteria through its level to it, so a new version leaves them unchanged, and saves them.
    Task PreserveAsync(PromotionCriteriaEntity criteria);

    Task<bool> PreserveSentAsync(PromotionCriteriaEntity criteria);
}
