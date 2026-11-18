using FluentResults;

using LeoHR.Server.Features.OnboardingSettings.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.OnboardingSettings;

[ApiController]
[Route("api/onboarding/settings")]
[Authorize(Policy = nameof(Policies.OnboardingWrite))]
public class OnboardingSettingsController(IOnboardingSettingsService service) : ControllerBase
{
    [HttpGet("responsible-roles")]
    public async Task<ActionResult<List<ResponsibleRoleDto>>> GetResponsibleRoles()
    {
        List<ResponsibleRoleDto> roles = await service.GetResponsibleRoles();
        return Ok(roles);
    }

    [HttpGet("responsible-roles/options")]
    public ActionResult<List<ResponsibleRoleOptionDto>> GetTemplateRoleOptions()
    {
        return Ok(service.GetTemplateRoleOptions());
    }

    [HttpPut("responsible-roles")]
    public async Task<ActionResult> SaveResponsibleRoles([FromBody] SaveResponsibleRolesRequest request)
    {
        Result result = await service.SaveResponsibleRoles(request, User);
        return result.ToActionResult();
    }

    [HttpGet("responsible-positions")]
    [Authorize(Policy = nameof(Policies.OnboardingAdmin))]
    public async Task<ActionResult<ResponsiblePositionsDto>> GetResponsiblePositions()
    {
        Result<ResponsiblePositionsDto> result = await service.GetResponsiblePositions();
        return result.ToActionResult();
    }

    [HttpPut("responsible-positions")]
    [Authorize(Policy = nameof(Policies.OnboardingAdmin))]
    public async Task<ActionResult> SaveResponsiblePositions([FromBody] SaveResponsiblePositionsRequest request)
    {
        Result result = await service.SaveResponsiblePositions(request);
        return result.ToActionResult();
    }

    [HttpGet("reminders")]
    public async Task<ActionResult<OnboardingRemindersDto>> GetReminders()
    {
        Result<OnboardingRemindersDto> result = await service.GetReminders();
        return result.ToActionResult();
    }

    [HttpPut("reminders")]
    public async Task<ActionResult> SaveReminders([FromBody] SaveOnboardingRemindersRequest request)
    {
        Result result = await service.SaveReminders(request);
        return result.ToActionResult();
    }

    [HttpGet("phases")]
    public async Task<ActionResult<OnboardingPhasesDto>> GetPhases()
    {
        Result<OnboardingPhasesDto> result = await service.GetPhases();
        return result.ToActionResult();
    }

    [HttpPut("phases")]
    public async Task<ActionResult> SavePhases([FromBody] SaveOnboardingPhasesRequest request)
    {
        Result result = await service.SavePhases(request);
        return result.ToActionResult();
    }
}
