import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  filter,
  forkJoin,
  map,
  Observable,
  EMPTY,
  of,
  shareReplay,
  Subject,
  switchMap,
  take,
  tap,
} from 'rxjs';
import { PromotionFilterModel } from 'src/app/core/models/base-filter.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { Employee } from 'src/app/employees/models/employee.model';
import { ALL_HR_MANAGERS, DefaultHrFilterService } from 'src/app/employees/services/default-hr-filter.service';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { SearchOption, toEmployeeOptions } from 'src/app/shared/models/search-option.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { InfiniteScrollPaginator, PagedFetchResult } from 'src/app/shared/utils/infinite-scroll-paginator';
import { trackById } from 'src/app/shared/utils/track-by-id';
import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { Promotion } from '../../models/promotion.model';
import { PromotionFilterTab, PromotionTab } from '../../models/promotion-filter-tab.model';
import { PromotionsService, PromotionTargetLevels } from '../../services/promotions.service';

interface QueryParams {
  searchTerm: string;
  activeTab: PromotionTab;
  hrManagers: string[];
}

const AVATAR_COLORS = ['#A8D5C2', '#F4C4A1', '#B8C9E8', '#F0D4A8', '#C4B8E8', '#E8B8C4', '#B8E8C4', '#D4E8A8'];
const PAGE_SIZE = 100;
const LOAD_MORE_THRESHOLD = 20;
const DEFAULT_TAB: PromotionTab = PromotionStatus.InProgress;
// List filter for an employee without an open promotion: all of their own promotions.
const ALL_OWN_PROMOTIONS: QueryParams = { searchTerm: '', activeTab: PromotionFilterTab.All, hrManagers: [] };

