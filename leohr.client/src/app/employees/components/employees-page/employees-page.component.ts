import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, NavigationEnd, NavigationStart, ParamMap, Router } from '@angular/router';
import {
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  shareReplay,
  startWith,
  take,
  tap,
} from 'rxjs';
import { EmployeeFilterModel } from 'src/app/core/models/base-filter.model';
import { InfiniteScrollPaginator, PagedFetchResult } from 'src/app/shared/utils/infinite-scroll-paginator';
import { trackById } from 'src/app/shared/utils/track-by-id';
import { Employee, EmployeeFilterOptions } from '../../models/employee.model';
import { ALL_HR_MANAGERS, DefaultHrFilterService } from '../../services/default-hr-filter.service';
import { EmployeesService } from '../../services/employees.service';

type FilterKey = 'seniority' | 'profile' | 'project' | 'hrManager';

interface QueryParams {
  searchTerm: string;
  seniorities: string[];
  profiles: string[];
  projects: string[];
  hrManagers: string[];
}

function toQueryParams(params: ParamMap): QueryParams {
  return {
    searchTerm: params.get('searchTerm') ?? '',
    seniorities: params.getAll('seniority'),
    profiles: params.getAll('profile'),
    projects: params.getAll('project'),
    hrManagers: params.getAll('hrManager').filter((hr) => hr !== ALL_HR_MANAGERS),
  };
}

const AVATAR_COLORS = ['#A8D5C2', '#F4C4A1', '#B8C9E8', '#F0D4A8', '#C4B8E8', '#E8B8C4', '#B8E8C4', '#D4E8A8'];
const EMPTY_FILTER_OPTIONS: EmployeeFilterOptions = { seniorities: [], profiles: [], projects: [], hrManagers: [] };
const PAGE_SIZE = 100;
const LOAD_MORE_THRESHOLD = 20;

