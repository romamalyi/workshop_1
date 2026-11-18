using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Features.OnboardingFlows;

internal static class FlowTaskGeneration
{
    public static Result AddTasks(
        OnboardingFlowEntity flow,
        IEnumerable<OnboardingTaskTemplateEntity> templates,
        Dictionary<string, string> roleKeysByPerson,
        Dictionary<string, string?> roleMappings)
    {
        foreach (OnboardingTaskTemplateEntity template in templates)
        {
            DateOnly? anchorDate = template.DueAnchor switch
            {
                DueAnchor.StartDate => flow.StartDate,
                DueAnchor.EndOfProbation => flow.StartDate.AddMonths(flow.ProbationMonths),
                _ => null,
            };
            if (anchorDate is null)
            {
                return Result.Fail(new BusinessRuleError($"The due rule of the template '{template.Title}' is not supported."));
            }

            flow.Tasks.Add(new OnboardingFlowTaskEntity
            {
                TemplateId = template.Id,
                Title = template.Title,
                Description = template.Description,
                Phase = template.Phase,
                MilestoneId = template.MilestoneId,
                ResponsibleRoleKey = template.ResponsibleRoleKey,
                DueOffsetDays = template.DueOffsetDays,
                DueDirection = template.DueDirection,
                DueAnchor = template.DueAnchor,
                DueDate = ComputeDueDate(anchorDate.Value, template.DueOffsetDays, template.DueDirection),
                AssigneeId = ResolveAssignee(template.ResponsibleRoleKey, flow.ResponsiblePersonId, roleKeysByPerson, roleMappings),
                Fields =
                [
                    .. template.Fields.Select(f => new OnboardingFlowTaskFieldEntity
                    {
                        TemplateFieldId = f.Id,
                        SortOrder = f.SortOrder,
                        FieldType = f.FieldType,
                        Label = f.Label,
                    })
                ],
            });
        }

        return Result.Ok();
    }

    public static string? ResolveAssignee(
        string? roleKey,
        string responsiblePersonId,
        Dictionary<string, string> roleKeysByPerson,
        Dictionary<string, string?> roleMappings)
    {
        if (roleKey is null)
        {
            return null;
        }

        if (roleKeysByPerson.TryGetValue(responsiblePersonId, out string? heldRoleKey) && heldRoleKey == roleKey)
        {
            return responsiblePersonId;
        }

        return roleMappings.TryGetValue(roleKey, out string? defaultEmployeeId) ? defaultEmployeeId : null;
    }

    private static DateOnly ComputeDueDate(DateOnly anchorDate, int offsetDays, DueDirection direction)
    {
        DateOnly date = direction == DueDirection.Before ? anchorDate.AddDays(-offsetDays) : anchorDate.AddDays(offsetDays);
        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(-1),
            DayOfWeek.Sunday => date.AddDays(1),
            _ => date,
        };
    }
}
