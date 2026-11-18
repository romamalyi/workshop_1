import { Seniority } from '../promotion-criteria/promotion-criteria.enums';
import {
  DueAnchor,
  DueDirection,
  OnboardingPhase,
  TemplateAppliesTo,
  TemplateFieldType,
} from './onboarding-template.enums';

export const PHASE_LABELS: Record<OnboardingPhase, string> = {
  [OnboardingPhase.BeforeFirstDay]: 'Before 1st day',
  [OnboardingPhase.FirstDay]: '1st day',
  [OnboardingPhase.FirstWeek]: '1st week',
  [OnboardingPhase.SecondWeek]: '2nd week',
  [OnboardingPhase.FirstMonth]: '1st month',
  [OnboardingPhase.ThirdMonth]: '3rd month',
};

export const PHASE_HINTS: Record<OnboardingPhase, string> = {
  [OnboardingPhase.BeforeFirstDay]: 'Generated relative to the start date',
  [OnboardingPhase.FirstDay]: 'All due on the start date',
  [OnboardingPhase.FirstWeek]: 'Due during the first week after the start date',
  [OnboardingPhase.SecondWeek]: 'Due during the second week after the start date',
  [OnboardingPhase.FirstMonth]: 'Due within the first month after the start date',
  [OnboardingPhase.ThirdMonth]: 'Due around the end of the trial period',
};

export const DUE_ANCHOR_LABELS: Record<DueAnchor, string> = {
  [DueAnchor.StartDate]: 'start date',
  [DueAnchor.EndOfProbation]: 'end of probation',
};

export const DUE_ANCHOR_SHORT_LABELS: Record<DueAnchor, string> = {
  [DueAnchor.StartDate]: 'Start',
  [DueAnchor.EndOfProbation]: 'End of probation',
};

export const APPLIES_TO_LABELS: Record<TemplateAppliesTo, string> = {
  [TemplateAppliesTo.AllHires]: 'All hires',
  [TemplateAppliesTo.Optional]: 'Optional',
};

export const FIELD_TYPE_LABELS: Record<TemplateFieldType, string> = {
  [TemplateFieldType.Note]: 'Note',
  [TemplateFieldType.Checklist]: 'Checklist',
  [TemplateFieldType.EmployeePicker]: 'Employee picker',
};

export interface TemplateChecklistItem {
  id: number;
  text: string;
}

export interface TemplateField {
  id: number;
  fieldType: TemplateFieldType;
  label: string;
  hint: string | null;
  personRestrictionKey: string | null;
  checklistItems: TemplateChecklistItem[];
}

export interface OnboardingTemplate {
  id: number;
  title: string;
  phase: OnboardingPhase;
  milestoneId: number;
  responsibleRoleKey: string | null;
  responsibleRoleName: string | null;
  description: string | null;
  dueOffsetDays: number;
  dueDirection: DueDirection;
  dueAnchor: DueAnchor;
  appliesTo: TemplateAppliesTo;
  suggestedFor: Seniority[];
  isActive: boolean;
  fields: TemplateField[];
  updatedAt: string | null;
  updatedByName: string | null;
}

export interface MilestoneOption {
  id: number;
  name: string;
}

export interface SaveChecklistItemInput {
  id?: number;
  text: string;
}

export interface SaveFieldInput {
  id?: number;
  fieldType: TemplateFieldType;
  label: string;
  hint: string | null;
  personRestrictionKey: string | null;
  checklistItems: SaveChecklistItemInput[];
}

export interface SaveTemplateRequest {
  title: string;
  phase: OnboardingPhase;
  milestoneId: number;
  responsibleRoleKey: string | null;
  description: string | null;
  dueOffsetDays: number;
  dueDirection: DueDirection;
  dueAnchor: DueAnchor;
  appliesTo: TemplateAppliesTo;
  suggestedFor: Seniority[];
  fields: SaveFieldInput[];
}
