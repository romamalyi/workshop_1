import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
  model,
  signal,
} from '@angular/core';

@Component({
  selector: 'app-date-picker',
  templateUrl: './date-picker.component.html',
  styleUrls: ['./date-picker.component.css'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DatePickerComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly value = model<string>('');
  readonly placeholder = input('Select date...');
  readonly disableFuture = input(false, { transform: booleanAttribute });
  readonly disablePast = input(false, { transform: booleanAttribute });

  readonly isOpen = signal(false);

  private readonly PANEL_HEIGHT_ESTIMATE = 340;
  readonly openUpward = signal(false);

  private readonly today = new Date();
  private readonly viewYear = signal(this.today.getFullYear());
  private readonly viewMonth = signal(this.today.getMonth());

  readonly MONTH_NAMES = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];
  readonly DAY_NAMES = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

  readonly monthLabel = computed(() => `${this.MONTH_NAMES[this.viewMonth()]} ${this.viewYear()}`);

  readonly displayValue = computed(() => {
    const v = this.value();
    if (!v) return '';
    const parts = v.split('-');
    return `${parts[2]} ${this.MONTH_NAMES[+parts[1] - 1]} ${parts[0]}`;
  });

  readonly weeks = computed<(Date | null)[][]>(() => {
    const year = this.viewYear();
    const month = this.viewMonth();

    const first = new Date(year, month, 1);
    let startDow = first.getDay();
    startDow = startDow === 0 ? 6 : startDow - 1;

    const days: (Date | null)[] = Array(startDow).fill(null);
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    for (let i = 1; i <= daysInMonth; i++) {
      days.push(new Date(year, month, i));
    }
    while (days.length % 7 !== 0) days.push(null);

    const weeks: (Date | null)[][] = [];
    for (let i = 0; i < days.length; i += 7) {
      weeks.push(days.slice(i, i + 7));
    }
    return weeks;
  });

  toggle(): void {
    if (!this.isOpen()) {
      this.syncViewToValue();
      this.updateOpenDirection();
    }
    this.isOpen.update((open) => !open);
  }

  close(): void {
    this.isOpen.set(false);
  }

  prevMonth(): void {
    if (this.viewMonth() === 0) {
      this.viewMonth.set(11);
      this.viewYear.update((y) => y - 1);
    } else {
      this.viewMonth.update((m) => m - 1);
    }
  }

  nextMonth(): void {
    if (this.viewMonth() === 11) {
      this.viewMonth.set(0);
      this.viewYear.update((y) => y + 1);
    } else {
      this.viewMonth.update((m) => m + 1);
    }
  }

  select(day: Date | null): void {
    if (!day) return;
    this.value.set(this.toIso(day));
    this.isOpen.set(false);
  }

  clear(): void {
    this.value.set('');
    this.isOpen.set(false);
  }

  isSelected(day: Date | null): boolean {
    if (!day || !this.value()) return false;
    return this.toIso(day) === this.value();
  }

  isDisabled(day: Date | null): boolean {
    if (!day) return true;
    const iso = this.toIso(day);
    const todayIso = this.toIso(this.today);
    return (this.disableFuture() && iso > todayIso) || (this.disablePast() && iso < todayIso);
  }

  isToday(day: Date | null): boolean {
    if (!day) return false;
    return this.toIso(day) === this.toIso(this.today);
  }

  private updateOpenDirection(): void {
    const rect = this.host.nativeElement.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    const spaceAbove = rect.top;
    const fitsBelow = spaceBelow >= this.PANEL_HEIGHT_ESTIMATE;
    this.openUpward.set(!fitsBelow && spaceAbove > spaceBelow);
  }

  private syncViewToValue(): void {
    const v = this.value();
    if (v) {
      const [y, m] = v.split('-').map(Number);
      this.viewYear.set(y);
      this.viewMonth.set(m - 1);
    } else {
      this.viewYear.set(this.today.getFullYear());
      this.viewMonth.set(this.today.getMonth());
    }
  }

  private toIso(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