@Component({
  selector: 'app-promotions-page',
  templateUrl: './promotions-page.component.html',
  styleUrl: './promotions-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionsPageComponent {
  private readonly employeesService = inject(EmployeesService);
  private readonly defaultHrFilter = inject(DefaultHrFilterService);
  private readonly promotionsService = inject(PromotionsService);
  private readonly router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly toastService = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly canReadManage = this.authService.canReadManage;
  protected readonly canWriteManage = this.authService.canWriteManage;

  constructor() {
    toObservable(this.authService.isProfileLoaded)
      .pipe(
        filter((loaded) => loaded && this.canWriteManage()),
        switchMap(() => this.promotionsService.getDefaultHolder().pipe(catchError(() => of(null)))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((holder) => {
        if (holder?.employeeId) this.defaultHolderId = holder.employeeId;
      });

    // An HR starts on their own employees, unless the URL already picks an HR manager.
    this.defaultHrManager$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((hrManager) => {
      if (hrManager && !this.route.snapshot.queryParamMap.has('hrManager')) this.updateUrl({ hrManager: [hrManager] });
    });

    // Debounce after combining, and wait for the default: the list must not load unfiltered first,
    // then again once the HR default is applied.
    combineLatest([
      this.queryParams$,
      this.reload$,
      this.showAllOwnPromotions$,
      this.defaultHrManager$,
      toObservable(this.hrCleared),
    ])
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(([params, , showAllOwn, , hrCleared]) => {
        this.currentParams = showAllOwn ? ALL_OWN_PROMOTIONS : params;
        this.currentHrCleared = hrCleared;
        this.loadFailed.set(false);
        this.paginator.loadFirstPage();
      });

    this.hasOwnActivePromotion$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((hasActive) => {
      if (hasActive !== null) this.hasActivePromotion.set(hasActive);
    });

    this.targetLevelsRequest$
      .pipe(
        tap(() => this.targetLevels.set(null)),
        switchMap((employeeId) =>
          employeeId
            ? this.promotionsService.getTargetLevels(employeeId).pipe(
                tap({
                  subscribe: () => this.targetLevelsLoading.set(true),
                  finalize: () => this.targetLevelsLoading.set(false),
                }),
                catchError((err: HttpErrorResponse) => {
                  this.toastService.error(firstApiErrorMessage(err, 'Failed to load promotion levels.'));
                  return EMPTY;
                }),
              )
            : EMPTY,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((levels) => this.targetLevels.set(levels));
  }

  // null when the active-promotion check failed, so neither answer is known.
  private readonly hasOwnActivePromotion$: Observable<boolean | null> = toObservable(
    this.authService.isProfileLoaded,
  ).pipe(
    filter((loaded) => loaded && !this.canReadManage()),
    switchMap(() =>
      forkJoin({
        level: this.promotionsService.getMyLevel().pipe(catchError(() => of(null))),
        hasActive: this.promotionsService.getActivePromotion().pipe(
          map(() => true),
          catchError((err: HttpErrorResponse) => of(err.status === 404 ? false : null)),
        ),
      }),
    ),
    tap(({ level }) => {
      if (level) {
        this.userSeniority.set(level.seniority ?? '');
        this.userTechnology.set(level.technology ?? '');
      }
    }),
    map(({ hasActive }) => hasActive),
    shareReplay(1),
  );

  private readonly showAllOwnPromotions$ = toObservable(this.authService.isProfileLoaded).pipe(
    filter((loaded) => loaded),
    switchMap(() =>
      this.canReadManage() ? of(false) : this.hasOwnActivePromotion$.pipe(map((hasActive) => hasActive === false)),
    ),
    distinctUntilChanged(),
  );

  private readonly reload$ = new BehaviorSubject<void>(undefined);
  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  protected readonly rowHeight = 84;

  public readonly queryParams$ = this.route.queryParamMap.pipe(
    map((params) => ({
      searchTerm: params.get('searchTerm') ?? '',
      activeTab: this.toFilterTab(params.get('tab')),
      hrManagers: params.getAll('hrManager').filter((hr) => hr !== ALL_HR_MANAGERS),
    })),
    distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
  );
  readonly search$ = this.queryParams$.pipe(map((p) => p.searchTerm));
  readonly activeFilter$ = this.queryParams$.pipe(map((p) => p.activeTab));
  readonly selectedHrManagers$ = this.queryParams$.pipe(map((p) => p.hrManagers));

  // Only people who see every promotion get the HR filter.
  private readonly hrManagerOptions$ = toObservable(this.authService.isProfileLoaded).pipe(
    filter((loaded) => loaded),
    take(1),
    switchMap(() =>
      this.canReadManage()
        ? this.employeesService.getFilterOptions().pipe(
            map((options) => options.hrManagers),
            catchError(() => of([] as string[])),
          )
        : of([] as string[]),
    ),
    shareReplay(1),
  );

  private readonly defaultHrManager$ = this.defaultHrFilter.resolve(this.hrManagerOptions$).pipe(shareReplay(1));
  private readonly defaultHrManager = toSignal(this.defaultHrManager$, { initialValue: null });

  // Accumulated pages for the current filter — reset to page 1 on every filter/search/reload$
  // change, and appended to as the user scrolls near the end (loadNextPageIfNeeded, via the
  // paginator below). Infinite scroll over paged requests instead of loading every promotion at
  // once, since the list can grow large over time.
  protected readonly promotions = signal<Promotion[]>([]);
  private currentParams: QueryParams = { searchTerm: '', activeTab: DEFAULT_TAB, hrManagers: [] };
  private currentHrCleared = false;

  protected readonly loading = signal(true);
  // After the first load, a reload keeps the current list on screen until the new one arrives.
  protected readonly loaded = signal(false);
  protected readonly loadFailed = signal(false);
  protected readonly loadingMore = signal(false);
  // The HR filter with every box unchecked: it matches nothing, which the URL can't express (no param means "All").
  protected readonly hrCleared = signal(false);

  private readonly paginator = new InfiniteScrollPaginator<Promotion>({
    pageSize: PAGE_SIZE,
    loadMoreThreshold: LOAD_MORE_THRESHOLD,
    fetchPage: (page) =>
      this.currentHrCleared
        ? of<PagedFetchResult<Promotion>>({ items: [], totalCount: 0 })
        : this.promotionsService.getAll(this.buildFilter(this.currentParams, page)),
    getViewport: () => this.viewport(),
    onItems: (items) => {
      this.promotions.set(items);
      this.loaded.set(true);
    },
    onLoadingChange: (loading) => this.loading.set(loading),
    onLoadingMoreChange: (loadingMore) => this.loadingMore.set(loadingMore),
    onError: (context) => {
      if (context === 'first') this.loadFailed.set(true);
    },
  });

  private buildFilter(params: QueryParams, page: number): PromotionFilterModel {
    return {
      searchTerm: params.searchTerm || null,
      status: this.mapTabToEnum(params.activeTab),
      showOnlyPendingActivities: params.activeTab === PromotionFilterTab.PendingApprovals || null,
      hrManagers: params.hrManagers.length ? params.hrManagers : null,
      pageNumber: page,
      pageSize: PAGE_SIZE,
    };
  }

  loadNextPageIfNeeded(): void {
    this.paginator.loadNextPageIfNeeded();
  }

  private readonly employees$ = toObservable(this.authService.isProfileLoaded).pipe(
    filter((loaded) => loaded),
    switchMap(() => {
      if (!this.canWriteManage()) {
        return of([]);
      }

      return this.employeesService.getActiveEmployees().pipe(catchError(() => of([])));
    }),
    shareReplay(1),
  );

  readonly vm$ = combineLatest({
    employees: this.employees$,
    hrManagers: this.hrManagerOptions$,
  });

  readonly filters: { key: PromotionTab; label: string }[] = [
    { key: PromotionStatus.InProgress, label: 'In progress' },
    { key: PromotionFilterTab.PendingApprovals, label: 'Pending approvals' },
    { key: PromotionStatus.Ready, label: 'Ready to send' },
    { key: PromotionStatus.Sent, label: 'Sent' },
    { key: PromotionFilterTab.All, label: 'All' },
  ];

  readonly roadmapModalOpen = signal(false);
  // null = not resolved yet, so the toolbar and empty-state banner stay hidden until we know.
  readonly hasActivePromotion = signal<boolean | null>(null);
  readonly showNoPromotionBanner = computed(() => !this.canReadManage() && this.hasActivePromotion() === false);
  readonly userTechnology = signal<string>('');
  readonly userSeniority = signal<string>('');

  openRoadmapModal(): void {
    this.roadmapModalOpen.set(true);
  }

  modalOpen = false;
  selectedEmployeeId = '';
  selectedEmployee: Employee | null = null;
  private readonly targetLevelsRequest$ = new Subject<string | null>();
  readonly targetLevels = signal<PromotionTargetLevels | null>(null);
  readonly targetLevelsLoading = signal(false);

  private defaultHolderId = '';
  selectedHolderId = this.defaultHolderId;
  selectedHolder: Employee | null = null;

  overrideToRole = false;
  customToRole = '';

  readonly levelOptions = computed<SearchOption[]>(() =>
    (this.targetLevels()?.levels ?? []).map((level) => ({ id: level, label: level })),
  );

  private employeeOptionsCache: { employees: Employee[]; options: SearchOption[] } | null = null;
  private holderOptionsCache: { employees: Employee[]; excludeId: string; options: SearchOption[] } | null = null;

  employeeOptions(employees: Employee[]): SearchOption[] {
    const cache = this.employeeOptionsCache;
    if (cache && cache.employees === employees) return cache.options;
    const options = toEmployeeOptions(employees);
    this.employeeOptionsCache = { employees, options };
    return options;
  }

  holderOptions(employees: Employee[]): SearchOption[] {
    const cache = this.holderOptionsCache;
    if (cache && cache.employees === employees && cache.excludeId === this.selectedEmployeeId) return cache.options;
    const options = toEmployeeOptions(employees.filter((e) => e.id !== this.selectedEmployeeId));
    this.holderOptionsCache = { employees, excludeId: this.selectedEmployeeId, options };
    return options;
  }

  get selectedEmployeeName(): string | null {
    return this.selectedEmployee ? `${this.selectedEmployee.firstName} ${this.selectedEmployee.lastName}` : null;
  }

  get selectedHolderName(): string | null {
    return this.selectedHolder ? `${this.selectedHolder.firstName} ${this.selectedHolder.lastName}` : null;
  }

  pickHolder(option: SearchOption | null, employees: Employee[]): void {
    const holder = employees.find((e) => e.id === option?.id);
    if (!holder) return;
    this.selectedHolder = holder;
    this.selectedHolderId = holder.id;
  }

  pickEmployee(option: SearchOption | null, employees: Employee[]): void {
    const emp = employees.find((e) => e.id === option?.id);
    if (!emp) return;
    this.selectedEmployee = emp;
    this.selectedEmployeeId = emp.id;
    if (this.selectedHolderId === emp.id) {
      this.selectedHolder = null;
      this.selectedHolderId = '';
    }
    this.loadTargetLevels();
  }

  private loadTargetLevels(): void {
    this.overrideToRole = false;
    this.customToRole = '';
    if (this.selectedEmployee) {
      this.targetLevelsRequest$.next(this.selectedEmployee.id);
    }
  }

  onSearchChange(search: string): void {
    this.updateUrl({ searchTerm: search || null });
  }

  // No selection is "All": no param, but an HR's stays "all" in the URL so the default doesn't come back.
  onHrManagersChange(hrManagers: string[]): void {
    const keepAll = this.defaultHrManager() !== null;
    this.updateUrl({ hrManager: hrManagers.length ? hrManagers : keepAll ? ALL_HR_MANAGERS : null });
  }

  setFilter(tabKey: PromotionTab): void {
    this.updateUrl({ tab: tabKey === DEFAULT_TAB ? null : tabKey });
  }

  private updateUrl(queryParams: Record<string, string | string[] | null>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  get autoToRole(): string | null {
    return this.targetLevels()?.levels[0] ?? null;
  }

  get effectiveToRole(): string | null {
    if (this.overrideToRole) return this.customToRole || null;
    return this.autoToRole;
  }

  openModal(employees: Employee[] = []): void {
    this.selectedHolder = employees.find((e) => e.id === this.defaultHolderId) ?? null;
    this.selectedHolderId = this.selectedHolder?.id ?? this.defaultHolderId;
    this.modalOpen = true;
  }

  closeModal(): void {
    this.modalOpen = false;
    this.selectedEmployeeId = '';
    this.selectedEmployee = null;
    this.selectedHolderId = this.defaultHolderId;
    this.selectedHolder = null;
    this.overrideToRole = false;
    this.customToRole = '';
    this.targetLevelsRequest$.next(null);
  }

  confirmAdd(): void {
    const emp = this.selectedEmployee;
    const fromRole = this.targetLevels()?.fromRole;
    const technology = this.targetLevels()?.technology;
    const toRole = this.effectiveToRole;
    if (!emp || !fromRole || !technology || !toRole) return;
    this.promotionsService
      .create({
        employeeId: emp.id,
        fromRole,
        toRole,
        technology,
        promotionHolderId: this.selectedHolderId,
      })
      .pipe(
        tap((created) => {
          this.closeModal();
          this.toastService.success('Promotion added successfully.');
          this.openDetail(created);
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to add promotion.'));
          return of(null);
        }),
        take(1),
      )
      .subscribe();
  }

  openDetail(p: Promotion): void {
    this.router.navigate(['/promotions', p.id]);
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  protected readonly trackById = trackById<Promotion>;

  getInitials(p: Promotion): string {
    return ((p.firstName[0] ?? '') + (p.lastName[0] ?? '')).toUpperCase();
  }

  getAvatarColor(p: Promotion): string {
    const key = p.firstName + p.lastName;
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = (hash * 31 + key.charCodeAt(i)) & 0xffffff;
    }
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
  }

  // An unknown tab in the URL falls back to the default rather than filtering on a status the API would reject.
  private toFilterTab(raw: string | null): PromotionTab {
    return this.filters.some((f) => f.key === raw) ? (raw as PromotionTab) : DEFAULT_TAB;
  }

  private mapTabToEnum(tab: PromotionTab): PromotionStatus | null {
    if (tab === PromotionFilterTab.All || tab === PromotionFilterTab.PendingApprovals) return null;
    return tab;
  }
}
