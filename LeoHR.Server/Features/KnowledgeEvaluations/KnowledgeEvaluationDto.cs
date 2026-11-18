using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.KnowledgeEvaluations;

public record KnowledgeEvaluationDto(
    int Id,
    int PromotionId,
    string EmployeeId,
    string EmployeeName,
    DateOnly RequestedDate,
    KnowledgeEvaluationOption RequestedOption,
    string? Comment,
    string? ConductedById,
    string? ConductedByName,
    DateOnly? EvaluationDate,
    KnowledgeEvaluationStatus Status,
    string? Link
);

public record RequestKnowledgeEvaluationRequest(
    DateOnly RequestedDate,
    KnowledgeEvaluationOption RequestedOption,
    [MaxLength(KnowledgeEvaluationEntity.CommentMaxLength)] string? Comment = null
);

public record UpdateKnowledgeEvaluationRequest(
    KnowledgeEvaluationStatus Status,
    [MaxLength(100)] string? ConductedById = null,
    DateOnly? EvaluationDate = null,
    [MaxLength(KnowledgeEvaluationEntity.LinkMaxLength)] string? Link = null
);
