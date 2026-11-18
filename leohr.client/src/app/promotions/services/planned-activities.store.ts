import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PlannedActivity, PromotionsService } from './promotions.service';

@Injectable({ providedIn: 'root' })
export class PlannedActivitiesStore {
  private readonly promotionsService = inject(PromotionsService);
  private readonly toastService = inject(ToastService);

  private readonly _items = signal<PlannedActivity[]>([]);
  readonly count = computed(() => this._items().length);

  refresh(): void {
    this.promotionsService
      .getPlannedActivities()
      .pipe(
        catchError((err) => {
          this.toastService.error(firstApiErrorMessage(err, 'Could not load planned activities.'));
          return of<PlannedActivity[]>([]);
        }),
      )
      .subscribe((list) => this._items.set(list));
  }
}
