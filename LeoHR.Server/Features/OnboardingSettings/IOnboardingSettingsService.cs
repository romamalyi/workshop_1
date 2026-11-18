using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.OnboardingSettings.Dto;
using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingSettings;

public interface IOnboardingSettingsService
{
    Task<List<ResponsibleRoleDto>> GetResponsibleRoles();
    List<ResponsibleRoleOptionDto> GetTemplateRoleOptions();
    Task<Result> SaveResponsibleRoles(SaveResponsibleRolesRequest request, ClaimsPrincipal user);
    Task<Result<ResponsiblePositionsDto>> GetResponsiblePositions();
    Task<Result> SaveResponsiblePositions(SaveResponsiblePositionsRequest request);
    Task<Result<OnboardingRemindersDto>> GetReminders();
    Task<Result> SaveReminders(SaveOnboardingRemindersRequest request);
    Task<Result<OnboardingPhasesDto>> GetPhases();
    Task<Result> SavePhases(SaveOnboardingPhasesRequest request);
}
