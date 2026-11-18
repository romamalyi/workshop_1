using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingSettings;

public class OnboardingCandidatesService(
    LeoHrDbContext db,
    IEmployeeApiClient employeeApi) : IOnboardingCandidates
{
    public async Task<List<ResponsibleCandidateDto>> GetResponsibleCandidates()
    {
        IReadOnlyList<ApiEmployee> allEmployees = await employeeApi.GetAllAsync();
        List<string> allowed = await db.ResponsiblePositions.AsNoTracking().Select(p => p.Position).ToListAsync();
        return [.. OnboardingCandidates.ActiveWithPosition(allEmployees, allowed)
            .Select(e => new ResponsibleCandidateDto(e.Id, $"{e.FirstName} {e.LastName}".Trim(), e.Position?.Trim()))
            .OrderBy(e => e.FullName, StringComparer.InvariantCultureIgnoreCase)];
    }

    public async Task<List<EmployeeCandidateDto>> GetAssigneeCandidates(string? roleKey)
    {
        IReadOnlyList<ApiEmployee> allEmployees = await employeeApi.GetAllAsync();
        List<ApiEmployee> activeEmployees = [.. allEmployees.Where(e => e.Active)];

        // Only a task without a responsible role may go to anyone; a role task goes to that role's holders only.
        if (roleKey is null)
        {
            return OnboardingCandidates.ToCandidates(activeEmployees);
        }

        OnboardingRoleDefinition? roleDef = OnboardingRoles.Find(roleKey);
        return roleDef is null ? [] : OnboardingCandidates.ForRole(roleDef, activeEmployees);
    }

    public async Task<List<EmployeeCandidateDto>> GetPersonFieldCandidates(string? restrictionKey)
    {
        IReadOnlyList<ApiEmployee> allEmployees = await employeeApi.GetAllAsync();
        IEnumerable<ApiEmployee> active = allEmployees.Where(e => e.Active);

        return OnboardingCandidates.ToCandidates(
            string.IsNullOrWhiteSpace(restrictionKey) ? active : active.Where(e => PersonRestrictions.Matches(e, restrictionKey)));
    }
}
