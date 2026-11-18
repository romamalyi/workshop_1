import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { Employee } from 'src/app/employees/models/employee.model';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SearchOption, toEmployeeOptions } from 'src/app/shared/models/search-option.model';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PromotionDetail, PromotionsService } from '../../services/promotions.service';

@Component({
  selector: 'app-promotion-edit-modal',
  templateUrl: './promotion-edit-modal.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionEditModalComponent implements OnInit {
  private readonly promotionsService = inject(PromotionsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly toastService = inject(ToastService);

  public readonly promotion = input.required<PromotionDetail>();
  public readonly saved = output<void>();
  public readonly closeModal = output<void>();

  private readonly employees = toSignal(
    this.employeesService.getActiveEmployees().pipe(
      catchError((err) => {
        this.toastService.error(firstApiErrorMessage(err, 'Failed to load employees.'));
        return of([] as Employee[]);
      }),
    ),
    { initialValue: [] as Employee[] },
  );

  public readonly holderId = signal('');
  public readonly toRole = signal('');
  public readonly saving = signal(false);

  public readonly holderName = computed(() => {
    const holder = this.employees().find((e) => e.id === this.holderId());
    if (holder) return `${holder.firstName} ${holder.lastName}`;
    return this.holderId() === this.promotion().promotionHolderId ? (this.promotion().promotionHolderName ?? '') : '';
  });

  public readonly holderOptions = computed(() =>
    toEmployeeOptions(this.employees().filter((e) => e.id !== this.promotion().employeeId)),
  );

  public readonly levelOptions = computed(() => {
    const levels = this.promotion().targetLevels ?? [];
    const current = this.promotion().toRole;
    return levels.includes(current) ? levels : [current, ...levels];
  });

  public readonly levelChanged = computed(() => this.toRole() !== this.promotion().toRole);

  public readonly canSave = computed(
    () =>
      !this.saving() &&
      !!this.holderId() &&
      (this.levelChanged() || this.holderId() !== this.promotion().promotionHolderId),
  );

  ngOnInit(): void {
    this.holderId.set(this.promotion().promotionHolderId);
    this.toRole.set(this.promotion().toRole);
  }

  // Closing mid-save would destroy the modal before `saved` reaches the page, leaving stale data.
  close(): void {
    if (!this.saving()) this.closeModal.emit();
  }

  selectHolder(option: SearchOption | null): void {
    if (option) this.holderId.set(option.id);
  }

  save(): void {
    if (!this.canSave()) return;
    this.saving.set(true);
    this.promotionsService
      .update(this.promotion().id, { promotionHolderId: this.holderId(), toRole: this.toRole() })
      .subscribe({
        next: () => {
          this.toastService.success('Promotion updated.');
          this.saved.emit();
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to update the promotion.'));
        },
      });
  }
}
