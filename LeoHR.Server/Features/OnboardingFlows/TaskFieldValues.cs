using FluentResults;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Features.OnboardingFlows;

internal record TaskFieldChange(OnboardingTaskActivityKind Kind, string? ChecklistItemText = null);

internal static class TaskFieldValues
{
    public static async Task<Result<List<TaskFieldChange>>> Apply(
        OnboardingFlowTaskFieldEntity field,
        TaskFieldValueInput input,
        Dictionary<int, OnboardingTaskTemplateFieldEntity> templateFields,
        IEmployeeApiClient employeeApi)
    {
        List<TaskFieldChange> changes = [];
        switch (field.FieldType)
        {
            case TemplateFieldType.Note:
                string? note = string.IsNullOrWhiteSpace(input.NoteValue) ? null : input.NoteValue;
                if (note != field.NoteValue)
                {
                    field.NoteValue = note;
                    changes.Add(new TaskFieldChange(OnboardingTaskActivityKind.FieldChanged));
                }

                break;

            case TemplateFieldType.EmployeePicker:
                if (string.IsNullOrWhiteSpace(input.PersonId))
                {
                    if (field.PersonId is not null)
                    {
                        field.PersonId = null;
                        changes.Add(new TaskFieldChange(OnboardingTaskActivityKind.FieldChanged));
                    }

                    break;
                }

                if (field.PersonId == input.PersonId)
                {
                    break;
                }

                ApiEmployee? person = await employeeApi.GetByIdAsync(input.PersonId);
                if (person is not { Active: true })
                {
                    return Result.Fail(new BusinessRuleError("Person was not found.", nameof(TaskFieldValueInput.PersonId)));
                }

                string? restriction = templateFields.GetValueOrDefault(field.TemplateFieldId ?? 0)?.PersonRestrictionKey;
                if (restriction is not null && !PersonRestrictions.Matches(person, restriction))
                {
                    return Result.Fail(new BusinessRuleError($"This person cannot be picked: {restriction} only.", nameof(TaskFieldValueInput.PersonId)));
                }

                field.PersonId = input.PersonId;
                changes.Add(new TaskFieldChange(OnboardingTaskActivityKind.FieldChanged));
                break;

            case TemplateFieldType.Checklist:
                Dictionary<int, string> itemTexts = templateFields.GetValueOrDefault(field.TemplateFieldId ?? 0)?.ChecklistItems
                    .ToDictionary(i => i.Id, i => i.Text) ?? [];
                HashSet<int> wanted = [.. input.CheckedItemIds ?? []];
                if (!wanted.IsSubsetOf(itemTexts.Keys))
                {
                    return Result.Fail(new BusinessRuleError("Checked items must belong to this field.", nameof(TaskFieldValueInput.CheckedItemIds)));
                }

                foreach (OnboardingFlowTaskCheckedItemEntity untick in field.CheckedItems.Where(c => !wanted.Contains(c.ChecklistItemId)).ToList())
                {
                    field.CheckedItems.Remove(untick);
                    changes.Add(new TaskFieldChange(OnboardingTaskActivityKind.ChecklistItemUnchecked, itemTexts.GetValueOrDefault(untick.ChecklistItemId)));
                }

                foreach (int itemId in wanted.Except(field.CheckedItems.Select(c => c.ChecklistItemId)).ToList())
                {
                    field.CheckedItems.Add(new OnboardingFlowTaskCheckedItemEntity { ChecklistItemId = itemId });
                    changes.Add(new TaskFieldChange(OnboardingTaskActivityKind.ChecklistItemChecked, itemTexts[itemId]));
                }

                break;
        }

        return Result.Ok(changes);
    }

    public static bool IsFilled(OnboardingFlowTaskFieldEntity field, OnboardingTaskTemplateFieldEntity? templateField)
    {
        return field.FieldType switch
        {
            TemplateFieldType.Note => !string.IsNullOrWhiteSpace(field.NoteValue),
            TemplateFieldType.EmployeePicker => !string.IsNullOrEmpty(field.PersonId),
            TemplateFieldType.Checklist => templateField is null
                || templateField.ChecklistItems.All(i => field.CheckedItems.Any(c => c.ChecklistItemId == i.Id)),
            _ => true,
        };
    }
}
