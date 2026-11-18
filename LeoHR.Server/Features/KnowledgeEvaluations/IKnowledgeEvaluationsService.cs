using System.Security.Claims;

using FluentResults;

namespace LeoHR.Server.Features.KnowledgeEvaluations;

public interface IKnowledgeEvaluationsService
{
    Task<Result<List<KnowledgeEvaluationDto>>> GetAll();
    Task<Result<KnowledgeEvaluationDto?>> GetLatestForPromotion(int promotionId, ClaimsPrincipal user);
    Task<Result<KnowledgeEvaluationDto>> Create(
        int promotionId, RequestKnowledgeEvaluationRequest request, ClaimsPrincipal user);
    Task<Result<KnowledgeEvaluationDto>> UpdateRequest(
        int id, RequestKnowledgeEvaluationRequest request, ClaimsPrincipal user);
    Task<Result<KnowledgeEvaluationDto>> Update(int id, UpdateKnowledgeEvaluationRequest request);
}