@Component({
  selector: 'app-employees-page',
  templateUrl: './employees-page.component.html',
  styleUrl: './employees-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeesPageComponent {
  private readonly employeesService = inject(EmployeesService);
  private readonly defaultHrFilter = inject(DefaultHrFilterService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);
  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  protected readonly rowHeight = 57;

  readonly loading = signal(true);
  // After the first load, a reload keeps the current list on screen until the new one arrives.
  protected readonly loaded = signal(false);
  protected readonly loadingMore = signal(false);
  private readonly searchError = signal<string | null>(null);
  private readonly filterOptionsFailed = signal(false);
  readonly error = computed(
    () => this.searchError() ?? (this.filterOptionsFailed() ? 'Failed to load filters.' : null),
  );

  readonly queryParams$ = this.route.queryParamMap.pipe(
    map(toQueryParams),
    distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
  );

  readonly search$ = this.queryParams$.pipe(map((p) => p.searchTerm));

  // Fetched once — dropdown options list every value across all active employees, independent of
  // the current filter, so a facet never loses options the user could still switch back to.
  private readonly loadedFilterOptions$ = this.employeesService.getFilterOptions().pipe(
    tap(() => this.filterOptionsFailed.set(false)),
    catchError(() => {
      this.filterOptionsFailed.set(true);
      return of(EMPTY_FILTER_OPTIONS);
    }),
    shareReplay(1),
  );

  readonly filterOptions$ = this.loadedFilterOptions$.pipe(startWith(EMPTY_FILTER_OPTIONS), shareReplay(1));

  private readonly defaultHrManager$ = this.defaultHrFilter
    .resolve(this.loadedFilterOptions$.pipe(map((options) => options.hrManagers)))
    .pipe(shareReplay(1));
  private readonly defaultHrManager = toSignal(this.defaultHrManager$, { initialValue: null });

  // Accumulated pages for the current filter — reset to page 1 whenever the filter/search changes,
  // and appended to as the user scrolls near the end (loadNextPageIfNeeded, via the paginator
  // below). Infinite scroll over paged requests instead of loading every employee at once, since
  // the list can grow large.
  private readonly employees = signal<Employee[]>([]);
  private currentParams: QueryParams = { searchTerm: '', seniorities: [], profiles: [], projects: [], hrManagers: [] };
  private currentCleared: ReadonlySet<FilterKey> = new Set();
  // What the list on screen was loaded for, so the same filters coming back don't reload it.
  private loadedFor: string | null = null;

  private readonly paginator = new InfiniteScrollPaginator<Employee>({
    pageSize: PAGE_SIZE,
    loadMoreThreshold: LOAD_MORE_THRESHOLD,
    fetchPage: (page) =>
      this.currentCleared.size > 0
        ? of<PagedFetchResult<Employee>>({ items: [], totalCount: 0 })
        : this.employeesService.search(this.buildFilter(this.currentParams, page)),
    getViewport: () => this.viewport(),
    onItems: (items) => {
      this.employees.set(items);
      this.loaded.set(true);
    },
    onLoadingChange: (loading) => this.loading.set(loading),
    onLoadingMoreChange: (loadingMore) => this.loadingMore.set(loadingMore),
    onError: (context) => {
      if (context === 'first') {
        this.searchError.set('Failed to load employees.');
        this.loadedFor = null; // a failed list isn't "already shown": the next trigger reloads it
      }
    },
  });

  readonly vm$ = combineLatest({ employees: toObservable(this.employees), filterOptions: this.filterOptions$ });

  constructor() {
    this.applyDefaultsOnEnter(this.route.snapshot.queryParamMap);

    // One click changes the URL and the cleared set separately; the default is in the mix so the list loads once.
    combineLatest([this.queryParams$, toObservable(this.explicitlyClearedFilters), this.defaultHrManager$])
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(([params, cleared]) => {
        // The URL can pass through "no filters" and back to what is shown; reloading would only flash the list.
        const key = this.loadKey(params, cleared);
        if (key === this.loadedFor) return;

        this.loadedFor = key;
        this.currentParams = params;
        this.currentCleared = cleared;
        this.searchError.set(null);
        this.paginator.loadFirstPage();
      });

    this.applyDefaultsOnReturn();
    this.restoreViewportOnReattach();
  }

  private loadKey(params: QueryParams, cleared: ReadonlySet<FilterKey>): string {
    return JSON.stringify([params, [...cleared]]);
  }

  // `current` is the URL's filters on entry; an HR gets their own HR manager unless the URL already picks one.
  private applyDefaultsOnEnter(current: ParamMap): void {
    this.defaultHrManager$.pipe(take(1)).subscribe((hrManager) => {
      const defaultHr = hrManager && !current.has('hrManager') ? hrManager : null;
      const next = { ...toQueryParams(current), ...(defaultHr ? { hrManagers: [defaultHr] } : {}) };

      // Mark the list about to be replaced as loading now, not after the debounce.
      if (this.loadKey(next, this.explicitlyClearedFilters()) !== this.loadedFor) this.loading.set(true);
      if (defaultHr) this.updateUrl({ hrManager: [defaultHr] });
    });
  }

  // The page is kept alive (AppReuseStrategy), so entering it again needs this; the constructor runs once.
  // A link or the menu starts from the defaults; back/forward brings the URL as left, with "all" kept in it.
  private applyDefaultsOnReturn(): void {
    let wasOnList = true;
    this.router.events.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (!(event instanceof NavigationEnd)) return;

      const url = this.router.parseUrl(event.urlAfterRedirects);
      const segments = url.root.children['primary']?.segments ?? [];
      const onList = segments.length === 1 && segments[0].path === 'employees';
      if (onList && !wasOnList) this.applyDefaultsOnEnter(convertToParamMap(url.queryParams));
      wasOnList = onList;
    });
  }

  private restoreViewportOnReattach(): void {
    let savedOffset = 0;
    let restorePending = false;
    let isHistoryNavigation = false;
    this.router.events.pipe(takeUntilDestroyed()).subscribe((event) => {
      const viewport = this.viewport();
      if (!viewport) return;
      const attached = viewport.elementRef.nativeElement.isConnected;
      if (event instanceof NavigationStart) {
        isHistoryNavigation = event.navigationTrigger === 'popstate';
        if (attached) savedOffset = viewport.measureScrollOffset();
      } else if (event instanceof NavigationEnd && !attached && !restorePending) {
        restorePending = true;
        // Bound to this view, so it runs only after the outlet has reattached and rendered the page.
        afterNextRender(
          () => {
            restorePending = false;
            const current = this.viewport();
            if (!current) return;
            current.checkViewportSize();
            current.scrollToOffset(isHistoryNavigation ? savedOffset : 0);
            current.checkViewportSize();
          },
          { injector: this.injector },
        );
      }
    });
  }

  private buildFilter(params: QueryParams, page: number): EmployeeFilterModel {
    return {
      searchTerm: params.searchTerm || null,
      seniorities: params.seniorities,
      profiles: params.profiles,
      projects: params.projects,
      hrManagers: params.hrManagers,
      pageNumber: page,
      pageSize: PAGE_SIZE,
    };
  }

  loadNextPageIfNeeded(): void {
    this.paginator.loadNextPageIfNeeded();
  }

  // An empty selection collapses to the same URL/query-param shape as the param being absent — the
  // backend has no way to tell "filter to nothing" apart from "no filter applied" from an empty array
  // alone. `explicitlyClearedFilters` is local UI state that both renders the checkboxes as unchecked
  // (instead of snapping back to "all checked") and — via the paginator's fetchPage above — short-
  // circuits the result list to empty. It never reaches the URL or the backend, and resets on reload.
  private readonly explicitlyClearedFilters = signal<ReadonlySet<FilterKey>>(new Set());

  isCleared(key: FilterKey): boolean {
    return this.explicitlyClearedFilters().has(key);
  }

  setCleared(key: FilterKey, cleared: boolean): void {
    if (this.isCleared(key) === cleared) return;
    const updatedSet = new Set(this.explicitlyClearedFilters());
    if (cleared) updatedSet.add(key);
    else updatedSet.delete(key);
    this.explicitlyClearedFilters.set(updatedSet);
  }

  // An empty selection drops the param, except for an HR's HR managers: there it stays "all", or the default would come back.
  onFilterChange(key: FilterKey, selected: string[]): void {
    const keepAll = key === 'hrManager' && this.defaultHrManager() !== null;
    this.updateUrl({ [key]: selected.length ? selected : keepAll ? ALL_HR_MANAGERS : null });
  }

  onSearchChange(search: string): void {
    this.updateUrl({ searchTerm: search || null });
  }

  private updateUrl(queryParams: Record<string, string | string[] | null>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  protected readonly trackById = trackById<Employee>;

  getInitials(emp: Employee): string {
    return ((emp.firstName?.[0] ?? '') + (emp.lastName?.[0] ?? '')).toUpperCase();
  }

  getAvatarColor(emp: Employee): string {
    const key = (emp.firstName ?? '') + (emp.lastName ?? '');
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = (hash * 31 + key.charCodeAt(i)) & 0xffffff;
    }
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
  }
}
