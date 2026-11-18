import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, EMPTY, Subject, switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { SENIORITY_LABELS } from '../../../promotion-criteria/promotion-criteria.model';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingPhase } from '../../onboarding-template.enums';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { formatShortDueRule } from '../../onboarding-template.util';
import { FlowWarningKind, OnboardingTaskStatus } from '../../models/onboarding-flow.enums';
import { TaskNavigationState } from '../../models/onboarding-task-detail.model';
import {
  FLOW_WARNING_TITLES,
  FlowWarning,
  OnboardingFlow,
  OnboardingFlowTask,
  TASK_STATUS_CLASSES,
  TASK_STATUS_LABELS,
} from '../../models/onboarding-flow.model';
import { EmployeeCandidate } from '../../../shared/models/employee-candidate.model';
import {
  doneTaskCount,
  initials,
  progressPercent,
  readFlowsQueryParams,
  relativeStart,
} from '../../onboarding-flow.util';
import { OnboardingFlowsService } from '../../services/onboarding-flows.service';
import { OnboardingMyTasksService } from '../../services/onboarding-my-tasks.service';

interface TaskRow extends OnboardingFlowTask {
  isUnassigned: boolean;
  statusLabel: string;
  statusClass: string;
  dueRule: string;
}

interface WarningBanner {
  kind: FlowWarningKind;
  title: string;
  text: string;
  action: 'change' | 'settings';
}

interface PhaseGroup {
  phase: OnboardingPhase;
  label: string;
  firstDue: string;
  lastDue: string;
  doneCount: number;
  rows: TaskRow[];
}

