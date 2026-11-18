using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class KnowledgeEvaluationEntity : BaseAuditableEntity
{
    public const int CommentMaxLength = 1000;
    public const int LinkMaxLength = 500;

    public int Id { get; set; }
    public int PromotionId { get; set; }
    public DateOnly RequestedDate { get; set; }
    public KnowledgeEvaluationOption RequestedOption { get; set; }
    public string? Comment { get; set; }
    public string? ConductedById { get; set; }
    public DateOnly? EvaluationDate { get; set; }
    public KnowledgeEvaluationStatus Status { get; set; } = KnowledgeEvaluationStatus.Planned;
    public string? Link { get; set; }

    public PromotionEntity Promotion { get; set; } = null!;
}
