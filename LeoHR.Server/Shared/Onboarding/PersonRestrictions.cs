using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.Shared.Onboarding;

// A Person field is restricted to employees holding one position; the key is the position title itself.
public static class PersonRestrictions
{
    public static List<string> Positions(IEnumerable<ApiEmployee> employees)
    {
        return [.. employees
            .Where(e => e.Active && !string.IsNullOrWhiteSpace(e.Position))
            .Select(e => e.Position!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
    }

    public static bool Matches(ApiEmployee employee, string position)
    {
        return string.Equals(employee.Position?.Trim(), position.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