@Component({
  selector: 'app-onboarding-flow-overview',
  templateUrl: './onboarding-flow-overview.component.html',
  styleUrl: './onboarding-flow-overview.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingFlowOverviewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly flowsService = inject(OnboardingFlowsService);
  private readonly myTasks = inject(OnboardingMyTasksService);
  private readonly auth = inject(AuthService);

  readonly Status = OnboardingTaskStatus;
  readonly flowsQueryParams = readFlowsQueryParams(this.router);
  readonly taskState: TaskNavigationState = { from: 'flow', flowsQueryParams: this.flowsQueryParams ?? undefined };
  readonly levelLabels = SENIORITY_LABELS;
  readonly initials = initials;
  readonly relativeStart = relativeStart;

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly flow = signal<OnboardingFlow | null>(null);

  private readonly fragment = toSignal(this.route.fragment, { initialValue: null });
  readonly highlightedTaskId = computed(() => {
    const match = /^task-(\d+)$/.exec(this.fragment() ?? '');
    return match ? Number(match[1]) : null;
  });

  readonly isOnboardingAdmin = this.auth.isOnboardingAdmin;

  private readonly dismissedKinds = signal<ReadonlySet<FlowWarningKind>>(new Set());

  readonly personWarning = computed(
    () => this.flow()?.warnings.find((w) => w.kind !== FlowWarningKind.RoleDefaultInactive) ?? null,
  );

  readonly personBadge = computed(() =>
    this.personWarning()?.kind === FlowWarningKind.ResponsibleInactive ? 'No longer active' : 'Position not allowed',
  );

  readonly banners = computed<WarningBanner[]>(() => {
    const flow = this.flow();
    if (!flow) return [];
    const dismissed = this.dismissedKinds();
    return flow.warnings.filter((w) => !dismissed.has(w.kind)).map((w) => toBanner(w, flow));
  });

  readonly changeDialogOpen = signal(false);

  dismiss(kind: FlowWarningKind): void {
    this.dismissedKinds.update((kinds) => new Set([...kinds, kind]));
  }

  onResponsibleChanged(): void {
    this.changeDialogOpen.set(false);
    this.reload();
  }

  readonly isResponsibleMe = computed(() => {
    const userId = this.auth.userId();
    return !!userId && this.flow()?.responsiblePersonId === userId;
  });

  readonly doneCount = computed(() => {
    const flow = this.flow();
    return flow ? doneTaskCount(flow) : 0;
  });

  readonly percent = computed(() => {
    const flow = this.flow();
    return flow ? progressPercent(flow) : 0;
  });

  private readonly rows = computed<TaskRow[]>(() => (this.flow()?.tasks ?? []).map(toRow));

  readonly unassignedRows = computed(() => this.rows().filter((r) => r.isUnassigned));

  readonly bannerTitle = computed(() => {
    const count = this.unassignedRows().length;
    return count === 1 ? '1 task was created without an assignee' : `${count} tasks were created without an assignee`;
  });

  readonly phaseGroups = computed<PhaseGroup[]>(() => {
    const rows = this.rows();
    return Object.values(OnboardingPhase)
      .map((phase) => rows.filter((r) => r.phase === phase))
      .filter((phaseRows) => phaseRows.length > 0)
      .map((phaseRows) => {
        const dueDates = phaseRows.map((r) => r.dueDate).sort();
        return {
          phase: phaseRows[0].phase,
          label: PHASE_LABELS[phaseRows[0].phase],
          firstDue: dueDates[0],
          lastDue: dueDates[dueDates.length - 1],
          doneCount: phaseRows.filter((r) => r.status === OnboardingTaskStatus.Done).length,
          rows: phaseRows,
        };
      });
  });

  // switchMap drops a stale response when a newer reload starts before it arrives.
  private readonly reload$ = new Subject<void>();

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.reload$
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.loadFailed.set(false);
          return this.flowsService.getById(id).pipe(
            catchError((err: unknown) => {
              this.loading.set(false);
              this.loadFailed.set(true);
              this.toast.error(firstApiErrorMessage(err, 'Could not load the onboarding flow.'));
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((flow) => {
        this.flow.set(flow);
        this.loading.set(false);
        this.scrollToHighlightedTask();
      });

    this.reload();
  }

  reload(): void {
    this.reload$.next();
  }

  openTask(taskId: number): void {
    void this.router.navigate(['/onboarding/tasks', taskId], { state: this.taskState });
  }

  onCompleted(taskId: number): void {
    this.flow.update(
      (flow) =>
        flow && {
          ...flow,
          tasks: flow.tasks.map((t) => (t.id === taskId ? { ...t, status: OnboardingTaskStatus.Done } : t)),
        },
    );
  }

  onAssigned(taskId: number, assignee: EmployeeCandidate): void {
    this.flow.update(
      (flow) =>
        flow && {
          ...flow,
          tasks: flow.tasks.map((t) =>
            t.id === taskId
              ? {
                  ...t,
                  assigneeId: assignee.id,
                  assigneeName: assignee.fullName,
                  status: OnboardingTaskStatus.NotStarted,
                }
              : t,
          ),
        },
    );
    this.myTasks
      .refresh()
      .pipe(
        catchError(() => EMPTY),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  private scrollToHighlightedTask(): void {
    const taskId = this.highlightedTaskId();
    if (taskId === null) return;
    afterNextRender(() => document.getElementById(`task-${taskId}`)?.scrollIntoView({ block: 'center' }), {
      injector: this.injector,
    });
  }
}

function toBanner(warning: FlowWarning, flow: OnboardingFlow): WarningBanner {
  const name = flow.responsiblePersonName ?? 'The responsible person';
  const role = flow.responsiblePersonRoleName || 'another position';
  const unchanged = 'Nothing changed automatically: their tasks and notifications continue as before.';
  const handOver = 'Keep them or hand the flow to someone in an allowed position.';

  switch (warning.kind) {
    case FlowWarningKind.ResponsiblePositionNotAllowed:
      return {
        kind: warning.kind,
        title: FLOW_WARNING_TITLES[warning.kind],
        text: `${name} is still responsible for this flow, but ${role} was removed from the allowed positions in Onboarding settings. ${unchanged} ${handOver}`,
        action: 'change',
      };
    case FlowWarningKind.ResponsiblePositionChanged:
      return {
        kind: warning.kind,
        title: FLOW_WARNING_TITLES[warning.kind],
        text: `${name} is still responsible for this flow, but now holds ${role}, which is not an allowed position in Onboarding settings. ${unchanged} ${handOver}`,
        action: 'change',
      };
    case FlowWarningKind.ResponsibleInactive:
      return {
        kind: warning.kind,
        title: FLOW_WARNING_TITLES[warning.kind],
        text: `${name} is still responsible for this flow, but was deactivated or left the company. ${unchanged} Hand the flow to someone in an allowed position.`,
        action: 'change',
      };
    case FlowWarningKind.RoleDefaultInactive:
      return {
        kind: warning.kind,
        title: FLOW_WARNING_TITLES[warning.kind],
        text: `The default person for ${warning.roleNames.join(', ')} is no longer active. Nothing changed automatically: existing tasks keep their assignees, and new tasks of that role will not resolve to anyone until a new default is set.`,
        action: 'settings',
      };
  }
}

function toRow(task: OnboardingFlowTask): TaskRow {
  const isUnassigned = !task.assigneeId && task.status !== OnboardingTaskStatus.Done;
  return {
    ...task,
    isUnassigned,
    statusLabel: isUnassigned ? 'Unassigned' : TASK_STATUS_LABELS[task.status],
    statusClass: isUnassigned ? 'ob-attention--unresolved' : TASK_STATUS_CLASSES[task.status],
    dueRule: formatShortDueRule(task.dueOffsetDays, task.dueDirection, task.dueAnchor),
  };
}
