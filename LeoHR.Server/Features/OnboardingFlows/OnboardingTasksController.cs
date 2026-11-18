using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.OnboardingFlows;

// The service scopes the tasks to the caller unless they can manage.
[ApiController]
[Route("api/onboarding/tasks")]
[Authorize]
public class OnboardingTasksController(IOnboardingFlowsService service) : ControllerBase
{
    [HttpGet("has-any")]
    public Task<bool> HasAssignedTasks()
    {
        return service.HasAssignedTasks(User);
    }

    [HttpGet]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public Task<PagedResult<OnboardingTaskFlowDto>> GetMyTasks([FromQuery] OnboardingTaskFilterModel filter)
    {
        return service.GetMyTasks(filter, User);
    }

    [HttpGet("summary")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public Task<OnboardingTasksSummaryDto> GetMyTasksSummary([FromQuery] OnboardingTaskFilterModel filter)
    {
        return service.GetMyTasksSummary(filter, User);
    }

    [HttpGet("person-field-candidates")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult<List<EmployeeCandidateDto>>> GetPersonFieldCandidates([FromQuery] string? restrictionKey)
    {
        Result<List<EmployeeCandidateDto>> result = await service.GetPersonFieldCandidates(restrictionKey, User);
        return result.ToActionResult();
    }

    [HttpGet("{taskId:int}")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult<OnboardingTaskDetailDto>> GetTask(int taskId)
    {
        Result<OnboardingTaskDetailDto> result = await service.GetTask(taskId, User);
        return result.ToActionResult();
    }

    [HttpPatch("{taskId:int}/fields")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult> UpdateFields(int taskId, UpdateTaskFieldsRequest request)
    {
        Result result = await service.UpdateTaskFields(taskId, request, User);
        return result.ToActionResult();
    }

    [HttpPost("{taskId:int}/start")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult> StartTask(int taskId)
    {
        Result result = await service.StartTask(taskId, User);
        return result.ToActionResult();
    }

    [HttpPost("{taskId:int}/undo-start")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult> UndoStartTask(int taskId)
    {
        Result result = await service.UndoStartTask(taskId, User);
        return result.ToActionResult();
    }

    [HttpPost("{taskId:int}/reopen")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult> ReopenTask(int taskId)
    {
        Result result = await service.ReopenTask(taskId, User);
        return result.ToActionResult();
    }

    [HttpPut("{taskId:int}/done")]
    [Authorize(Policy = nameof(Policies.OnboardingTasks))]
    public async Task<ActionResult> CompleteTask(int taskId)
    {
        Result result = await service.CompleteTask(taskId, User);
        return result.ToActionResult();
    }
}
