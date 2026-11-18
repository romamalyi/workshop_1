import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { Observable, catchError, map, of, startWith } from 'rxjs';
import { Employee } from '../../../employees/models/employee.model';
import { EmployeesService } from '../../../employees/services/employees.service';
import { EmailTemplatesService } from '../../../core/services/email-templates.service';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

const AVATAR_COLORS = [
  '#A8D5C2', '#F4C4A1', '#B8C9E8', '#F0D4A8',
  '#C4B8E8', '#E8B8C4', '#B8E8C4', '#D4E8A8',
];

const MS_PER_DAY = 1000 * 60 * 60 * 24;

export interface BirthdayEntry {
  employee: Employee;
  day: number;
  highlight: boolean;
}

export interface MonthGroup {
  month: number;
  monthName: string;
  isCurrent: boolean;
  entries: BirthdayEntry[];
}

interface PageState {
  loading: boolean;
  groups: MonthGroup[];
  error: string | null;
}

@Component({
  selector: 'app-birthdays-page',
  templateUrl: './birthdays-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BirthdaysPageComponent {
  private readonly employeesService = inject(EmployeesService);
  private readonly emailTemplates = inject(EmailTemplatesService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly currentMonth = new Date().getMonth();

  digestSentAt: Record<number, Date | undefined> = {};

  wishModalEntry: BirthdayEntry | null = null;
  wishModalGroup: MonthGroup | null = null;
  wishSubject = '';
  wishBody = '';
  wishSentFor: Record<string, boolean> = {};

  digestModalMonth: MonthGroup | null = null;
  digestIncluded: Record<string, boolean> = {};
  digestMessage = '';

  readonly state$: Observable<PageState> = this.employeesService.getActiveEmployees().pipe(
    map((employees: Employee[]) => ({
      loading: false,
      groups: this.buildGroups(employees),
      error: null,
    })),
    catchError(err => {
      console.error('Birthdays load error:', err);
      return of({ loading: false, groups: [] as MonthGroup[], error: 'Failed to load employees.' });
    }),
    startWith({ loading: true, groups: [] as MonthGroup[], error: null }),
  );

  private hashCode(str: string): number {
    let h = 5381;
    for (let i = 0; i < str.length; i++) {
      h = ((h << 5) + h + str.charCodeAt(i)) & 0x7fffffff;
    }
    return h;
  }

  private buildGroups(employees: Employee[]): MonthGroup[] {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const todayYear = today.getFullYear();

    const raw: { employee: Employee; month: number; day: number; daysUntilNext: number }[] = [];

    for (const emp of employees) {
      if (!emp.birthday) continue;
      const [, monthStr, dayStr] = emp.birthday.split('-');
      const month = parseInt(monthStr, 10) - 1;
      const day = parseInt(dayStr, 10);
      const bday = new Date(todayYear, month, day);
      let diff = Math.round((bday.getTime() - today.getTime()) / MS_PER_DAY);
      if (diff < 0) {
        const next = new Date(todayYear + 1, month, day);
        diff = Math.round((next.getTime() - today.getTime()) / MS_PER_DAY);
      }
      raw.push({ employee: emp, month, day, daysUntilNext: diff });
    }

    const hasToday = raw.some(e => e.daysUntilNext === 0);
    let highlightedIds: Set<string>;
    if (hasToday) {
      highlightedIds = new Set(raw.filter(e => e.daysUntilNext === 0).map(e => e.employee.id));
    } else {
      const upcoming = [...raw]
        .filter(e => e.daysUntilNext > 0)
        .sort((a, b) => a.daysUntilNext - b.daysUntilNext)
        .slice(0, 3);
      highlightedIds = new Set(upcoming.map(e => e.employee.id));
    }

    const monthMap = new Map<number, BirthdayEntry[]>();
    for (const e of raw) {
      if (!monthMap.has(e.month)) monthMap.set(e.month, []);
      monthMap.get(e.month)!.push({ employee: e.employee, day: e.day, highlight: highlightedIds.has(e.employee.id) });
    }

    const groups: MonthGroup[] = [];
    for (let i = 0; i < 12; i++) {
      const month = (this.currentMonth + i) % 12;
      const entries = monthMap.get(month);
      if (entries && entries.length > 0) {
        groups.push({
          month,
          monthName: MONTH_NAMES[month],
          isCurrent: i === 0,
          entries: [...entries].sort((a, b) => a.day - b.day),
        });
      }
    }

    return groups;
  }

  getInitials(emp: Employee): string {
    return ((emp.firstName?.[0] ?? '') + (emp.lastName?.[0] ?? '')).toUpperCase();
  }

  getAvatarColor(emp: Employee): string {
    const h = this.hashCode((emp.firstName ?? '') + (emp.lastName ?? ''));
    return AVATAR_COLORS[h % AVATAR_COLORS.length];
  }

  flatEntries(groups: MonthGroup[]): { entry: BirthdayEntry; group: MonthGroup }[] {
    const result: { entry: BirthdayEntry; group: MonthGroup }[] = [];
    for (const g of groups) {
      for (const e of g.entries) result.push({ entry: e, group: g });
    }
    return result;
  }

  monthAbbr(group: MonthGroup): string {
    return group.monthName.slice(0, 3);
  }

  daysUntil(group: MonthGroup, entry: BirthdayEntry): number {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const year = today.getFullYear();
    const bday = new Date(year, group.month, entry.day);
    const diff = Math.round((bday.getTime() - today.getTime()) / MS_PER_DAY);
    if (!group.isCurrent && diff < 0) {
      const nextYear = new Date(year + 1, group.month, entry.day);
      return Math.round((nextYear.getTime() - today.getTime()) / MS_PER_DAY);
    }
    return diff;
  }

  daysUntilLabel(group: MonthGroup, entry: BirthdayEntry): string {
    const d = this.daysUntil(group, entry);
    if (d === 0) return 'Today! 🎉';
    if (d === 1) return 'Tomorrow';
    if (d === -1) return 'Yesterday';
    if (d < 0) return `${Math.abs(d)} days ago`;
    return `In ${d} days`;
  }

  sendMonthlyDigest(month: number): void {
    this.digestSentAt = { ...this.digestSentAt, [month]: new Date() };
    this.cdr.markForCheck();
  }

  openWishModal(group: MonthGroup, entry: BirthdayEntry): void {
    const vars = { firstName: entry.employee.firstName ?? '', lastName: entry.employee.lastName ?? '' };
    this.emailTemplates.get('birthday').pipe(catchError(() => of(null))).subscribe(t => {
      this.wishModalEntry = entry;
      this.wishModalGroup = group;
      this.wishSubject = t ? this.emailTemplates.fill(t.subject, vars) : `Happy Birthday, ${entry.employee.firstName}! 🎂`;
      this.wishBody    = t ? this.emailTemplates.fill(t.body, vars)    : `Hi ${entry.employee.firstName},\n\nHappy Birthday!\n\nHR Team`;
      this.cdr.markForCheck();
    });
  }

  closeWishModal(): void {
    this.wishModalEntry = null;
    this.wishModalGroup = null;
    this.cdr.markForCheck();
  }

  sendWishCard(): void {
    if (this.wishModalEntry) {
      this.wishSentFor = { ...this.wishSentFor, [this.entryKey(this.wishModalEntry)]: true };
    }
    this.closeWishModal();
  }

  openDigestModal(group: MonthGroup): void {
    this.digestModalMonth = group;
    this.digestIncluded = {};
    for (const e of group.entries) {
      this.digestIncluded[e.employee.id ?? (e.employee.firstName ?? '') + (e.employee.lastName ?? '')] = true;
    }
    this.digestMessage = '';
    this.cdr.markForCheck();
  }

  closeDigestModal(): void {
    this.digestModalMonth = null;
    this.cdr.markForCheck();
  }

  confirmSendDigest(): void {
    if (!this.digestModalMonth) return;
    this.digestSentAt = { ...this.digestSentAt, [this.digestModalMonth.month]: new Date() };
    this.closeDigestModal();
  }

  entryKey(entry: BirthdayEntry): string {
    return entry.employee.id ?? (entry.employee.firstName ?? '') + (entry.employee.lastName ?? '');
  }

  formatSentAt(date: Date): string {
    return date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
      + ' at ' + date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  }
}
