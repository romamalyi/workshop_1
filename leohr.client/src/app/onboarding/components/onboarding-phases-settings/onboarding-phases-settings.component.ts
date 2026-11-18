import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingPhases } from '../../models/onboarding-settings.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';

const MIN_ONBOARDING_HOURS = 1;
const MAX_ONBOARDING_HOURS = 200;

@Component({
  selector: 'app-onboarding-phases-settings',
  standalone: false,
  templateUrl: './onboarding-phases-settings.component.html',
  styleUrl: '../onboarding-settings-page/onboarding-settings-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingPhasesSettingsComponent {
  private readonly settingsService = inject(OnboardingSettingsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly probationOptions = [1, 3, 6];
  readonly minHours = MIN_ONBOARDING_HOURS;
  readonly maxHours = MAX_ONBOARDING_HOURS;

  readonly phasesResource = httpResource<OnboardingPhases>(() => this.settingsService.phasesEndpoint);

  private readonly draft = signal<OnboardingPhases | null>(null);

  // value() throws while the resource is in an error state, so read it only once it has one.
  private readonly saved = computed<OnboardingPhases | undefined>(() =>
    this.phasesResource.hasValue() ? this.phasesResource.value() : undefined,
  );

  readonly phases = computed<OnboardingPhases | undefined>(() => this.draft() ?? this.saved());

  readonly isDirty = computed<boolean>(() => {
    const draft = this.draft();
    const saved = this.saved();
    return (
      !!draft &&
      !!saved &&
      (draft.probationMonths !== saved.probationMonths || draft.maxOnboardingHours !== saved.maxOnboardingHours)
    );
  });

  readonly hoursValid = computed<boolean>(() => {
    const hours = this.phases()?.maxOnboardingHours;
    return hours != null && Number.isInteger(hours) && hours >= MIN_ONBOARDING_HOURS && hours <= MAX_ONBOARDING_HOURS;
  });

  update(change: Partial<OnboardingPhases>): void {
    const current = this.phases();
    if (current) this.draft.set({ ...current, ...change });
  }

  reset(): void {
    this.draft.set(null);
  }

  save(): void {
    const phases = this.phases();
    if (!phases || !this.hoursValid()) return;

    this.settingsService
      .savePhases(phases)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Phase settings saved.');
          this.phasesResource.set(phases);
          this.draft.set(null);
        },
        error: (err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Failed to save phase settings.'));
        },
      });
  }
}
