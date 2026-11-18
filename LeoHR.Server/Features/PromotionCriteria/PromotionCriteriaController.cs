using FluentResults;

using LeoHR.Server.Shared;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.PromotionCriteria;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.PromotionCriteria;

[ApiController]
[Route("api/promotion-criteria")]
[Authorize]
public class PromotionCriteriaController(IPromotionCriteriaService service) : ControllerBase
{
    [HttpGet]
    public Task<List<PromotionCriteriaDto>> GetAll([FromQuery] string? technology, [FromQuery] bool includeInactive = false)
    {
        return service.GetAll(technology, includeInactive);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PromotionCriteriaDto>> GetById(int id)
    {
        Result<PromotionCriteriaDto> result = await service.GetById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<PromotionCriteriaDto>> Create([FromBody] PromotionCriteriaRequest request)
    {
        Result<PromotionCriteriaDto> result = await service.Create(request);
        if (result.IsFailed) return result.ToActionResult();
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<PromotionCriteriaDto>> Update(int id, [FromBody] PromotionCriteriaRequest request, [FromQuery] EditMode mode = EditMode.Soft)
    {
        Result<PromotionCriteriaDto> result = await service.Update(id, request, mode);
        return result.ToActionResult();
    }

    [HttpPatch("{id}/reactivate")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<PromotionCriteriaDto>> Reactivate(int id)
    {
        Result<PromotionCriteriaDto> result = await service.Reactivate(id);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult> Delete(int id)
    {
        Result result = await service.Delete(id);
        return result.ToActionResult();
    }
}
