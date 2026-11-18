using FluentResults;

using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.FeatureFlags;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace LeoHR.Server.Features.KnowledgeEvaluations;

[ApiController]
[Route("api/knowledge-evaluations")]
[Authorize]
[FeatureGate(FeatureFlagNames.KnowledgeEvaluation)]
public class KnowledgeEvaluationsController(IKnowledgeEvaluationsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<List<KnowledgeEvaluationDto>>> GetAll()
    {
        Result<List<KnowledgeEvaluationDto>> result = await service.GetAll();
        return result.ToActionResult();
    }

    [HttpGet("promotion/{promotionId:int}")]
    public async Task<ActionResult<KnowledgeEvaluationDto?>> GetLatestForPromotion(int promotionId)
    {
        Result<KnowledgeEvaluationDto?> result = await service.GetLatestForPromotion(promotionId, User);
        return result.ToActionResult();
    }

    [HttpPost("promotion/{promotionId:int}")]
    public async Task<ActionResult<KnowledgeEvaluationDto>> Create(
        int promotionId, [FromBody] RequestKnowledgeEvaluationRequest request)
    {
        Result<KnowledgeEvaluationDto> result = await service.Create(promotionId, request, User);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/request")]
    public async Task<ActionResult<KnowledgeEvaluationDto>> UpdateRequest(
        int id, [FromBody] RequestKnowledgeEvaluationRequest request)
    {
        Result<KnowledgeEvaluationDto> result = await service.UpdateRequest(id, request, User);
        return result.ToActionResult();
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<KnowledgeEvaluationDto>> Update(
        int id, [FromBody] UpdateKnowledgeEvaluationRequest request)
    {
        Result<KnowledgeEvaluationDto> result = await service.Update(id, request);
        return result.ToActionResult();
    }
}
