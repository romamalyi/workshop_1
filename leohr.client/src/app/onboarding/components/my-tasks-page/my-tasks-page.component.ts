import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { InfiniteScrollPaginator } from '../../../shared/utils/infinite-scroll-paginator';
import { OnboardingTaskStatus } from '../../models/onboarding-flow.enums';
import { TaskNavigationState } from '../../models/onboarding-task-detail.model';
import {
  MyOnboardingTask,
  MyOnboardingTaskFlow,
  MyOnboardingTasksSummary,
  MyTasksQuery,
  MyTasksScope,
  MyTasksStatus,
  TASK_STATUS_CLASSES,
  TASK_STATUS_LABELS,
} from '../../models/onboarding-flow.model';
import { daysFromToday, initials, relativeStart } from '../../onboarding-flow.util';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { pluralize } from '../../onboarding-template.util';
import { OnboardingMyTasksService } from '../../services/onboarding-my-tasks.service';

const PAGE_SIZE = 10;
const STATUSES: readonly MyTasksStatus[] = ['Open', 'Overdue', 'Done', 'All'];

interface TaskRow extends MyOnboardingTask {
  status: OnboardingTaskStatus;
  detail: string;
  dueLabel: string;
  isDueToday: boolean;
}

interface HireGroup {
  flowId: number;
  name: string;
  meta: string;
  counts: string;
  rows: TaskRow[];
}

