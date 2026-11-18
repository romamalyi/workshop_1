using FluentResults;

using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LeoHR.Server.Features.Promotions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromotionsController(IPromotionsService promotionsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PromotionDto>>> GetAll([FromQuery] PromotionFilterModel filter)
    {
        Result<PagedResult<PromotionDto>> result = await promotionsService.GetAll(User, filter);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<PromotionDto>> Create([FromBody] CreatePromotionRequest request)
    {
        Result<PromotionDto> result = await promotionsService.Create(request, User);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PromotionDetailDto>> GetDetail(int id)
    {
        Result<PromotionDetailDto> result = await promotionsService.GetDetailByPromotionId(id, User);
        return result.ToActionResult();
    }

    [HttpGet("active")]
    public async Task<ActionResult<PromotionDetailDto>> GetPromotionByUserId()
    {
        Result<PromotionDetailDto> result = await promotionsService.GetDetailForCurrentUser(User);
        return result.ToActionResult();
    }

    [HttpGet("my-level")]
    public async Task<ActionResult<EmployeeLevelDto>> GetMyLevel()
    {
        Result<EmployeeLevelDto> result = await promotionsService.GetCurrentUserLevel(User);
        return result.ToActionResult();
    }

    [HttpGet("active/{employeeId}")]
    public async Task<ActionResult<PromotionDetailDto?>> GetActivePromotionForEmployee(string employeeId)
    {
        Result<PromotionDetailDto?> result = await promotionsService.GetActiveDetailForEmployee(employeeId, User);
        return result.ToActionResult();
    }

    [HttpGet("by-company-id/{companyId}")]
    public async Task<ActionResult<PromotionDetailDto?>> GetByCompanyId(string companyId)
    {
        Result<PromotionDetailDto?> result = await promotionsService.GetByCompanyId(companyId, User);
        return result.ToActionResult();
    }

    [HttpGet("target-levels")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<PromotionTargetLevelsDto>> GetTargetLevels(
        [FromQuery, BindRequired] string employeeId)
    {
        Result<PromotionTargetLevelsDto> result = await promotionsService.GetTargetLevels(employeeId);
        return result.ToActionResult();
    }

    [HttpGet("pending-approvals")]
    [Authorize(Policy = nameof(Policies.ManageRead))]
    public async Task<ActionResult<List<PendingApprovalDto>>> GetPendingApprovals(
        [FromQuery] List<string>? hrManagers)
    {
        Result<List<PendingApprovalDto>> result = await promotionsService.GetPendingApprovals(hrManagers);
        return result.ToActionResult();
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> Update(int id, [FromBody] UpdatePromotionRequest request)
    {
        Result result = await promotionsService.Update(id, request);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/email-recipients")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<EmailRecipients>> GetEmailRecipients(int id)
    {
        Result<EmailRecipients> result = await promotionsService.GetEmailRecipients(id);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/send")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> SendPromotionEmail(int id, [FromBody] SendPromotionRequest? request)
    {
        Result result = await promotionsService.SendPromotionEmail(id, request ?? new SendPromotionRequest(), User);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/criteria")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> SetCriterion(int id, [FromBody] SetCriterionRequest request)
    {
        Result result = await promotionsService.SetCriterion(id, request);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/criteria-verification")]
    public async Task<ActionResult> VerifyCriteria(int id)
    {
        Result result = await promotionsService.VerifyCriteria(id, User);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/activities")]
    public async Task<ActionResult<PromotionActivityDto>> AddActivity(int id, [FromBody] AddActivityRequest request)
    {
        Result<PromotionActivityDto> result = await promotionsService.AddActivity(id, request, User);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/activities/batch")]
    public async Task<ActionResult<AddActivitiesResult>> AddActivities([FromBody] int[] activityIds, int id)
    {
        Result<AddActivitiesResult> result = await promotionsService.AddActivitiesToPromotion(activityIds, id, User);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}/activities/{employeeActivityId:int}")]
    public async Task<ActionResult> RemoveActivity(int id, int employeeActivityId)
    {
        Result result = await promotionsService.RemoveActivity(id, employeeActivityId, User);
        return result.ToActionResult();
    }

    [HttpPatch("{id:int}/activities/{employeeActivityId:int}/approve")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> ApproveActivity(int id, int employeeActivityId, [FromBody] ApproveActivityRequest request)
    {
        Result result = await promotionsService.ApproveActivity(
            id, employeeActivityId, request.AccomplishedOn!.Value, request.ApprovedById, User);
        return result.ToActionResult();
    }

    [HttpPatch("{id:int}/activities/{employeeActivityId:int}/revert-approval")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> RevertActivityApproval(int id, int employeeActivityId)
    {
        Result result = await promotionsService.RevertActivityApproval(id, employeeActivityId);
        return result.ToActionResult();
    }
}
