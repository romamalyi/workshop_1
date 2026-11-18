import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { ResponsiblePositions } from '../../models/onboarding-settings.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';

@Component({
  selector: 'app-onboarding-responsible-positions-settings',
  standalone: false,
  templateUrl: './onboarding-responsible-positions-settings.component.html',
  styleUrl: '../onboarding-settings-page/onboarding-settings-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingResponsiblePositionsSettingsComponent {
  private readonly settingsService = inject(OnboardingSettingsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly positionsResource = httpResource<ResponsiblePositions>(() => this.settingsService.positionsEndpoint);
  readonly isSaving = signal(false);

  readonly positions = computed<ResponsiblePositions | undefined>(() =>
    this.positionsResource.hasValue() ? this.positionsResource.value() : undefined,
  );

  readonly addable = computed(() => {
    const current = this.positions();
    if (!current) return [];
    const taken = new Set(current.allowed.map((p) => p.name.toLowerCase()));
    return current.available.filter((p) => !taken.has(p.name.toLowerCase()));
  });

  add(name: string | null): void {
    const current = this.positions();
    if (!name || !current) return;
    this.save([...current.allowed.map((p) => p.name), name]);
  }

  remove(name: string): void {
    const current = this.positions();
    if (!current || current.allowed.length <= 1) return;
    this.save(current.allowed.filter((p) => p.name !== name).map((p) => p.name));
  }

  private save(names: string[]): void {
    this.isSaving.set(true);
    this.settingsService
      .saveResponsiblePositions({ positions: names })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.positionsResource.reload();
        },
        error: (err: unknown) => {
          this.isSaving.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Failed to save allowed positions.'));
        },
      });
  }
}
