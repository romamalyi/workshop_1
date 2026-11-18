import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { EmployeeCandidate } from '../../../shared/models/employee-candidate.model';
import { SearchOption, toCandidate, toSearchOptions } from '../../../shared/models/search-option.model';
import { NO_ROLE_HOLDERS } from '../../onboarding-flow.util';
import { OnboardingAttentionService } from '../../services/onboarding-attention.service';

@Component({
  selector: 'app-task-assignee-select',
  templateUrl: './task-assignee-select.component.html',
  styleUrl: './task-assignee-select.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskAssigneeSelectComponent {
  private readonly attention = inject(OnboardingAttentionService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly taskId = input.required<number>();
  readonly taskTitle = input.required<string>();
  readonly roleKey = input<string | null>(null);

  readonly assigned = output<EmployeeCandidate>();

  readonly noRoleHoldersText = NO_ROLE_HOLDERS;

  readonly saving = signal(false);

  readonly candidates = toSignal(
    toObservable(this.roleKey).pipe(
      switchMap((roleKey) =>
        this.attention.candidates(roleKey).pipe(
          catchError((err: unknown) => {
            this.toast.error(firstApiErrorMessage(err, 'Could not load employees to assign.'));
            return of<EmployeeCandidate[]>([]);
          }),
        ),
      ),
    ),
    { initialValue: null },
  );

  readonly options = computed(() => toSearchOptions(this.candidates() ?? []));

  readonly noRoleHolders = computed(() => this.roleKey() !== null && this.candidates()?.length === 0);

  assign(option: SearchOption | null): void {
    if (!option) return;
    const candidate = toCandidate(option);

    this.saving.set(true);
    this.attention
      .assign(this.taskId(), candidate.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toast.success(`${this.taskTitle()} assigned to ${candidate.fullName}`);
          this.assigned.emit(candidate);
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not assign the task.'));
        },
      });
  }
}
