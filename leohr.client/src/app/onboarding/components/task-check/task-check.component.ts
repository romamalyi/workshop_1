import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingTaskStatus } from '../../models/onboarding-flow.enums';
import { OnboardingMyTasksService } from '../../services/onboarding-my-tasks.service';

// Mirrors the server's TaskAccess.CanEdit; the server stays the final judge.
export const NO_ASSIGNEE_HINT = 'Assign someone first';
export const NOT_ALLOWED_HINT = 'Only the assignee or an Admin can complete this task';

/** The checkbox of an onboarding task: completes a plain task, opens a task that has fields. */
@Component({
  selector: 'app-task-check',
  templateUrl: './task-check.component.html',
  styleUrl: './task-check.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskCheckComponent {
  private readonly auth = inject(AuthService);
  private readonly tasksService = inject(OnboardingMyTasksService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly taskId = input.required<number>();
  readonly title = input.required<string>();
  readonly status = input.required<OnboardingTaskStatus>();
  readonly assigneeId = input<string | null>(null);
  readonly isStarted = input(false);
  readonly hasFields = input(false);

  readonly open = output<void>();
  readonly completed = output<void>();

  readonly Status = OnboardingTaskStatus;
  readonly completing = signal(false);

  readonly isDone = computed(() => this.status() === OnboardingTaskStatus.Done);
  readonly blockedReason = computed<string | null>(() => {
    if (this.isDone()) return null;
    const assigneeId = this.assigneeId();
    if (!assigneeId) return NO_ASSIGNEE_HINT;
    return this.auth.isOnboardingAdmin() || assigneeId === this.auth.currentUserId ? null : NOT_ALLOWED_HINT;
  });
  readonly disabled = computed(() => this.isDone() || this.completing() || this.blockedReason() !== null);
  readonly label = computed(() => `${this.hasFields() ? 'Open task' : 'Mark as done'}: ${this.title()}`);

  onCheck(event: Event): void {
    // `checked` is bound to the task's status, so the browser must not toggle it.
    event.preventDefault();
    if (this.disabled()) return;
    if (this.hasFields()) {
      this.open.emit();
      return;
    }

    this.completing.set(true);
    this.tasksService
      .complete(this.taskId())
      .pipe(
        catchError((err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Could not mark the task as done.'));
          return EMPTY;
        }),
        finalize(() => this.completing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Task marked as done.');
          // Not tied to this component: completing removes the row, and the badge must still update.
          this.tasksService
            .refresh()
            .pipe(catchError(() => EMPTY))
            .subscribe();
          this.completed.emit();
        },
      });
  }
}
