namespace LeoHR.Server.Shared.Promotions;

/// Implemented by Features/Promotions/PromotionCriteriaTicksService.
public interface IPromotionCriteriaTicks
{
    /// Ticks or unticks the activity score of an open promotion from its approved points, and saves it.
    Task SyncScoreAsync(int promotionId);
}
