import { SENIORITY_LABELS } from '../promotion-criteria/promotion-criteria.model';
import { DueAnchor, DueDirection, TemplateAppliesTo, TemplateFieldType } from './onboarding-template.enums';
import {
  DUE_ANCHOR_LABELS,
  DUE_ANCHOR_SHORT_LABELS,
  FIELD_TYPE_LABELS,
  OnboardingTemplate,
} from './onboarding-template.model';

export function pluralize(n: number, singular: string): string {
  return n === 1 ? singular : `${singular}s`;
}

export function formatDueRule(days: number, direction: DueDirection, anchor: DueAnchor): string {
  const anchorLabel = DUE_ANCHOR_LABELS[anchor];
  if (days === 0) {
    return `Due on ${anchorLabel}.`;
  }
  return `Due ${days} ${pluralize(days, 'calendar day')} ${direction.toLowerCase()} ${anchorLabel}.`;
}

export function formatShortDueRule(days: number, direction: DueDirection, anchor: DueAnchor): string {
  const anchorLabel = DUE_ANCHOR_SHORT_LABELS[anchor];
  if (days === 0) return anchor === DueAnchor.EndOfProbation ? anchorLabel : `${anchorLabel} date`;
  const sign = direction === DueDirection.Before ? '–' : '+';
  return `${anchorLabel} ${sign} ${days} ${pluralize(days, 'day')}`;
}

export function formatTemplateDueRule(template: OnboardingTemplate): string {
  return formatDueRule(template.dueOffsetDays, template.dueDirection, template.dueAnchor);
}

export function formatFieldSummary(fields: { fieldType: TemplateFieldType; checklistItemCount: number }[]): string {
  if (fields.length === 0) return '—';
  return fields
    .map((field) => {
      if (field.fieldType === TemplateFieldType.Checklist) {
        const count = field.checklistItemCount;
        return `Checklist (${count} ${pluralize(count, 'item')})`;
      }
      return FIELD_TYPE_LABELS[field.fieldType];
    })
    .join(', ');
}

type Applicability = Pick<OnboardingTemplate, 'appliesTo' | 'suggestedFor'>;

function suggestedLevels(template: Applicability): string {
  return template.suggestedFor.map((level) => SENIORITY_LABELS[level]).join(', ');
}

export function appliesBadge(template: Applicability): string {
  if (template.appliesTo === TemplateAppliesTo.AllHires) return 'All hires';
  return template.suggestedFor.length > 0 ? `Optional · ${suggestedLevels(template)}` : 'Optional';
}

export function appliesTooltip(template: Applicability): string {
  if (template.appliesTo === TemplateAppliesTo.AllHires)
    return 'Created for every flow of this type, with no choice in the wizard.';
  return template.suggestedFor.length > 0
    ? `Optional. Suggested for ${suggestedLevels(template)}; disabling it needs confirmation.`
    : 'Optional. Offered as a tick in the wizard.';
}
