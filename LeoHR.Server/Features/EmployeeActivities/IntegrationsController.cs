using FluentResults;

using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.EmployeeActivities;

[ApiController]
[Route("api/integrations")]
[Authorize(Policy = nameof(Policies.Integrations))]
public class IntegrationsController(IEmployeeActivitiesService service) : ControllerBase
{
    [HttpGet("activities")]
    public Task<List<IntegrationActivityDto>> GetActivities()
    {
        return service.GetIntegrationActivities();
    }

    [HttpPost("employee-activities")]
    public async Task<ActionResult> RecordEmployeeActivity([FromBody] RecordEmployeeActivityRequest request)
    {
        Result result = await service.RecordEmployeeActivity(request, User);
        return result.IsFailed ? result.ToActionResult() : StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("employee-activities")]
    public async Task<ActionResult<List<IntegrationEmployeeActivityDto>>> GetEmployeeActivities(
        [FromQuery] IntegrationEmployeeActivitiesFilter filter)
    {
        Result<List<IntegrationEmployeeActivityDto>> result = await service.GetIntegrationEmployeeActivities(filter, User);
        return result.ToActionResult();
    }

    [HttpPut("employee-activities/{id:int}")]
    public async Task<ActionResult> UpdateEmployeeActivity(int id, [FromBody] UpdateIntegrationEmployeeActivityRequest request)
    {
        Result result = await service.UpdateIntegrationEmployeeActivity(id, request, User);
        return result.ToActionResult();
    }
}
