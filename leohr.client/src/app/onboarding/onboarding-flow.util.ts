import { Params, Router } from '@angular/router';
import { PHASE_LABELS } from './onboarding-template.model';
import { FlowAttention, OnboardingFlowStatus, OnboardingTaskStatus } from './models/onboarding-flow.enums';
import { OnboardingFlow } from './models/onboarding-flow.model';
import { FlowNavigationState } from './models/onboarding-task-detail.model';

const AVATAR_COLORS = ['#A8C5E8', '#C2A8D5', '#E8C4A8', '#A8D5C2', '#E8A8C4', '#A8D5D5', '#D5D5A8', '#C5A8E8'];
const MS_PER_DAY = 24 * 60 * 60 * 1000;
export const PROBATION_REVIEW_WINDOW_DAYS = 14;
// A calendar week: 5 working days plus the weekend.
export const STARTING_SOON_DAYS = 7;

export function readFlowsQueryParams(router: Router): Params | null {
  const state = (router.currentNavigation()?.extras.state ?? history.state) as FlowNavigationState | null;
  return state?.flowsQueryParams ?? null;
}

export function avatarColor(id: number): string {
  return AVATAR_COLORS[id % AVATAR_COLORS.length];
}

/** The avatar of a person picked in a field: their initials, or "?" when nobody is picked. */
export function personAvatar(id: string | null, name: string | null): { initials: string; color: string } {
  if (!id) return { initials: '?', color: 'var(--surface-subtle)' };
  const hash = [...id].reduce((sum, char) => sum + char.charCodeAt(0), 0);
  return { initials: initials(name ?? 'Unknown person'), color: avatarColor(hash) };
}

export function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return (first + last).toUpperCase();
}

/** Whole days from today to an ISO `yyyy-MM-dd` date; negative once the date has passed. */
export function daysFromToday(isoDate: string): number {
  const [year, month, day] = isoDate.split('-').map(Number);
  const today = new Date();
  const todayUtc = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  return Math.round((Date.UTC(year, month - 1, day) - todayUtc) / MS_PER_DAY);
}

export function relativeStart(startDate: string): string {
  const days = daysFromToday(startDate);
  if (days === 0) return 'Today';
  if (days === 1) return 'Tomorrow';
  if (days > 1) return `In ${days} days`;
  return `Day ${1 - days}`;
}

/** The phase of the next task not yet past due, or the last phase once every task is. */
export function currentPhase(flow: OnboardingFlow): string {
  if (flow.tasks.length === 0) return '—';
  const upcoming = flow.tasks.find((t) => daysFromToday(t.dueDate) >= 0) ?? flow.tasks[flow.tasks.length - 1];
  return PHASE_LABELS[upcoming.phase];
}

export const NO_ROLE_HOLDERS = {
  title: 'No employees with this role',
  hint: 'Add one in Employees, or check Onboarding Settings',
} as const;

export function unassignedLabel(count: number): string {
  return count === 1 ? '1 task has no assignee' : `${count} tasks have no assignee`;
}

export function taskCountLabel(count: number): string {
  return count === 1 ? '1 task' : `${count} tasks`;
}

export function withinNextDays(isoDate: string, days: number): boolean {
  const diff = daysFromToday(isoDate);
  return diff >= 0 && diff <= days;
}

export function isProbationReviewDue(flow: OnboardingFlow): boolean {
  return withinNextDays(flow.probationReviewDate, PROBATION_REVIEW_WINDOW_DAYS);
}

export function doneTaskCount(flow: OnboardingFlow): number {
  return flow.tasks.filter((t) => t.status === OnboardingTaskStatus.Done).length;
}

export function progressPercent(flow: OnboardingFlow): number {
  return flow.tasks.length === 0 ? 0 : Math.round((doneTaskCount(flow) * 100) / flow.tasks.length);
}

export function flowAttention(flow: OnboardingFlow): FlowAttention | null {
  if (flow.status === OnboardingFlowStatus.CreatedUnresolved) return FlowAttention.Unresolved;
  if (isProbationReviewDue(flow)) return FlowAttention.ReviewDue;
  if (flow.status === OnboardingFlowStatus.Completed) return FlowAttention.Completed;
  return null;
}
