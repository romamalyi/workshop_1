import { Seniority } from '../../promotion-criteria/promotion-criteria.enums';
import { DueAnchor, DueDirection, OnboardingPhase } from '../onboarding-template.enums';
import {
  EmploymentType,
  FlowAttention,
  FlowWarningKind,
  OnboardingFlowStatus,
  OnboardingTaskStatus,
} from './onboarding-flow.enums';

export const EMPLOYMENT_TYPE_LABELS: Record<EmploymentType, string> = {
  [EmploymentType.FullTime]: 'Full-time',
  [EmploymentType.PartTime]: 'Part-time',
};

export const FLOW_STATUS_LABELS: Record<OnboardingFlowStatus, string> = {
  [OnboardingFlowStatus.Created]: 'Created',
  [OnboardingFlowStatus.CreatedUnresolved]: 'Created — Unresolved',
  [OnboardingFlowStatus.Completed]: 'Completed',
};

export const FLOW_ATTENTION_LABELS: Record<FlowAttention, string> = {
  [FlowAttention.Unresolved]: 'Unresolved',
  [FlowAttention.ReviewDue]: 'Review due',
  [FlowAttention.Completed]: 'Completed',
};

export const FLOW_ATTENTION_CLASSES: Record<FlowAttention, string> = {
  [FlowAttention.Unresolved]: 'ob-attention--unresolved',
  [FlowAttention.ReviewDue]: 'ob-attention--review',
  [FlowAttention.Completed]: 'ob-attention--completed',
};

export const TASK_STATUS_LABELS: Record<OnboardingTaskStatus, string> = {
  [OnboardingTaskStatus.NotStarted]: 'Not started',
  [OnboardingTaskStatus.InProgress]: 'In progress',
  [OnboardingTaskStatus.Overdue]: 'Overdue',
  [OnboardingTaskStatus.Done]: 'Done',
};

export const TASK_STATUS_CLASSES: Record<OnboardingTaskStatus, string> = {
  [OnboardingTaskStatus.NotStarted]: 'ob-attention--notstarted',
  [OnboardingTaskStatus.InProgress]: 'ob-attention--inprogress',
  [OnboardingTaskStatus.Overdue]: 'ob-attention--overdue',
  [OnboardingTaskStatus.Done]: 'ob-attention--completed',
};

/** The statuses the API returns for a task; Overdue is derived for display. */
export type FlowTaskStatus = Exclude<OnboardingTaskStatus, OnboardingTaskStatus.Overdue>;

export interface OnboardingFlowTask {
  id: number;
  title: string;
  description: string | null;
  phase: OnboardingPhase;
  milestoneName: string;
  responsibleRoleKey: string | null;
  responsibleRoleName: string | null;
  assigneeId: string | null;
  assigneeName: string | null;
  dueDate: string;
  dueOffsetDays: number;
  dueDirection: DueDirection;
  dueAnchor: DueAnchor;
  status: FlowTaskStatus;
  hasFields: boolean;
}

export interface FlowWarning {
  kind: FlowWarningKind;
  roleNames: string[];
}

export const FLOW_WARNING_TITLES: Record<FlowWarningKind, string> = {
  [FlowWarningKind.ResponsiblePositionNotAllowed]: 'Responsible person no longer in an allowed position',
  [FlowWarningKind.ResponsiblePositionChanged]: 'Responsible person changed position',
  [FlowWarningKind.ResponsibleInactive]: 'Responsible person is no longer active',
  [FlowWarningKind.RoleDefaultInactive]: 'Default person for a role is no longer active',
};

export interface OnboardingFlow {
  id: number;
  fullName: string;
  position: string | null;
  employmentType: EmploymentType;
  startDate: string;
  level: Seniority | null;
  onboardingTypeId: number | null;
  onboardingTypeName: string | null;
  onboardingTypeIsArchived: boolean;
  responsiblePersonId: string;
  responsiblePersonName: string | null;
  responsiblePersonRoleName: string;
  warnings: FlowWarning[];
  createdById: string | null;
  createdByName: string | null;
  status: OnboardingFlowStatus;
  unassignedTaskCount: number;
  completedAt: string | null;
  probationReviewDate: string;
  tasks: OnboardingFlowTask[];
}

export interface CreateOnboardingFlowRequest {
  fullName: string;
  position: string | null;
  employmentType: EmploymentType;
  startDate: string;
  level: Seniority | null;
  responsiblePersonId: string;
  onboardingTypeId: number;
  optionalTemplateIds: number[];
}

export interface UnassignedTask {
  taskId: number;
  title: string;
  phase: OnboardingPhase;
  responsibleRoleKey: string | null;
  responsibleRoleName: string | null;
  dueDate: string;
  flowId: number;
  flowFullName: string;
}

export interface MyOnboardingTask {
  id: number;
  title: string;
  phase: OnboardingPhase;
  dueDate: string;
  isDone: boolean;
  isStarted: boolean;
  fieldLabels: string[];
  assigneeId: string | null;
  assigneeName: string | null;
  responsibleRoleName: string | null;
  isAssignedToMe: boolean;
  flowId: number;
  flowFullName: string;
  flowPosition: string | null;
  flowStartDate: string;
}

export type MyTasksScope = 'All' | 'Mine';

export type MyTasksStatus = 'Open' | 'Overdue' | 'Done' | 'All';

export interface MyTasksQuery {
  searchTerm: string;
  scope: MyTasksScope;
  status: MyTasksStatus;
  today?: string;
}

export interface MyOnboardingTaskFlow {
  flowId: number;
  tasks: MyOnboardingTask[];
}

export interface MyOnboardingTasksSummary {
  taskCount: number;
  flowCount: number;
  mineCount: number;
  overdueCount: number;
  dueTodayCount: number;
  allScopeCount: number;
  mineScopeCount: number;
}

export interface RecheckAssignment {
  taskId: number;
  responsibleRoleKey: string | null;
  responsibleRoleName: string | null;
  assigneeId: string;
  assigneeName: string | null;
}

export interface ApplyRecheckResult {
  assignedCount: number;
}
