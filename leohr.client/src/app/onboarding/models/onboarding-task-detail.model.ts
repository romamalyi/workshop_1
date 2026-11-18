import { Params } from '@angular/router';
import { OnboardingPhase, TemplateFieldType } from '../onboarding-template.enums';
import { OnboardingTaskStatus, TaskActivityKind } from './onboarding-flow.enums';

export type TaskOrigin = 'flow' | 'my-tasks';

export interface FlowNavigationState {
  flowsQueryParams?: Params;
}

export interface TaskNavigationState extends FlowNavigationState {
  from: TaskOrigin;
  // The task list's search and filters, so its back link returns to them.
  queryParams?: Params;
}

export interface TaskChecklistItem {
  id: number;
  text: string;
  isChecked: boolean;
}

export interface OnboardingTaskField {
  id: number;
  fieldType: TemplateFieldType;
  label: string;
  hint: string | null;
  personRestrictionKey: string | null;
  personRestrictionLabel: string | null;
  noteValue: string | null;
  personId: string | null;
  personName: string | null;
  checklistItems: TaskChecklistItem[];
}

export interface OnboardingTaskDetail {
  id: number;
  title: string;
  description: string | null;
  phase: OnboardingPhase;
  milestoneName: string;
  dueDate: string;
  status: OnboardingTaskStatus;
  flowId: number;
  flowFullName: string;
  assigneeId: string | null;
  assigneeName: string | null;
  responsibleRoleKey: string | null;
  responsibleRoleName: string | null;
  isAssignedToMe: boolean;
  canEdit: boolean;
  canChangeAssignee: boolean;
  fields: OnboardingTaskField[];
  activity: TaskActivity[];
}

export interface TaskActivity {
  kind: TaskActivityKind;
  fromName: string | null;
  toName: string | null;
  fieldLabel: string | null;
  checklistItemText: string | null;
  at: string;
  actorName: string | null;
}

export interface TaskFieldValueInput {
  fieldId: number;
  noteValue?: string | null;
  personId?: string | null;
  checkedItemIds?: number[];
}
