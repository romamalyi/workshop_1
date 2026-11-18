using FluentResults;

using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.AppSettings;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = nameof(Policies.Manage))]
public class AppSettingsController(IAppSettingsFeatureService appSettingsService) : ControllerBase
{
    [HttpGet("employee-history")]
    public async Task<ActionResult<EmployeeHistorySettingsDto>> GetEmployeeHistorySettings()
    {
        Result<EmployeeHistorySettingsDto> result = await appSettingsService.GetEmployeeHistorySettings(User);
        return result.ToActionResult();
    }

    [HttpPut("employee-history")]
    public async Task<ActionResult> SaveEmployeeHistorySettings([FromBody] SaveEmployeeHistorySettingsRequest request)
    {
        Result result = await appSettingsService.SaveEmployeeHistorySettings(User, request);
        return result.ToActionResult();
    }

    [HttpGet("promotion-default-holder")]
    public async Task<ActionResult<PromotionDefaultHolderDto>> GetPromotionDefaultHolder()
    {
        Result<PromotionDefaultHolderDto> result = await appSettingsService.GetPromotionDefaultHolder(User);
        return result.ToActionResult();
    }

    [HttpPut("promotion-default-holder")]
    public async Task<ActionResult> SavePromotionDefaultHolder([FromBody] SavePromotionDefaultHolderRequest request)
    {
        Result result = await appSettingsService.SavePromotionDefaultHolder(User, request);
        return result.ToActionResult();
    }
}
