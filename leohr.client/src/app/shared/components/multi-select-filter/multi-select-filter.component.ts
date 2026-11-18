import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  HostListener,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';

const PANEL_WIDTH = 240;

// Checklist dropdown with search. An empty `selected` means "All"; `cleared` is "nothing checked", which matches nothing.
@Component({
  selector: 'app-multi-select-filter',
  templateUrl: './multi-select-filter.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MultiSelectFilterComponent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly label = input.required<string>();
  readonly options = input.required<string[]>();
  readonly selected = input<string[]>([]);
  readonly cleared = model(false);
  readonly searchPlaceholder = input('Search...');
  readonly selectedChange = output<string[]>();

  protected readonly open = signal(false);
  protected readonly search = signal('');
  protected readonly panelStyle = signal<{ top: string; left: string; width: string } | null>(null);

  protected readonly shownOptions = computed(() => {
    const query = this.search().trim().toLowerCase();
    return query ? this.options().filter((option) => option.toLowerCase().includes(query)) : this.options();
  });
  protected readonly isFiltered = computed(
    () => this.selected().length > 0 && this.selected().length !== this.options().length,
  );
  protected readonly isAllSelected = computed(() =>
    this.selected().length > 0 ? this.selected().length === this.options().length : !this.cleared(),
  );

  // Clicks bubble on purpose, so other dropdowns on the page close too; only clicks inside this one are ignored.
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!event.composedPath().includes(this.host.nativeElement)) this.close();
  }

  @HostListener('window:resize')
  @HostListener('window:scroll')
  protected close(): void {
    this.open.set(false);
    this.panelStyle.set(null);
  }

  protected toggleOpen(event: MouseEvent): void {
    if (this.open()) {
      this.close();
      return;
    }

    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const left = Math.max(8, Math.min(rect.right - PANEL_WIDTH, window.innerWidth - PANEL_WIDTH - 8));
    this.panelStyle.set({ top: `${rect.bottom + 4}px`, left: `${left}px`, width: `${PANEL_WIDTH}px` });
    this.search.set('');
    this.open.set(true);
  }

  protected isSelected(option: string): boolean {
    return this.selected().length > 0 ? this.selected().includes(option) : !this.cleared();
  }

  protected toggle(option: string): void {
    const current = this.selected().length > 0 ? this.selected() : this.cleared() ? [] : this.options();
    const next = current.includes(option) ? current.filter((o) => o !== option) : [...current, option];
    this.cleared.set(next.length === 0);
    // Nothing or everything checked is the same "no filter" selection; `cleared` tells them apart.
    this.selectedChange.emit(next.length === 0 || next.length === this.options().length ? [] : next);
  }

  protected toggleAll(): void {
    this.cleared.set(this.isAllSelected());
    this.selectedChange.emit([]);
  }
}
