import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, EMPTY, finalize, forkJoin, map, Observable, of, switchMap, take, tap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingTaskStatus } from '../../models/onboarding-flow.enums';
import { TASK_STATUS_CLASSES, TASK_STATUS_LABELS } from '../../models/onboarding-flow.model';
import {
  OnboardingTaskDetail,
  OnboardingTaskField,
  TaskNavigationState,
} from '../../models/onboarding-task-detail.model';
import { EmployeeCandidate } from '../../../shared/models/employee-candidate.model';
import { SearchOption, toCandidate, toSearchOptions } from '../../../shared/models/search-option.model';
import { NO_ROLE_HOLDERS, personAvatar } from '../../onboarding-flow.util';
import { TaskFieldsAutosave } from '../../onboarding-task-autosave';
import { LoadState, TaskLoadResult } from '../../onboarding-task.resolver';
import { fieldHelperText, isFieldFilled, taskActivityTitle } from '../../onboarding-task-detail.util';
import { TemplateFieldType } from '../../onboarding-template.enums';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { OnboardingAttentionService } from '../../services/onboarding-attention.service';
import { OnboardingMyTasksService } from '../../services/onboarding-my-tasks.service';
import { OnboardingTaskDetailService } from '../../services/onboarding-task-detail.service';

const UNSAVED_CHANGES_MESSAGE = 'Your changes could not be saved. Retry before continuing.';

