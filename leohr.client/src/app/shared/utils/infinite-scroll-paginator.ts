import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { catchError, Observable, of } from 'rxjs';

export interface PagedFetchResult<T> {
  items: T[];
  totalCount: number;
}

export interface InfiniteScrollPaginatorOptions<T> {
  pageSize: number;
  loadMoreThreshold?: number;
  fetchPage: (page: number) => Observable<PagedFetchResult<T>>;
  getViewport?: () => CdkVirtualScrollViewport | undefined;
  onItems: (items: T[]) => void;
  onLoadingChange?: (loading: boolean) => void;
  onLoadingMoreChange?: (loadingMore: boolean) => void;
  onError?: (context: 'first' | 'more') => void;
}

// Drives cdk-virtual-scroll-viewport infinite scroll: fetch page 1 on demand (loadFirstPage — call
// whenever the filter/search changes), then fetch subsequent pages as the user scrolls near the
// rendered end (loadNextPageIfNeeded — wire to the viewport's (scrolledIndexChange)). What varies
// per list (the actual HTTP call, where accumulated items go, whether loading/error is surfaced)
// is supplied through the options' delegates instead of being duplicated per component.
export class InfiniteScrollPaginator<T> {
  private items: T[] = [];
  private hasMoreValue = true;
  private loadingFirst = false;
  private loadingMoreValue = false;
  private currentPage = 1;
  // Bumped on every loadFirstPage call so a page request still in flight for a since-replaced
  // filter can recognise it's stale and not clobber state for the new one when it resolves late.
  private requestId = 0;

  constructor(private readonly options: InfiniteScrollPaginatorOptions<T>) {}

  get hasMore(): boolean {
    return this.hasMoreValue;
  }

  loadFirstPage(): void {
    const requestId = ++this.requestId;
    this.currentPage = 1;
    this.loadingFirst = true;
    this.options.onLoadingChange?.(true);

    this.options
      .fetchPage(1)
      .pipe(
        catchError(() => {
          this.options.onError?.('first');
          return of<PagedFetchResult<T>>({ items: [], totalCount: 0 });
        }),
      )
      .subscribe((result) => {
        if (requestId !== this.requestId) return; // superseded by a newer filter change
        this.items = result.items;
        this.hasMoreValue = this.items.length === this.options.pageSize && this.items.length < result.totalCount;
        this.loadingFirst = false;
        this.options.onItems(this.items);
        this.options.onLoadingChange?.(false);
        // A new filter/search result replaces the list wholesale, so the virtualized viewport
        // should land back on the first row instead of staying at whatever scroll offset it had.
        this.options.getViewport?.()?.scrollToIndex(0);
      });
  }

  loadNextPageIfNeeded(): void {
    const viewport = this.options.getViewport?.();
    if (!viewport) return;
    const { end } = viewport.getRenderedRange();
    if (end < viewport.getDataLength() - (this.options.loadMoreThreshold ?? 0)) return;

    this.loadNextPage();
  }

  // For a list outside a virtual-scroll viewport, where the caller decides the end is near.
  loadNextPage(): void {
    if (this.loadingFirst || this.loadingMoreValue || !this.hasMoreValue) return;

    const requestId = this.requestId;
    const nextPage = this.currentPage + 1;
    this.loadingMoreValue = true;
    this.options.onLoadingMoreChange?.(true);

    this.options
      .fetchPage(nextPage)
      .pipe(
        catchError(() => {
          this.options.onError?.('more');
          return of<PagedFetchResult<T>>({ items: [], totalCount: this.items.length });
        }),
      )
      .subscribe((result) => {
        this.loadingMoreValue = false;
        this.options.onLoadingMoreChange?.(false);
        if (requestId !== this.requestId) return; // the filter changed while this page was in flight
        this.currentPage = nextPage;
        this.items = [...this.items, ...result.items];
        this.hasMoreValue = result.items.length === this.options.pageSize && this.items.length < result.totalCount;
        this.options.onItems(this.items);
      });
  }
}
