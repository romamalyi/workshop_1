import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PendingApproval, PromotionsService } from './promotions.service';

@Injectable({ providedIn: 'root' })
export class PendingApprovalsStore {
  private readonly promotionsService = inject(PromotionsService);
  private readonly toastService = inject(ToastService);

  private readonly _items = signal<PendingApproval[]>([]);
  readonly items = this._items.asReadonly();
  readonly count = computed(() => this._items().length);

  refresh(): void {
    this.promotionsService
      .getPendingApprovals()
      .pipe(
        catchError((err) => {
          this.toastService.error(firstApiErrorMessage(err, 'Could not load pending approvals.'));
          return of<PendingApproval[]>([]);
        }),
      )
      .subscribe((list) => this._items.set(list));
  }

  drop(employeeActivityId: number): void {
    this._items.update((list) => list.filter((i) => i.employeeActivityId !== employeeActivityId));
  }
}
