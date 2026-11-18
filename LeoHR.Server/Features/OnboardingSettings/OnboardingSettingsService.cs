using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.OnboardingSettings.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingSettings;

public class OnboardingSettingsService(
    LeoHrDbContext db,
    IEmployeeApiClient employeeApi,
    IOnboardingFlowWarnings flowWarnings) : IOnboardingSettingsService
{
    private const string OnlyAdminsChangeHrManagerDefault = "Only admins can change the default HR Manager.";

    public async Task<List<ResponsibleRoleDto>> GetResponsibleRoles()
    {
        Dictionary<string, string?> savedMappings = await db.ResponsibleRoleMappings
            .AsNoTracking()
            .ToDictionaryAsync(m => m.RoleKey, m => m.DefaultEmployeeId);

        IReadOnlyList<ApiEmployee> allEmployees = await employeeApi.GetAllAsync();
        List<ApiEmployee> activeEmployees = [.. allEmployees.Where(e => e.Active)];

        return [.. OnboardingRoles.MappableRoles
            .Select(roleDef => BuildRoleDto(roleDef, activeEmployees, savedMappings))];
    }

    public List<ResponsibleRoleOptionDto> GetTemplateRoleOptions()
    {
        return [.. OnboardingRoles.MappableRoles.Select(r => new ResponsibleRoleOptionDto(r.Key, r.ExactTitle))];
    }

    public async Task<Result<ResponsiblePositionsDto>> GetResponsiblePositions()
    {
        IReadOnlyList<ApiEmployee> allEmployees = await employeeApi.GetAllAsync();
        List<ApiEmployee> active = [.. allEmployees.Where(e => e.Active)];
        List<string> allowed = await LoadAllowedPositions();

        List<PositionOptionDto> available = [.. PersonRestrictions.Positions(active)
            .Select(p => new PositionOptionDto(p, active.Count(e => PersonRestrictions.Matches(e, p))))];

        List<PositionOptionDto> allowedOptions = [.. allowed
            .Select(p => available.FirstOrDefault(o => string.Equals(o.Name, p, StringComparison.OrdinalIgnoreCase)) ?? new PositionOptionDto(p, 0))
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)];

        List<ApiEmployee> allowedPeople = OnboardingCandidates.ActiveWithPosition(allEmployees, allowed);

        return Result.Ok(new ResponsiblePositionsDto(
            allowedOptions,
            available,
            OnboardingCandidates.ToCandidates(allowedPeople),
            await flowWarnings.CountFlowsWithWarningsAsync()));
    }

    public async Task<Result> SaveResponsiblePositions(SaveResponsiblePositionsRequest request)
    {
        List<string> positions = [.. request.Positions
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (positions.Count == 0)
        {
            return Result.Fail("At least one position is required.");
        }

        List<ResponsiblePositionEntity> existing = await db.ResponsiblePositions.ToListAsync();
        db.ResponsiblePositions.RemoveRange(existing.Where(e => !positions.Contains(e.Position, StringComparer.OrdinalIgnoreCase)));
        db.ResponsiblePositions.AddRange(positions
            .Where(p => !existing.Any(e => string.Equals(e.Position, p, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new ResponsiblePositionEntity { Position = p }));

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> SaveResponsibleRoles(SaveResponsibleRolesRequest request, ClaimsPrincipal user)
    {
        if (request.Mappings.Any(m => !OnboardingRoles.MappableRoles.Any(r => r.Key == m.RoleKey)))
        {
            return Result.Fail("One or more role keys are invalid.");
        }

        List<ResponsibleRoleMappingEntity> existing = await db.ResponsibleRoleMappings.ToListAsync();

        if (!user.IsOnboardingAdmin() && ChangesHrManagerDefault(existing, request.Mappings))
        {
            return Result.Fail(new ForbiddenError(OnlyAdminsChangeHrManagerDefault));
        }

        foreach (RoleMappingInput input in request.Mappings)
        {
            UpsertMapping(existing, input);
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<OnboardingRemindersDto>> GetReminders()
    {
        OnboardingSettingsEntity settings = await ReadSettings();
        return Result.Ok(new OnboardingRemindersDto(
            settings.DaysBeforeStartToNotify,
            settings.WeeklyDigestEnabled,
            settings.OverdueAlertsEnabled));
    }

    public async Task<Result> SaveReminders(SaveOnboardingRemindersRequest request)
    {
        OnboardingSettingsEntity settings = await GetOrAddSettings();
        settings.DaysBeforeStartToNotify = request.DaysBeforeStartToNotify;
        settings.WeeklyDigestEnabled = request.WeeklyDigestEnabled;
        settings.OverdueAlertsEnabled = request.OverdueAlertsEnabled;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<OnboardingPhasesDto>> GetPhases()
    {
        OnboardingSettingsEntity settings = await ReadSettings();
        return Result.Ok(new OnboardingPhasesDto(settings.ProbationMonths, settings.MaxOnboardingHours));
    }

    public async Task<Result> SavePhases(SaveOnboardingPhasesRequest request)
    {
        OnboardingSettingsEntity settings = await GetOrAddSettings();
        settings.ProbationMonths = request.ProbationMonths;
        settings.MaxOnboardingHours = request.MaxOnboardingHours;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<OnboardingSettingsEntity> ReadSettings()
    {
        return await db.OnboardingSettings.AsNoTracking().SingleOrDefaultAsync() ?? new OnboardingSettingsEntity();
    }

    private async Task<OnboardingSettingsEntity> GetOrAddSettings()
    {
        OnboardingSettingsEntity? settings = await db.OnboardingSettings.SingleOrDefaultAsync();
        if (settings is not null) return settings;

        settings = new OnboardingSettingsEntity();
        db.OnboardingSettings.Add(settings);
        return settings;
    }

    private static bool ChangesHrManagerDefault(List<ResponsibleRoleMappingEntity> existing, List<RoleMappingInput> inputs)
    {
        RoleMappingInput? input = inputs.FirstOrDefault(m => m.RoleKey == OnboardingRoles.HrManager.Key);
        string? current = existing.FirstOrDefault(m => m.RoleKey == OnboardingRoles.HrManager.Key)?.DefaultEmployeeId;
        return input is not null && input.DefaultEmployeeId != current;
    }

    private async Task<List<string>> LoadAllowedPositions()
    {
        return await db.ResponsiblePositions.AsNoTracking().Select(p => p.Position).ToListAsync();
    }

    private static ResponsibleRoleDto BuildRoleDto(
        OnboardingRoleDefinition roleDef,
        List<ApiEmployee> activeEmployees,
        Dictionary<string, string?> savedMappings)
    {
        List<EmployeeCandidateDto> candidates = OnboardingCandidates.ForRole(roleDef, activeEmployees);

        savedMappings.TryGetValue(roleDef.Key, out string? defaultEmployeeId);

        return new ResponsibleRoleDto(
            RoleKey: roleDef.Key,
            RoleName: roleDef.ExactTitle,
            DefaultEmployeeId: defaultEmployeeId,
            CandidatesCount: candidates.Count,
            DefaultInactive: defaultEmployeeId is not null && !activeEmployees.Any(e => e.Id == defaultEmployeeId),
            Candidates: candidates
        );
    }

    private void UpsertMapping(List<ResponsibleRoleMappingEntity> existing, RoleMappingInput input)
    {
        ResponsibleRoleMappingEntity? entity = existing.FirstOrDefault(x => x.RoleKey == input.RoleKey);
        string? employeeId = string.IsNullOrWhiteSpace(input.DefaultEmployeeId) ? null : input.DefaultEmployeeId;

        if (entity is not null)
        {
            entity.DefaultEmployeeId = employeeId;
        }
        else
        {
            db.ResponsibleRoleMappings.Add(new ResponsibleRoleMappingEntity
            {
                RoleKey = input.RoleKey,
                DefaultEmployeeId = employeeId
            });
        }
    }
}
