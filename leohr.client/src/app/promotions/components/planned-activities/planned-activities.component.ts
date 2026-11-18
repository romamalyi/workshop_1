import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import {
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  EMPTY,
  filter,
  map,
  of,
  shareReplay,
  switchMap,
  take,
} from 'rxjs';
import { ACTIVITY_CATEGORY, ActivityCategory, categoryBackground } from 'src/app/activities/activity.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { ALL_HR_MANAGERS, DefaultHrFilterService } from 'src/app/employees/services/default-hr-filter.service';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PlannedActivity, PromotionsService } from '../../services/promotions.service';

const CATEGORIES = Object.values(ActivityCategory);

@Component({
  selector: 'app-planned-activities',
  templateUrl: './planned-activities.component.html',
  styleUrl: './planned-activities.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlannedActivitiesComponent {
  private readonly promotionsService = inject(PromotionsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly defaultHrFilter = inject(DefaultHrFilterService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly canManageAdmin = this.authService.canManageAdmin;
  protected readonly categoryBackground = categoryBackground;
  protected readonly categoryOptions = CATEGORIES.map((c) => ACTIVITY_CATEGORY[c].label);

  readonly items = signal<PlannedActivity[]>([]);
  readonly editingItem = signal<PlannedActivity | null>(null);
  // A filter with every box unchecked: it matches nothing, which the URL can't express (no param means "All").
  protected readonly hrCleared = signal(false);
  protected readonly categoriesCleared = signal(false);
  private readonly reload = signal(0);

  readonly subtitle = computed(() => {
    const count = this.items().length;
    return count === 1
      ? '1 planned activity across open promotions'
      : `${count} planned activities across open promotions`;
  });

  readonly selectedHrManagers$ = this.route.queryParamMap.pipe(
    map((params) => params.getAll('hrManager').filter((hr) => hr !== ALL_HR_MANAGERS)),
    distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
  );

  readonly selectedCategories$ = this.route.queryParamMap.pipe(
    map((params) => CATEGORIES.filter((c) => params.getAll('category').includes(c))),
    distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
  );

  readonly selectedCategoryLabels$ = this.selectedCategories$.pipe(
    map((categories) => categories.map((c) => ACTIVITY_CATEGORY[c].label)),
  );

  readonly searchTerm$ = this.route.queryParamMap.pipe(
    map((params) => params.get('searchTerm') ?? ''),
    distinctUntilChanged(),
  );

  readonly hrManagerOptions$ = toObservable(this.authService.isProfileLoaded).pipe(
    filter((loaded) => loaded),
    take(1),
    switchMap(() =>
      this.employeesService.getFilterOptions().pipe(
        map((options) => options.hrManagers),
        catchError(() => of([] as string[])),
      ),
    ),
    shareReplay(1),
  );

  private readonly defaultHrManager$ = this.defaultHrFilter.resolve(this.hrManagerOptions$).pipe(shareReplay(1));
  private readonly defaultHrManager = toSignal(this.defaultHrManager$, { initialValue: null });

  constructor() {
    // An HR starts on their own employees, unless the URL already picks an HR manager.
    this.defaultHrManager$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((hrManager) => {
      if (hrManager && !this.route.snapshot.queryParamMap.has('hrManager')) this.updateUrl({ hrManager: [hrManager] });
    });

    // Wait for the default so the list doesn't load unfiltered first, then again once the HR default is applied.
    combineLatest([
      this.selectedHrManagers$,
      this.defaultHrManager$,
      this.selectedCategories$,
      this.searchTerm$,
      toObservable(this.hrCleared),
      toObservable(this.categoriesCleared),
      toObservable(this.reload),
    ])
      .pipe(
        filter(([hrManagers, defaultHr]) => !defaultHr || hrManagers.length > 0 || this.hasHrParam()),
        debounceTime(300),
        // Read the signals: a filter sets `cleared` before its URL change, which can land ahead of toObservable.
        switchMap(([hrManagers, , categories, searchTerm]) =>
          this.hrCleared() || this.categoriesCleared()
            ? of<PlannedActivity[]>([])
            : this.promotionsService
                .getPlannedActivities({ searchTerm: searchTerm.trim(), hrManagers, categories })
                .pipe(
                  catchError((err) => {
                    this.toastService.error(firstApiErrorMessage(err, 'Could not load planned activities.'));
                    return of<PlannedActivity[]>([]);
                  }),
                ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((list) => this.items.set(list));
  }

  // No selection is "All": no param, but an HR's stays "all" in the URL so the default doesn't come back.
  onHrManagersChange(hrManagers: string[]): void {
    const keepAll = this.defaultHrManager() !== null;
    this.updateUrl({ hrManager: hrManagers.length ? hrManagers : keepAll ? ALL_HR_MANAGERS : null });
  }

  onCategoriesChange(labels: string[]): void {
    const categories = CATEGORIES.filter((c) => labels.includes(ACTIVITY_CATEGORY[c].label));
    this.updateUrl({ category: categories.length ? categories : null });
  }

  onSearchChange(searchTerm: string): void {
    this.updateUrl({ searchTerm: searchTerm || null });
  }

  savePlannedDate(item: PlannedActivity, plannedAt: string): void {
    this.editingItem.set(null);
    this.promotionsService
      .updatePlannedDate(item.employeeId, item.employeeActivityId, plannedAt)
      .pipe(
        catchError((err) => {
          this.toastService.error(firstApiErrorMessage(err, 'Could not update the planned date.'));
          return EMPTY;
        }),
      )
      .subscribe(() => {
        this.toastService.success(`Updated the planned date of “${item.name}” for ${item.employeeName}`);
        this.reload.update((n) => n + 1);
      });
  }

  private hasHrParam(): boolean {
    return this.route.snapshot.queryParamMap.has('hrManager');
  }

  private updateUrl(queryParams: Record<string, string | string[] | null>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
