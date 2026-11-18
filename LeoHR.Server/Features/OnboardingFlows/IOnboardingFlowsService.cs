using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Shared.Models.Filter;
using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingFlows;

public interface IOnboardingFlowsService
{
    Task<List<OnboardingFlowDto>> GetAll();

    Task<Result<OnboardingFlowDto>> GetById(int id);

    Task<Result> ChangeResponsible(int id, ChangeResponsibleRequest request);

    Task<Result<OnboardingFlowDto>> CreateFlow(CreateOnboardingFlowRequest request);

    Task<List<UnassignedTaskDto>> GetUnassignedTasks();

    Task<Result> AssignTask(int taskId, AssignTaskRequest request, ClaimsPrincipal user);

    Task<bool> HasAssignedTasks(ClaimsPrincipal user);

    Task<Result<List<EmployeeCandidateDto>>> GetPersonFieldCandidates(string? restrictionKey, ClaimsPrincipal user);

    Task<PagedResult<OnboardingTaskFlowDto>> GetMyTasks(OnboardingTaskFilterModel filter, ClaimsPrincipal user);

    Task<OnboardingTasksSummaryDto> GetMyTasksSummary(OnboardingTaskFilterModel filter, ClaimsPrincipal user);

    Task<Result<OnboardingTaskDetailDto>> GetTask(int taskId, ClaimsPrincipal user);

    Task<Result> UpdateTaskFields(int taskId, UpdateTaskFieldsRequest request, ClaimsPrincipal user);

    Task<Result> StartTask(int taskId, ClaimsPrincipal user);

    Task<Result> UndoStartTask(int taskId, ClaimsPrincipal user);

    Task<Result> CompleteTask(int taskId, ClaimsPrincipal user);

    Task<Result> ReopenTask(int taskId, ClaimsPrincipal user);

    Task<List<RecheckAssignmentDto>> PreviewRecheck();

    Task<ApplyRecheckResultDto> ApplyRecheck(ApplyRecheckRequest request);
}