@Component({
  selector: 'app-onboarding-task-detail',
  templateUrl: './onboarding-task-detail.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(window:pagehide)': 'saveOnUnload()' },
})
export class OnboardingTaskDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(OnboardingTaskDetailService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly myTasks = inject(OnboardingMyTasksService);
  private readonly attention = inject(OnboardingAttentionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly phaseLabels = PHASE_LABELS;
  readonly statusLabels = TASK_STATUS_LABELS;
  readonly statusClasses = TASK_STATUS_CLASSES;
  readonly fieldHelperText = fieldHelperText;

  optionsFor(field: OnboardingTaskField): SearchOption[] {
    return this.personFieldOptions()[field.personRestrictionKey ?? ''] ?? [];
  }
  readonly FieldType = TemplateFieldType;
  readonly Status = OnboardingTaskStatus;
  readonly activityTitle = taskActivityTitle;
  readonly noRoleHoldersText = NO_ROLE_HOLDERS;
  readonly personAvatar = personAvatar;
  readonly canWriteOnboarding = this.auth.canWriteOnboarding;

  readonly task = signal<OnboardingTaskDetail | null>(null);
  readonly state = signal<LoadState>('loading');
  private readonly personFieldCandidates = signal<Record<string, EmployeeCandidate[]>>({});
  private readonly personFieldOptions = computed(() =>
    Object.fromEntries(Object.entries(this.personFieldCandidates()).map(([key, list]) => [key, toSearchOptions(list)])),
  );

  private readonly autosave = signal<TaskFieldsAutosave | null>(null);
  readonly saveState = computed(() => this.autosave()?.state() ?? 'idle');

  readonly canEditFields = computed(() => {
    const task = this.task();
    return !!task && task.canEdit && task.status !== OnboardingTaskStatus.Done;
  });

  readonly busy = signal(false);
  readonly confirmingComplete = signal(false);

  readonly changingAssignee = signal(false);
  // null while the role's holders are loading.
  readonly assigneeCandidates = signal<EmployeeCandidate[] | null>(null);
  readonly assigneeOptions = computed(() => toSearchOptions(this.assigneeCandidates() ?? []));
  readonly pendingAssignee = signal<EmployeeCandidate | null>(null);

  private readonly requirementsMet = computed(() => !!this.task()?.fields.every(isFieldFilled));

  readonly canComplete = computed(
    () => this.canEditFields() && this.requirementsMet() && this.saveState() !== 'error' && !this.busy(),
  );

  readonly completeHint = computed(() => {
    if (!this.canEditFields()) return '';
    if (this.task()?.fields.length === 0) return 'No fields on this task — mark it complete once it is done.';
    if (!this.requirementsMet()) return 'Fill every field and tick every checklist item to complete this task.';
    return this.saveState() === 'error' ? 'Retry saving your changes to complete this task.' : '';
  });

  readonly completeTitle = computed(() => `Mark '${this.task()?.title ?? ''}' as done?`);

  private readonly navigation = readNavigationState(this.router);
  private readonly origin = this.navigation?.from ?? null;

  readonly back = computed(() => {
    const from = this.origin ?? (this.canWriteOnboarding() ? 'flow' : 'my-tasks');
    if (from === 'flow' && this.canWriteOnboarding()) {
      const task = this.task();
      const flowsQueryParams = this.navigation?.flowsQueryParams;
      return task
        ? {
            label: `← ${task.flowFullName}`,
            link: ['/onboarding/flows', task.flowId],
            queryParams: null,
            state: { flowsQueryParams },
          }
        : { label: '← Flows', link: ['/onboarding/flows'], queryParams: flowsQueryParams ?? null };
    }
    return {
      label: this.canWriteOnboarding() ? '← Tasks' : '← My Tasks',
      link: ['/onboarding/my-tasks'],
      queryParams: this.navigation?.queryParams ?? null,
    };
  });

  readonly assigneeLabel = computed(() => {
    const task = this.task();
    if (!task) return '';
    return task.assigneeName ?? task.assigneeId ?? 'Unassigned';
  });

  constructor() {
    this.route.data
      .pipe(
        map((data) => data['result'] as TaskLoadResult),
        tap(() => {
          this.disposeAutosave();
          this.changingAssignee.set(false);
        }),
        takeUntilDestroyed(),
      )
      .subscribe(({ task, state }) => {
        this.task.set(task);
        this.state.set(state);
        if (task) this.startAutosave(task);
      });

    this.destroyRef.onDestroy(() => this.disposeAutosave());
  }

  setNote(field: OnboardingTaskField, value: string): void {
    if (!this.canEditFields()) return;
    this.patchField(field.id, { noteValue: value });
    this.autosave()?.queue({ fieldId: field.id, noteValue: value });
  }

  toggleItem(field: OnboardingTaskField, itemId: number, isChecked: boolean): void {
    if (!this.canEditFields()) return;
    const checklistItems = field.checklistItems.map((item) => (item.id === itemId ? { ...item, isChecked } : item));
    this.patchField(field.id, { checklistItems });
    this.autosave()?.queue({
      fieldId: field.id,
      checkedItemIds: checklistItems.filter((item) => item.isChecked).map((item) => item.id),
    });
  }

  setPerson(field: OnboardingTaskField, person: SearchOption | null): void {
    if (!this.canEditFields()) return;
    this.patchField(field.id, { personId: person?.id ?? null, personName: person?.label ?? null });
    this.autosave()?.queue({ fieldId: field.id, personId: person?.id ?? null });
  }

  start(): void {
    this.runAction(OnboardingTaskStatus.InProgress, (id) => this.service.start(id));
  }

  undoStart(): void {
    this.runAction(OnboardingTaskStatus.NotStarted, (id) => this.service.undoStart(id));
  }

  reopen(): void {
    this.runAction(OnboardingTaskStatus.InProgress, (id) => this.service.reopen(id));
  }

  complete(): void {
    this.confirmingComplete.set(false);
    this.runAction(OnboardingTaskStatus.Done, (id) => this.service.complete(id));
  }

  startChangeAssignee(): void {
    const task = this.task();
    if (!task) return;

    this.changingAssignee.set(true);
    this.assigneeCandidates.set(null);
    this.attention
      .candidates(task.responsibleRoleKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (candidates) => this.assigneeCandidates.set(candidates),
        error: (err: unknown) => {
          this.changingAssignee.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not load employees to assign.'));
        },
      });
  }

  pickAssignee(option: SearchOption | null): void {
    const task = this.task();
    if (!task || !option) return;
    const candidate = toCandidate(option);

    if (candidate.id === task.assigneeId) {
      this.changingAssignee.set(false);
    } else if (task.assigneeId) {
      this.pendingAssignee.set(candidate);
    } else {
      this.saveAssignee(candidate);
    }
  }

  confirmAssignee(candidate: EmployeeCandidate): void {
    this.pendingAssignee.set(null);
    this.saveAssignee(candidate);
  }

  leaveChecklist(event: FocusEvent, list: HTMLElement): void {
    if (!list.contains(event.relatedTarget as Node | null)) this.flushSave();
  }

  flushSave(): void {
    void this.autosave()?.flush();
  }

  saveOnUnload(): void {
    const task = this.task();
    const batch = this.autosave()?.takePending();
    if (task && batch?.length) this.service.updateFieldsOnUnload(task.id, batch);
  }

  private runAction(next: OnboardingTaskStatus, call: (taskId: number) => Observable<void>): void {
    const task = this.task();
    const autosave = this.autosave();
    if (!task || !autosave || this.busy()) return;

    const previous = task.status;
    this.busy.set(true);
    this.setStatus(next);

    this.afterSave(autosave, () => call(task.id).pipe(map(() => true)))
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (done) => {
          if (done) {
            this.refreshMyTasks();
            return;
          }
          this.setStatus(previous);
          this.toast.error(UNSAVED_CHANGES_MESSAGE);
        },
        error: (err: unknown) => {
          this.setStatus(previous);
          this.toast.error(firstApiErrorMessage(err, 'Could not update the task.'));
        },
      });
  }

  // The status reset and new activity entry come from the server, so the task is reloaded after the change.
  private saveAssignee(assignee: EmployeeCandidate): void {
    const task = this.task();
    const autosave = this.autosave();
    if (!task || !autosave || this.busy()) return;

    this.busy.set(true);
    this.afterSave(autosave, () =>
      this.attention.assign(task.id, assignee.id).pipe(switchMap(() => this.service.get(task.id))),
    )
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          if (!updated) {
            this.toast.error(UNSAVED_CHANGES_MESSAGE);
            return;
          }
          this.task.set(updated);
          this.loadPersonFieldCandidates(updated);
          this.changingAssignee.set(false);
          this.toast.success(`${updated.title} assigned to ${assignee.fullName}`);
          this.refreshMyTasks();
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Could not change the assignee.')),
      });
  }

  /** Flushes pending field edits first; emits null without calling when they could not be saved. */
  private afterSave<T>(autosave: TaskFieldsAutosave, call: () => Observable<T>): Observable<T | null> {
    return autosave.flush().pipe(
      take(1),
      switchMap((saved) => (saved ? call() : of(null))),
    );
  }

  private refreshMyTasks(): void {
    this.myTasks
      .refresh()
      .pipe(
        catchError(() => EMPTY),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  private setStatus(status: OnboardingTaskStatus): void {
    this.task.update((task) => (task ? { ...task, status } : task));
  }

  private startAutosave(task: OnboardingTaskDetail): void {
    this.autosave.set(new TaskFieldsAutosave((fields) => this.service.updateFields(task.id, fields)));
    this.loadPersonFieldCandidates(task);
  }

  private loadPersonFieldCandidates(task: OnboardingTaskDetail): void {
    if (Object.keys(this.personFieldCandidates()).length > 0) return;

    const restrictionKeys = [
      ...new Set(
        task.fields
          .filter((f) => f.fieldType === TemplateFieldType.EmployeePicker)
          .map((f) => f.personRestrictionKey ?? ''),
      ),
    ];

    if (task.canEdit && restrictionKeys.length > 0) {
      forkJoin(restrictionKeys.map((key) => this.service.getPersonFieldCandidates(key || null)))
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (lists) =>
            this.personFieldCandidates.set(Object.fromEntries(restrictionKeys.map((key, i) => [key, lists[i]]))),
          error: (err: unknown) =>
            this.toast.error(firstApiErrorMessage(err, 'Could not load the employee directory.')),
        });
    }
  }

  private disposeAutosave(): void {
    this.autosave()?.dispose();
    this.autosave.set(null);
  }

  private patchField(fieldId: number, patch: Partial<OnboardingTaskField>): void {
    this.task.update((task) =>
      task ? { ...task, fields: task.fields.map((f) => (f.id === fieldId ? { ...f, ...patch } : f)) } : task,
    );
  }
}

function readNavigationState(router: Router): TaskNavigationState | null {
  const state = (router.currentNavigation()?.extras.state ?? history.state) as Partial<TaskNavigationState> | null;
  return state?.from === 'flow' || state?.from === 'my-tasks'
    ? { from: state.from, queryParams: state.queryParams, flowsQueryParams: state.flowsQueryParams }
    : null;
}
