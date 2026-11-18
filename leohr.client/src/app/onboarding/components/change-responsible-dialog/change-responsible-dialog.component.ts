import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingFlow } from '../../models/onboarding-flow.model';
import { ResponsibleCandidate } from '../../models/responsible-roles.model';
import { initials } from '../../onboarding-flow.util';
import { OnboardingFlowsService } from '../../services/onboarding-flows.service';

@Component({
  selector: 'app-change-responsible-dialog',
  standalone: false,
  templateUrl: './change-responsible-dialog.component.html',
  styleUrl: './change-responsible-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeResponsibleDialogComponent {
  private readonly flowsService = inject(OnboardingFlowsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly flow = input.required<OnboardingFlow>();
  readonly changed = output<void>();
  readonly dismissed = output<void>();

  readonly initials = initials;
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly selectedId = signal<string | null>(null);
  readonly reassignNotStarted = signal(false);

  private readonly candidates = signal<ResponsibleCandidate[]>([]);

  readonly choices = computed(() => {
    const flow = this.flow();
    const hireName = flow.fullName.trim().toLowerCase();
    return this.candidates().filter((c) => c.id !== flow.responsiblePersonId && c.fullName.toLowerCase() !== hireName);
  });

  readonly currentName = computed(() => this.flow().responsiblePersonName ?? this.flow().responsiblePersonId);

  constructor() {
    this.flowsService
      .getResponsibleCandidates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (candidates) => {
          this.candidates.set(candidates);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not load responsible people.'));
        },
      });
  }

  confirm(): void {
    const personId = this.selectedId();
    if (!personId || this.submitting()) return;

    this.submitting.set(true);
    this.flowsService
      .changeResponsible(this.flow().id, personId, this.reassignNotStarted())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Responsible person changed.');
          this.changed.emit();
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not change the responsible person.'));
        },
      });
  }
}
