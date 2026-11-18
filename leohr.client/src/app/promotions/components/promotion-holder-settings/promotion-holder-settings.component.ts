import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { Employee } from 'src/app/employees/models/employee.model';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SearchOption, toEmployeeOptions } from 'src/app/shared/models/search-option.model';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PromotionsService } from '../../services/promotions.service';

@Component({
  selector: 'app-promotion-holder-settings',
  templateUrl: './promotion-holder-settings.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionHolderSettingsComponent {
  private readonly promotionsService = inject(PromotionsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly loading = signal(true);
  public readonly loadFailed = signal(false);
  public readonly saving = signal(false);
  public readonly employees = signal<Employee[]>([]);
  public readonly savedHolderId = signal('');
  public readonly holderId = signal('');

  public readonly holderName = computed(() => {
    const holder = this.employees().find((e) => e.id === this.holderId());
    return holder ? `${holder.firstName} ${holder.lastName}` : '';
  });

  public readonly holderOptions = computed(() => toEmployeeOptions(this.employees()));

  public readonly isDirty = computed(() => this.holderId() !== this.savedHolderId());

  constructor() {
    forkJoin({
      employees: this.employeesService.getActiveEmployees(),
      holder: this.promotionsService.getDefaultHolder(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ employees, holder }) => {
          this.employees.set(employees);
          this.savedHolderId.set(holder.employeeId);
          this.holderId.set(holder.employeeId);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.loadFailed.set(true);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to load the default promotion holder.'));
        },
      });
  }

  selectHolder(option: SearchOption | null): void {
    if (option) this.holderId.set(option.id);
  }

  reset(): void {
    this.holderId.set(this.savedHolderId());
  }

  save(): void {
    if (!this.isDirty() || this.saving()) return;
    const employeeId = this.holderId();
    this.saving.set(true);
    this.promotionsService
      .saveDefaultHolder(employeeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savedHolderId.set(employeeId);
          this.saving.set(false);
          this.toastService.success('Default promotion holder saved.');
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to save the default promotion holder.'));
        },
      });
  }
}
