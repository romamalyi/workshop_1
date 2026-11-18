import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import {
  catchError,
  combineLatest,
  distinctUntilChanged,
  EMPTY,
  filter,
  map,
  of,
  shareReplay,
  switchMap,
  take,
} from 'rxjs';
import { categoryBackground } from 'src/app/activities/activity.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { ALL_HR_MANAGERS, DefaultHrFilterService } from 'src/app/employees/services/default-hr-filter.service';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PendingApprovalsStore } from '../../services/pending-approvals.store';
import { PendingApproval, PromotionsService } from '../../services/promotions.service';

@Component({
  selector: 'app-pending-approvals',
  templateUrl: './pending-approvals.component.html',
  styleUrl: './pending-approvals.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PendingApprovalsComponent {
  private readonly store = inject(PendingApprovalsStore);
  private readonly promotionsService = inject(PromotionsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly defaultHrFilter = inject(DefaultHrFilterService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categoryBackground = categoryBackground;
  protected readonly tableCols = '1.6fr 2.2fr 1.2fr 1.2fr 1.2fr 0.8fr';
  protected readonly tableColsWithActions = `${this.tableCols} 120px`;
  protected readonly canWriteManage = this.authService.canWriteManage;

  readonly items = signal<PendingApproval[]>([]);
  readonly approvingItem = signal<PendingApproval | null>(null);
  // The HR filter with every box unchecked: it matches nothing, which the URL can't express (no param means "All").
  protected readonly hrCleared = signal(false);

  readonly subtitle = computed(() => {
    const count = this.items().length;
    return count === 1
      ? '1 activity reported by employees. Approve to count the points.'
      : `${count} activities reported by employees. Approve to count the points.`;
  });

  readonly selectedHrManagers$ = this.route.queryParamMap.pipe(
    map((params) => params.getAll('hrManager').filter((hr) => hr !== ALL_HR_MANAGERS)),
    distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
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
    combineLatest([this.selectedHrManagers$, this.defaultHrManager$, toObservable(this.hrCleared)])
      .pipe(
        filter(([hrManagers, defaultHr]) => !defaultHr || hrManagers.length > 0 || this.hasHrParam()),
        // Read the signal: the filter sets `cleared` before its URL change, which can land ahead of toObservable.
        switchMap(([hrManagers]) =>
          this.hrCleared()
            ? of<PendingApproval[]>([])
            : this.promotionsService.getPendingApprovals(hrManagers.length ? hrManagers : null).pipe(
                catchError((err) => {
                  this.toastService.error(firstApiErrorMessage(err, 'Could not load pending approvals.'));
                  return of<PendingApproval[]>([]);
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

  approve(item: PendingApproval, accomplishedOn: string): void {
    this.approvingItem.set(null);
    const approvedById = this.authService.currentUserId ?? undefined;
    const request$ =
      item.promotionId !== null
        ? this.promotionsService.approveActivity(
            item.promotionId,
            item.employeeActivityId,
            accomplishedOn,
            approvedById,
          )
        : this.promotionsService.approveEmployeeActivity(
            item.employeeId,
            item.employeeActivityId,
            accomplishedOn,
            approvedById,
          );
    request$
      .pipe(
        catchError((err) => {
          this.toastService.error(firstApiErrorMessage(err, 'Could not approve the activity.'));
          return EMPTY;
        }),
      )
      .subscribe(() => {
        this.toastService.success(`Approved “${item.name}” for ${item.employeeName}`);
        this.drop(item.employeeActivityId);
      });
  }

  // The store holds every pending approval for the rail badge, so it drops the row too.
  private drop(employeeActivityId: number): void {
    this.items.update((list) => list.filter((i) => i.employeeActivityId !== employeeActivityId));
    this.store.drop(employeeActivityId);
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
