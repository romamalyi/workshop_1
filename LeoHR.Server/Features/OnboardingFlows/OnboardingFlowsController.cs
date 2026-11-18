using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.OnboardingFlows;

[ApiController]
[Route("api/onboarding/flows")]
[Authorize(Policy = nameof(Policies.OnboardingWrite))]
public class OnboardingFlowsController(IOnboardingFlowsService service, IOnboardingCandidates candidateLookup) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OnboardingFlowDto>>> GetAll()
    {
        List<OnboardingFlowDto> flows = await service.GetAll();
        return Ok(flows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OnboardingFlowDto>> GetById(int id)
    {
        Result<OnboardingFlowDto> result = await service.GetById(id);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/responsible")]
    [Authorize(Policy = nameof(Policies.OnboardingAdmin))]
    public async Task<ActionResult> ChangeResponsible(int id, [FromBody] ChangeResponsibleRequest request)
    {
        Result result = await service.ChangeResponsible(id, request);
        return result.ToActionResult();
    }

    [HttpGet("responsible-candidates")]
    public async Task<ActionResult<List<ResponsibleCandidateDto>>> GetResponsibleCandidates()
    {
        List<ResponsibleCandidateDto> candidates = await candidateLookup.GetResponsibleCandidates();
        return Ok(candidates);
    }

    [HttpGet("assignee-candidates")]
    public async Task<ActionResult<List<EmployeeCandidateDto>>> GetAssigneeCandidates([FromQuery] string? roleKey)
    {
        List<EmployeeCandidateDto> candidates = await candidateLookup.GetAssigneeCandidates(roleKey);
        return Ok(candidates);
    }

    [HttpGet("unassigned-tasks")]
    public async Task<ActionResult<List<UnassignedTaskDto>>> GetUnassignedTasks()
    {
        List<UnassignedTaskDto> tasks = await service.GetUnassignedTasks();
        return Ok(tasks);
    }

    [HttpGet("unassigned-tasks/recheck")]
    public async Task<ActionResult<List<RecheckAssignmentDto>>> PreviewRecheck()
    {
        List<RecheckAssignmentDto> assignments = await service.PreviewRecheck();
        return Ok(assignments);
    }

    [HttpPost("unassigned-tasks/recheck")]
    public async Task<ActionResult<ApplyRecheckResultDto>> ApplyRecheck([FromBody] ApplyRecheckRequest request)
    {
        ApplyRecheckResultDto result = await service.ApplyRecheck(request);
        return Ok(result);
    }

    [HttpPut("tasks/{taskId:int}/assignee")]
    public async Task<ActionResult> AssignTask(int taskId, [FromBody] AssignTaskRequest request)
    {
        Result result = await service.AssignTask(taskId, request, User);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<OnboardingFlowDto>> Create([FromBody] CreateOnboardingFlowRequest request)
    {
        Result<OnboardingFlowDto> result = await service.CreateFlow(request);
        return result.ToActionResult();
    }
}
