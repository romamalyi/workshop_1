import { TemplateFieldType } from './onboarding-template.enums';
import { TaskActivityKind } from './models/onboarding-flow.enums';
import { OnboardingTaskField, TaskActivity } from './models/onboarding-task-detail.model';

export function isFieldFilled(field: OnboardingTaskField): boolean {
  switch (field.fieldType) {
    case TemplateFieldType.Note:
      return !!field.noteValue?.trim();
    case TemplateFieldType.EmployeePicker:
      return !!field.personId;
    case TemplateFieldType.Checklist:
      return field.checklistItems.every((item) => item.isChecked);
  }
}

export function fieldHelperText(field: OnboardingTaskField): string {
  switch (field.fieldType) {
    case TemplateFieldType.Note:
      return 'Note field · required';
    case TemplateFieldType.EmployeePicker:
      return field.personRestrictionLabel
        ? `Pick from the imported directory · ${field.personRestrictionLabel} only · required`
        : 'Pick from the imported directory · required';
    case TemplateFieldType.Checklist: {
      const ticked = field.checklistItems.filter((item) => item.isChecked).length;
      return `Checklist · ${ticked} of ${field.checklistItems.length} ticked · all required`;
    }
  }
}

export function taskActivityTitle(entry: TaskActivity): string {
  switch (entry.kind) {
    case TaskActivityKind.AssigneeChanged:
      return entry.fromName
        ? `Assignee changed from ${entry.fromName} to ${entry.toName ?? 'Unknown'}`
        : `Assigned to ${entry.toName ?? 'Unknown'}`;
    case TaskActivityKind.Started:
      return 'Task started';
    case TaskActivityKind.StartUndone:
      return 'Task start undone';
    case TaskActivityKind.FieldChanged:
      return `${entry.fieldLabel} changed`;
    case TaskActivityKind.ChecklistItemChecked:
      return `${entry.fieldLabel}: ${entry.checklistItemText ?? 'item'} checked`;
    case TaskActivityKind.ChecklistItemUnchecked:
      return `${entry.fieldLabel}: ${entry.checklistItemText ?? 'item'} unchecked`;
    case TaskActivityKind.Completed:
      return 'Task marked as done';
    case TaskActivityKind.Reopened:
      return 'Task reopened';
    default:
      // A kind added on the server before the client knows it still renders something readable.
      return entry.kind;
  }
}
