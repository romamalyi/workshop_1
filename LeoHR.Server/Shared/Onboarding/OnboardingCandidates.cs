using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.Shared.Onboarding;

public static class OnboardingCandidates
{
    public static List<ApiEmployee> ActiveWithPosition(IEnumerable<ApiEmployee> employees, List<string> positions)
    {
        return [.. employees.Where(e => e.Active && positions.Any(p => PersonRestrictions.Matches(e, p)))];
    }

    public static List<EmployeeCandidateDto> ForRole(OnboardingRoleDefinition roleDef, List<ApiEmployee> activeEmployees)
    {
        return ToCandidates(activeEmployees.Where(e => PersonRestrictions.Matches(e, roleDef.ExactTitle)));
    }

    public static List<EmployeeCandidateDto> ToCandidates(IEnumerable<ApiEmployee> employees)
    {
        return [.. employees
            .Select(e => new EmployeeCandidateDto(e.Id, $"{e.FirstName} {e.LastName}".Trim()))
            .OrderBy(e => e.FullName, StringComparer.InvariantCultureIgnoreCase)];
    }
}
