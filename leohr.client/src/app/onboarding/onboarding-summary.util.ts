import { formatDate } from '@angular/common';
import { OnboardingFlowStatus, OnboardingTaskStatus } from './models/onboarding-flow.enums';
import { OnboardingFlow, OnboardingFlowTask } from './models/onboarding-flow.model';
import { OnboardingSummaryCard } from './models/onboarding.model';
import {
  daysFromToday,
  isProbationReviewDue,
  PROBATION_REVIEW_WINDOW_DAYS,
  STARTING_SOON_DAYS,
  withinNextDays,
} from './onboarding-flow.util';

const DUE_SOON_DAYS = 5;
const RECENTLY_COMPLETED_DAYS = 30;
const MAX_OVERDUE_EXAMPLES = 2;
const MS_PER_DAY = 24 * 60 * 60 * 1000;
const LOCALE = 'en-US';
// Details of one item go in parentheses, so this separator only ever splits items.
export const ITEM_SEPARATOR = ' · ';

interface FlowTask {
  flow: OnboardingFlow;
  task: OnboardingFlowTask;
}

export function flowsSubtitle(flows: OnboardingFlow[]): string {
  const active = flows.filter((f) => f.status !== OnboardingFlowStatus.Completed).length;
  const recentlyCompleted = flows.filter(
    (f) => f.completedAt && Date.now() - Date.parse(f.completedAt) <= RECENTLY_COMPLETED_DAYS * MS_PER_DAY,
  ).length;
  const total = flows.length === 1 ? '1 flow' : `${flows.length} flows`;
  return `${total} · ${active} active · ${recentlyCompleted} completed in the last ${RECENTLY_COMPLETED_DAYS} days`;
}

export function buildSummaryCards(flows: OnboardingFlow[], currentUserId: string | null): OnboardingSummaryCard[] {
  const openTasks: FlowTask[] = flows
    .flatMap((flow) => flow.tasks.map((task) => ({ flow, task })))
    .filter(({ task }) => task.status !== OnboardingTaskStatus.Done)
    .sort((a, b) => a.task.dueDate.localeCompare(b.task.dueDate));

  return [
    startingSoonCard(flows),
    overdueCard(
      openTasks.filter(({ task }) => daysFromToday(task.dueDate) < 0),
      currentUserId,
    ),
    dueSoonCard(
      openTasks.filter(({ task }) => withinNextDays(task.dueDate, DUE_SOON_DAYS)),
      currentUserId,
    ),
    probationCard(flows),
  ];
}

function startingSoonCard(flows: OnboardingFlow[]): OnboardingSummaryCard {
  const starting = flows
    .filter((f) => withinNextDays(f.startDate, STARTING_SOON_DAYS))
    .sort((a, b) => a.startDate.localeCompare(b.startDate));
  return {
    label: 'Starting this week',
    value: starting.length,
    detail:
      starting.map((f) => `${f.fullName} (${startDayLabel(f.startDate)})`).join(ITEM_SEPARATOR) ||
      'No one starting soon',
  };
}

function overdueCard(overdue: FlowTask[], currentUserId: string | null): OnboardingSummaryCard {
  const lines = overdue.map(
    ({ flow, task }) => `${task.title} (${flow.fullName}, ${assigneeLabel(task, currentUserId)})`,
  );
  return {
    label: 'Overdue tasks',
    value: overdue.length,
    detail: lines.slice(0, MAX_OVERDUE_EXAMPLES).join(ITEM_SEPARATOR) || 'Nothing overdue',
    more: lines.slice(MAX_OVERDUE_EXAMPLES),
    alert: overdue.length > 0,
  };
}

function dueSoonCard(dueSoon: FlowTask[], currentUserId: string | null): OnboardingSummaryCard {
  const byAssignee = new Map<string, number>();
  let unassigned = 0;
  for (const { task } of dueSoon) {
    if (!task.assigneeId) {
      unassigned++;
      continue;
    }
    const label = assigneeLabel(task, currentUserId);
    byAssignee.set(label, (byAssignee.get(label) ?? 0) + 1);
  }
  // Unassigned goes last so the parts always add up to the total.
  const parts = [...byAssignee].sort((a, b) => b[1] - a[1]).map(([label, count]) => `${count} ${label}`);
  if (unassigned > 0) parts.push(`${unassigned} unassigned`);
  return {
    label: `Due in the next ${DUE_SOON_DAYS} days`,
    value: dueSoon.length,
    detail: parts.join(ITEM_SEPARATOR) || 'Nothing due',
  };
}

function probationCard(flows: OnboardingFlow[]): OnboardingSummaryCard {
  const reviews = flows
    .filter(isProbationReviewDue)
    .map((f) => ({ name: f.fullName, date: f.probationReviewDate }))
    .sort((a, b) => a.date.localeCompare(b.date));
  return {
    label: 'Probation reviews',
    value: reviews.length,
    detail:
      reviews.map((r) => `${r.name} (review ${formatDate(r.date, 'd MMM', LOCALE)})`).join(ITEM_SEPARATOR) ||
      `None in the next ${PROBATION_REVIEW_WINDOW_DAYS} days`,
  };
}

function startDayLabel(startDate: string): string {
  const days = daysFromToday(startDate);
  if (days === 0) return 'today';
  if (days === 1) return 'tomorrow';
  const weekday = formatDate(startDate, 'EEE', LOCALE);
  return days >= 7 ? `next ${weekday}` : weekday;
}

function assigneeLabel(task: OnboardingFlowTask, currentUserId: string | null): string {
  if (!task.assigneeId) return 'Unassigned';
  if (task.assigneeId === currentUserId) return 'You';
  return task.assigneeName ?? 'Unknown';
}
