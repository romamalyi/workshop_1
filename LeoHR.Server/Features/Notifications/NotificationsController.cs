using FluentResults;

using LeoHR.Server.Features.Notifications.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.Notifications;

[ApiController]
[Route("api/integrations")]
[Authorize(Policy = nameof(Policies.Integrations))]
public class NotificationsController(INotificationsService service) : ControllerBase
{
    [HttpPost("events")]
    public async Task<ActionResult> ReportEmployeeEvent([FromBody] EmployeeEventRequest request)
    {
        Result result = await service.HandleEmployeeEvent(request);
        return result.IsFailed ? result.ToActionResult() : Accepted();
    }
}
