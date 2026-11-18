using FluentResults;

using LeoHR.Server.Shared;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.Activities;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivitiesController(IActivitiesService service) : ControllerBase
{
    [HttpGet]
    public Task<List<ActivityDto>> GetAll([FromQuery] string? category, [FromQuery] bool includeInactive = false)
    {
        return service.GetAll(category, includeInactive);
    }

    [HttpGet("group")]
    public Task<List<CategorizedActivity>> GetAllGrouped()
    {
        return service.GetGroupedActivities();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ActivityDto>> GetById(int id)
    {
        Result<ActivityDto> result = await service.GetById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<ActivityDto>> Create([FromBody] CreateActivityRequest request)
    {
        Result<ActivityDto> result = await service.Create(request);
        if (result.IsFailed) return result.ToActionResult();
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<ActivityDto>> Update(int id, [FromBody] UpdateActivityRequest request, [FromQuery] EditMode mode = EditMode.Soft)
    {
        Result<ActivityDto> result = await service.Update(id, request, mode);
        return result.ToActionResult();
    }

    [HttpPatch("{id}/reactivate")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult<ActivityDto>> Reactivate(int id)
    {
        Result<ActivityDto> result = await service.Reactivate(id);
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
