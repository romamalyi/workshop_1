import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { EmployeeHistorySettings } from '../../models/employee-history-settings.model';
import { EmployeesService } from '../../services/employees.service';

interface HistoryToggle {
  key: keyof EmployeeHistorySettings;
  label: string;
  hint: string;
}

@Component({
  selector: 'app-employee-settings-page',
  standalone: false,
  templateUrl: './employee-settings-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeSettingsPageComponent {
  private readonly employeesService = inject(EmployeesService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly toggles: HistoryToggle[] = [
    { key: 'showManager', label: 'Show manager', hint: 'Display manager column in employment history' },
    { key: 'showMentor', label: 'Show mentor', hint: 'Display mentor column in employment history' },
    {
      key: 'showEmployment',
      label: 'Show employment type',
      hint: 'Display employment type column in employment history',
    },
  ];

  readonly activeTab = signal<'history' | 'email'>('history');
  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly saving = signal(false);
  private readonly saved = signal<EmployeeHistorySettings | null>(null);
  readonly settings = signal<EmployeeHistorySettings | null>(null);

  readonly isDirty = computed(() => {
    const settings = this.settings();
    const saved = this.saved();
    return !!settings && !!saved && this.toggles.some((t) => settings[t.key] !== saved[t.key]);
  });

  constructor() {
    this.employeesService
      .getHistorySettings()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings) => {
          this.saved.set(settings);
          this.settings.set(settings);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.loadFailed.set(true);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to load employee history settings.'));
        },
      });
  }

  toggle(key: keyof EmployeeHistorySettings): void {
    const settings = this.settings();
    if (settings) this.settings.set({ ...settings, [key]: !settings[key] });
  }

  reset(): void {
    this.settings.set(this.saved());
  }

  save(): void {
    const settings = this.settings();
    if (!settings || !this.isDirty() || this.saving()) return;
    this.saving.set(true);
    this.employeesService
      .saveHistorySettings(settings)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saved.set(settings);
          this.saving.set(false);
          this.toastService.success('Employee history settings saved.');
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to save employee history settings.'));
        },
      });
  }
}
