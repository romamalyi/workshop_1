using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingSettings.Dto;

public record ResponsibleRoleDto(
    string RoleKey,
    string RoleName,
    string? DefaultEmployeeId,
    int CandidatesCount,
    bool DefaultInactive,
    List<EmployeeCandidateDto> Candidates
);

public record RoleMappingInput(
    [Required] string RoleKey,
    string? DefaultEmployeeId
);

public record SaveResponsibleRolesRequest(
    [Required] List<RoleMappingInput> Mappings
);

public record PositionOptionDto(string Name, int EmployeeCount);

public record ResponsiblePositionsDto(
    List<PositionOptionDto> Allowed,
    List<PositionOptionDto> Available,
    List<EmployeeCandidateDto> Candidates,
    int FlowsWithWarningsCount
);

public record SaveResponsiblePositionsRequest(
    [Required][MinLength(1)] List<string> Positions
);

public record OnboardingRemindersDto(
    int DaysBeforeStartToNotify,
    bool WeeklyDigestEnabled,
    bool OverdueAlertsEnabled
);

public record SaveOnboardingRemindersRequest(
    [AllowedValues(7, 14, 30)] int DaysBeforeStartToNotify,
    bool WeeklyDigestEnabled,
    bool OverdueAlertsEnabled
);

public record OnboardingPhasesDto(
    int ProbationMonths,
    int MaxOnboardingHours
);

public record SaveOnboardingPhasesRequest(
    [AllowedValues(1, 3, 6)] int ProbationMonths,
    [Range(1, 200)] int MaxOnboardingHours
);
