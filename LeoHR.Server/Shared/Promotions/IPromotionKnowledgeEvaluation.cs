namespace LeoHR.Server.Shared.Promotions;

public record PromotionKeInfo(string EmployeeId, string PromotionHolderId, bool IsSent, bool RequiresKe, bool KeTicked);

/// Implemented by Features/Promotions/PromotionKnowledgeEvaluationService.
public interface IPromotionKnowledgeEvaluation
{
    /// Null when the promotion doesn't exist.
    Task<PromotionKeInfo?> GetInfoAsync(int promotionId);

    /// Ticks "KE required" on an open promotion, moves it to Ready when that closes its criteria, and saves it.
    Task TickKnowledgeEvaluationAsync(int promotionId);
}
