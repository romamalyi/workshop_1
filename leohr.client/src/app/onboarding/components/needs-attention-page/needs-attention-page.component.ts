import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { RecheckAssignment } from '../../models/onboarding-flow.model';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { avatarColor, initials, taskCountLabel } from '../../onboarding-flow.util';
import { OnboardingAttentionService } from '../../services/onboarding-attention.service';

@Component({
  selector: 'app-needs-attention-page',
  templateUrl: './needs-attention-page.component.html',
  styleUrl: './needs-attention-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NeedsAttentionPageComponent {
  private readonly attention = inject(OnboardingAttentionService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly phaseLabels = PHASE_LABELS;
  readonly avatarColor = avatarColor;
  readonly initials = initials;

  readonly loading = signal(true);
  readonly tasks = this.attention.unassignedTasks;

  readonly checking = signal(false);
  readonly applying = signal(false);
  readonly recheckPreview = signal<RecheckAssignment[] | null>(null);

  constructor() {
    this.attention
      .refresh()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loading.set(false),
        error: (err: unknown) => {
          this.loading.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not load unassigned tasks.'));
        },
      });
  }

  recheck(): void {
    this.checking.set(true);
    this.attention
      .previewRecheck()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (assignments) => {
          this.checking.set(false);
          if (assignments.length === 0) {
            this.toast.warning('No listed task has a mapped role yet — map a person in Responsible Roles first.');
            return;
          }
          this.recheckPreview.set(assignments);
        },
        error: (err: unknown) => {
          this.checking.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not recheck role mappings.'));
        },
      });
  }

  confirmRecheck(): void {
    const preview = this.recheckPreview();
    if (!preview) return;

    this.applying.set(true);
    this.attention
      .applyRecheck(preview)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ assignedCount }) => {
          this.applying.set(false);
          this.recheckPreview.set(null);
          const skipped = preview.length - assignedCount;
          this.toast.success(
            skipped > 0
              ? `${taskCountLabel(assignedCount)} assigned; ${skipped} skipped because their mapping changed.`
              : `${taskCountLabel(assignedCount)} assigned.`,
          );
        },
        error: (err: unknown) => {
          this.applying.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not apply role mappings.'));
        },
      });
  }
}
