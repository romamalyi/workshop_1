import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingReminders } from '../../models/onboarding-settings.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';

@Component({
  selector: 'app-onboarding-reminders-settings',
  standalone: false,
  templateUrl: './onboarding-reminders-settings.component.html',
  styleUrl: '../onboarding-settings-page/onboarding-settings-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingRemindersSettingsComponent {
  private readonly settingsService = inject(OnboardingSettingsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly dayOptions = [7, 14, 30];

  readonly remindersResource = httpResource<OnboardingReminders>(() => this.settingsService.remindersEndpoint);

  private readonly draft = signal<OnboardingReminders | null>(null);

  // value() throws while the resource is in an error state, so read it only once it has one.
  private readonly saved = computed<OnboardingReminders | undefined>(() =>
    this.remindersResource.hasValue() ? this.remindersResource.value() : undefined,
  );

  readonly reminders = computed<OnboardingReminders | undefined>(() => this.draft() ?? this.saved());

  readonly isDirty = computed<boolean>(() => {
    const draft = this.draft();
    const saved = this.saved();
    return (
      !!draft &&
      !!saved &&
      (draft.daysBeforeStartToNotify !== saved.daysBeforeStartToNotify ||
        draft.weeklyDigestEnabled !== saved.weeklyDigestEnabled ||
        draft.overdueAlertsEnabled !== saved.overdueAlertsEnabled)
    );
  });

  update(change: Partial<OnboardingReminders>): void {
    const current = this.reminders();
    if (current) this.draft.set({ ...current, ...change });
  }

  reset(): void {
    this.draft.set(null);
  }

  save(): void {
    const reminders = this.reminders();
    if (!reminders) return;

    this.settingsService
      .saveReminders(reminders)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Reminder settings saved.');
          this.remindersResource.set(reminders);
          this.draft.set(null);
        },
        error: (err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Failed to save reminder settings.'));
        },
      });
  }
}