@Component({
  selector: 'app-my-tasks-page',
  templateUrl: './my-tasks-page.component.html',
  styleUrl: './my-tasks-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksPageComponent {
  private readonly tasksService = inject(OnboardingMyTasksService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly datePipe = inject(DatePipe);
  private readonly destroyRef = inject(DestroyRef);

  readonly phaseLabels = PHASE_LABELS;
  readonly statusLabels = TASK_STATUS_LABELS;
  readonly statusClasses = TASK_STATUS_CLASSES;
  readonly initials = initials;
  readonly filters: { key: MyTasksStatus; label: string }[] = [
    { key: 'Open', label: 'Open' },
    { key: 'Overdue', label: TASK_STATUS_LABELS[OnboardingTaskStatus.Overdue] },
    { key: 'Done', label: TASK_STATUS_LABELS[OnboardingTaskStatus.Done] },
    { key: 'All', label: 'All' },
  ];
  readonly Status = OnboardingTaskStatus;

  readonly canWriteOnboarding = this.auth.canWriteOnboarding;
  readonly loading = signal(true);
  // After the first load, a reload keeps the current list on screen until the new one arrives.
  readonly loaded = signal(false);
  readonly loadingMore = signal(false);
  readonly loadFailed = signal(false);

  // A missing param is the default: all tasks, open ones.
  private readonly queryParams = toSignal(this.route.queryParams, { requireSync: true });
  readonly searchTerm = computed(() => (this.queryParams()['searchTerm'] as string | undefined) ?? '');
  readonly scope = computed<MyTasksScope>(() => (this.queryParams()['scope'] === 'Mine' ? 'Mine' : 'All'));
  readonly filter = computed<MyTasksStatus>(() => {
    const status = this.queryParams()['status'] as MyTasksStatus | undefined;
    return status && STATUSES.includes(status) ? status : 'Open';
  });
  private readonly query = computed<MyTasksQuery>(() => ({
    searchTerm: this.searchTerm().trim(),
    scope: this.scope(),
    status: this.filter(),
  }));
  // The list follows the query it was loaded for, not the URL, so it doesn't change before the new data arrives.
  private requestedQuery = this.query();
  private readonly shownQuery = signal(this.requestedQuery);

  readonly taskState = computed<TaskNavigationState>(() => ({ from: 'my-tasks', queryParams: this.queryParams() }));
  readonly showsAllTasks = computed(() => this.canWriteOnboarding() && this.shownQuery().scope === 'All');

  private readonly flows = signal<MyOnboardingTaskFlow[]>([]);
  private readonly summary = signal<MyOnboardingTasksSummary | null>(null);
  private latestSummary = 0;
  private readonly paginator = new InfiniteScrollPaginator<MyOnboardingTaskFlow>({
    pageSize: PAGE_SIZE,
    fetchPage: (page) => this.tasksService.getPage(this.requestQuery(), page, PAGE_SIZE),
    onItems: (flows) => {
      this.flows.set(flows);
      this.shownQuery.set(this.requestedQuery);
      this.loaded.set(true);
    },
    onLoadingChange: (loading) => this.loading.set(loading),
    onLoadingMoreChange: (loadingMore) => this.loadingMore.set(loadingMore),
    onError: (context) => {
      if (context === 'first') this.loadFailed.set(true);
      this.toast.error('Could not load your tasks.');
    },
  });

  readonly scopes = computed(() => {
    const summary = this.summary();
    return [
      { key: 'All' as const, label: `All tasks · ${summary?.allScopeCount ?? 0}` },
      { key: 'Mine' as const, label: `Assigned to me · ${summary?.mineScopeCount ?? 0}` },
    ];
  });

  readonly subtitle = computed(() => {
    const summary = this.summary();
    if (!summary) return '';
    if (summary.taskCount === 0) return 'No tasks';
    const parts = [`${countLabel(summary.taskCount, 'task')} across ${countLabel(summary.flowCount, 'flow')}`];
    if (this.showsAllTasks() && summary.mineCount !== summary.taskCount) {
      parts.push(`${summary.mineCount} assigned to you`);
    }
    if (summary.overdueCount > 0) parts.push(`${summary.overdueCount} overdue`);
    if (summary.dueTodayCount > 0) parts.push(`${summary.dueTodayCount} due today`);
    return parts.join(' · ');
  });

  // Rows marked done here stay in the loaded pages, so the status filter is applied again on screen.
  readonly groups = computed<HireGroup[]>(() =>
    this.flows().flatMap((flow) => {
      const rows = flow.tasks.map(toRow).filter((r) => matchesFilter(r, this.shownQuery().status));
      return rows.length > 0 ? [this.toGroup(rows)] : [];
    }),
  );

  readonly emptyMessage = computed(() => {
    const { searchTerm, scope, status } = this.shownQuery();
    if (searchTerm) return `No tasks match '${searchTerm}'`;
    if (status !== 'All') return `No ${status.toLowerCase()} tasks.`;
    if (!this.canWriteOnboarding()) return 'You have no onboarding tasks.';
    return scope === 'Mine' ? 'No tasks are assigned to you.' : 'There are no onboarding tasks yet.';
  });

  constructor() {
    toObservable(this.query)
      .pipe(
        distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
        debounceTime(300),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.load());
  }

  load(): void {
    this.requestedQuery = this.query();
    this.loadFailed.set(false);
    this.paginator.loadFirstPage();
    this.loadSummary();
  }

  loadMore(): void {
    this.paginator.loadNextPage();
  }

  onSearchChange(search: string): void {
    this.updateUrl({ searchTerm: search || null });
  }

  setScope(scope: MyTasksScope): void {
    this.updateUrl({ scope: scope === 'All' ? null : scope });
  }

  setFilter(status: MyTasksStatus): void {
    this.updateUrl({ status: status === 'Open' ? null : status });
  }

  markDone(row: TaskRow): void {
    const cardsBefore = this.groups().length;
    this.flows.update((flows) =>
      flows.map((flow) =>
        flow.flowId === row.flowId
          ? { ...flow, tasks: flow.tasks.map((t) => (t.id === row.id ? { ...t, isDone: true } : t)) }
          : flow,
      ),
    );
    // A card that empties drops out of the server's pages too, so the next page would skip a flow.
    if (this.groups().length < cardsBefore && this.paginator.hasMore) this.paginator.loadFirstPage();
    this.loadSummary();
  }

  private loadSummary(): void {
    const request = ++this.latestSummary;
    this.tasksService
      .getSummary(this.requestQuery())
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((summary) => {
        // An older response that arrives late must not overwrite the counts for a newer query.
        if (request === this.latestSummary) this.summary.set(summary);
      });
  }

  // The list's query, not the URL's: during the debounce they differ, and both requests must match the list.
  private requestQuery(): MyTasksQuery {
    return { ...this.requestedQuery, today: this.datePipe.transform(new Date(), 'yyyy-MM-dd') ?? undefined };
  }

  private updateUrl(queryParams: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  openTask(row: TaskRow): void {
    void this.router.navigate(['/onboarding/tasks', row.id], { state: this.taskState() });
  }

  private toGroup(rows: TaskRow[]): HireGroup {
    const first = rows[0];
    const mine = rows.filter((r) => r.isAssignedToMe).length;
    const overdue = rows.filter((r) => r.status === OnboardingTaskStatus.Overdue).length;
    const start = this.startLabel(first.flowStartDate);
    const counts = [countLabel(rows.length, 'task')];
    if (this.showsAllTasks() && mine > 0) counts.push(`${mine} yours`);
    if (overdue > 0) counts.push(`${overdue} overdue`);
    return {
      flowId: first.flowId,
      name: first.flowFullName,
      meta: first.flowPosition ? `${first.flowPosition} · ${start}` : start,
      counts: counts.join(' · '),
      rows,
    };
  }

  private startLabel(startDate: string): string {
    const days = daysFromToday(startDate);
    if (days > 0) return `starts ${this.datePipe.transform(startDate, 'EEE d MMM')}`;
    return days === 0 ? 'starts today' : relativeStart(startDate);
  }
}

function toRow(task: MyOnboardingTask): TaskRow {
  const days = daysFromToday(task.dueDate);
  let status = OnboardingTaskStatus.NotStarted;
  if (task.isDone) status = OnboardingTaskStatus.Done;
  else if (days < 0) status = OnboardingTaskStatus.Overdue;
  else if (task.isStarted) status = OnboardingTaskStatus.InProgress;

  return {
    ...task,
    status,
    detail: task.fieldLabels.length > 0 ? task.fieldLabels.join(', ') : 'Plain task',
    dueLabel: task.isDone ? '' : dueLabel(task.dueDate, days),
    isDueToday: !task.isDone && days === 0,
  };
}

function matchesFilter(row: TaskRow, filter: MyTasksStatus): boolean {
  switch (filter) {
    case 'Open':
      return row.status !== OnboardingTaskStatus.Done;
    case 'Overdue':
      return row.status === OnboardingTaskStatus.Overdue;
    case 'Done':
      return row.status === OnboardingTaskStatus.Done;
    default:
      return true;
  }
}

function dueLabel(dueDate: string, days: number): string {
  if (days < 0) return `${-days} ${pluralize(-days, 'day')} overdue`;
  return days === 0 ? 'Due today' : relativeStart(dueDate);
}

function countLabel(count: number, noun: string): string {
  return `${count} ${pluralize(count, noun)}`;
}
