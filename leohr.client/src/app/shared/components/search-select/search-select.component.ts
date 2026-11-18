import { ConnectedPosition } from '@angular/cdk/overlay';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output, signal } from '@angular/core';
import { SearchOption } from '../../models/search-option.model';

let nextListId = 0;

@Component({
  selector: 'app-search-select',
  templateUrl: './search-select.component.html',
  styleUrl: './search-select.component.css',
  standalone: false,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchSelectComponent {
  readonly listId = `search-list-${nextListId++}`;

  readonly label = input.required<string>();
  readonly options = input<SearchOption[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly selectedLabel = input<string | null>(null);
  readonly inputId = input<string | null>(null);
  readonly emptyText = input('Nothing selected');
  readonly readOnly = input(false);
  readonly clearable = input(true);
  readonly compact = input(false);
  readonly bare = input(false);
  readonly searchByHint = input(false);
  readonly placeholder = input('Search…');
  readonly disabled = input(false);

  readonly picked = output<SearchOption | null>();
  readonly blurred = output<void>();

  // The list opens below the input, or above it when there is more room there.
  readonly listPositions: ConnectedPosition[] = [
    { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 4 },
    { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -4 },
  ];
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  readonly listWidth = signal(0);

  readonly open = signal(false);
  readonly query = signal('');
  readonly activeIndex = signal(0);

  readonly shownLabel = computed(() => this.selectedLabel() ?? (this.selectedId() ? 'Unknown' : ''));

  readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    const byHint = this.searchByHint();
    return this.options().filter(
      (o) => o.label.toLowerCase().includes(query) || (byHint && (o.hint ?? '').toLowerCase().includes(query)),
    );
  });

  openList(): void {
    const inputWidth = this.host.nativeElement.querySelector('input')?.offsetWidth ?? 0;
    this.listWidth.set(this.bare() ? Math.max(inputWidth, 280) : inputWidth);
    this.query.set('');
    this.activeIndex.set(0);
    this.open.set(true);
  }

  closeList(): void {
    this.open.set(false);
  }

  onBlur(): void {
    this.closeList();
    this.blurred.emit();
  }

  onInput(value: string): void {
    this.query.set(value);
    this.activeIndex.set(0);
    this.open.set(true);
  }

  onKeydown(event: KeyboardEvent): void {
    const count = this.filtered().length;
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.activeIndex.update((i) => Math.min(i + 1, Math.max(count - 1, 0)));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update((i) => Math.max(i - 1, 0));
        break;
      case 'Enter': {
        const option = this.filtered()[this.activeIndex()];
        if (this.open() && option) {
          event.preventDefault();
          this.pick(option);
        }
        break;
      }
      case 'Escape':
        this.closeList();
        break;
    }
  }

  pick(option: SearchOption): void {
    this.picked.emit(option);
    this.closeList();
  }

  clear(): void {
    this.picked.emit(null);
  }
}
