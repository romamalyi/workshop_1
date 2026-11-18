using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingFlows;

public record FlowWarningContext(
    IReadOnlyDictionary<string, ApiEmployee> Employees,
    List<string> AllowedPositions,
    List<string> InactiveDefaultRoleNames);

public static class FlowWarnings
{
    public static List<FlowWarningDto> Build(OnboardingFlowEntity flow, bool isCompleted, FlowWarningContext context)
    {
        if (isCompleted || context.Employees.Count == 0)
        {
            return [];
        }

        List<FlowWarningDto> warnings = [];
        ApiEmployee? person = context.Employees.GetValueOrDefault(flow.ResponsiblePersonId);

        if (person is not { Active: true })
        {
            warnings.Add(new FlowWarningDto(FlowWarningKind.ResponsibleInactive, []));
        }
        else if (!context.AllowedPositions.Any(p => PersonRestrictions.Matches(person, p)))
        {
            bool moved = flow.ResponsiblePosition is not null
                && !string.Equals(flow.ResponsiblePosition.Trim(), person.Position?.Trim(), StringComparison.OrdinalIgnoreCase);
            warnings.Add(new FlowWarningDto(
                moved ? FlowWarningKind.ResponsiblePositionChanged : FlowWarningKind.ResponsiblePositionNotAllowed, []));
        }

        if (context.InactiveDefaultRoleNames.Count > 0)
        {
            warnings.Add(new FlowWarningDto(FlowWarningKind.RoleDefaultInactive, context.InactiveDefaultRoleNames));
        }

        return warnings;
    }
}
