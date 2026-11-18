import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, catchError, combineLatest, map, of, startWith, switchMap, tap } from 'rxjs';
import { Employee, EmployeeHistory } from '../../models/employee.model';
import { EmployeesService } from '../../services/employees.service';
import { ALL_HR_MANAGERS } from '../../services/default-hr-filter.service';
import { PromotionFilterTab } from 'src/app/promotions/models/promotion-filter-tab.model';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { HttpErrorResponse } from '@angular/common/http';

export type DetailTab = 'history' | 'details' | 'activities';

const MS_PER_YEAR = 1000 * 60 * 60 * 24 * 365.25;

interface HistoryGroup {
  year: number;
  items: EmployeeHistory[];
}

interface HistorySummary {
  years: number;
  initialGrade: string | null;
  currentGrade: string | null;
}

interface EmployeeDetailState {
  loading: boolean;
  employee: Employee | null;
  history: EmployeeHistory[];
  historyLoading: boolean;
  historyGroups: HistoryGroup[];
  historySummary: HistorySummary | null;
  error: string | null;
}

function buildHistoryGroups(history: EmployeeHistory[]): HistoryGroup[] {
  const map = new Map<number, EmployeeHistory[]>();
  for (const item of history) {
    const year = new Date(item.date).getFullYear();
    if (!map.has(year)) map.set(year, []);
    map.get(year)!.push(item);
  }
  return [...map.entries()].sort(([a], [b]) => b - a).map(([year, items]) => ({ year, items }));
}

function buildHistorySummary(history: EmployeeHistory[]): HistorySummary | null {
  if (!history.length) return null;

  // Sum up all work periods: joined/rejoined → left (or today)
  const asc = [...history].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
  let totalMs = 0;
  let periodStart: Date | null = null;
  for (const h of asc) {
    if (h.category === 'joined' || h.category === 'rejoined') {
      periodStart = new Date(h.date);
    } else if (h.category === 'left' && periodStart) {
      totalMs += new Date(h.date).getTime() - periodStart.getTime();
      periodStart = null;
    }
  }
  if (periodStart) totalMs += Date.now() - periodStart.getTime();
  const years = Math.floor(totalMs / MS_PER_YEAR);

  const gradeSetEvent = history.find((h) => h.category === 'grade');
  const initialGrade = gradeSetEvent ? gradeSetEvent.description.replace(/^Grade:\s*/i, '').trim() : null;

  const latestChangeEvent = history.find((h) => h.category === 'promotion' || h.category === 'grade-change');
  const currentGrade = latestChangeEvent
    ? (latestChangeEvent.description.match(/→\s*(.+)$/)?.[1]?.trim() ?? initialGrade)
    : initialGrade;

  return { years, initialGrade, currentGrade };
}

const AVATAR_COLORS = ['#A8D5C2', '#F4C4A1', '#B8C9E8', '#F0D4A8', '#C4B8E8', '#E8B8C4', '#B8E8C4', '#D4E8A8'];

@Component({
  selector: 'app-employee-detail',
  templateUrl: './employee-detail.component.html',
  styleUrl: './employee-detail.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly employeesService = inject(EmployeesService);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly authService = inject(AuthService);

  state$!: Observable<EmployeeDetailState>;
  activeTab: DetailTab = 'details';
  protected readonly canReadManage = this.authService.canReadManage;
  protected readonly canWriteManage = this.authService.canWriteManage;
  readonly currentPdpUrl = signal<string | null>(null);

  ngOnInit(): void {
    this.state$ = combineLatest([this.route.paramMap, this.authService.userProfile$]).pipe(
      switchMap(([params, profile]) => {
        const id = params.get('id');
        const userId = profile?.sub;
        if (!id)
          return of({
            loading: false,
            employee: null,
            history: [],
            historyLoading: false,
            historyGroups: [],
            historySummary: null,
            error: 'Invalid ID.',
          });
        // Both requests start on open; the profile renders with the employee and history fills in when it lands.
        return combineLatest([
          this.employeesService.getEmployeeById(id).pipe(
            catchError(() => of(null)),
            tap((employee) => this.currentPdpUrl.set(employee?.pdpUrl ?? null)),
          ),
          this.employeesService.getEmployeeHistory(id, userId).pipe(
            catchError(() => of([] as EmployeeHistory[])),
            startWith(null),
          ),
        ]).pipe(
          map(([employee, history]) => {
            return {
              loading: false,
              employee,
              history: history ?? [],
              historyLoading: history === null,
              historyGroups: buildHistoryGroups(history ?? []),
              historySummary: buildHistorySummary(history ?? []),
              error: employee ? null : 'Employee not found.',
            };
          }),
          startWith({
            loading: true,
            employee: null,
            history: [],
            historyLoading: true,
            historyGroups: [],
            historySummary: null,
            error: null,
          }),
        );
      }),
    );
  }
  public goToPromotionText = computed(() =>
    this.authService.canReadManage() ? 'View promotions' : 'View my promotion',
  );

  goToPromotions(employee: Employee | null): void {
    if (!employee) return;
    if (!this.authService.canReadManage()) {
      //Guard will redirect to it's own promotion or will show an empty list it there is no active promotion.
      this.router.navigate(['/promotions']);
      return;
    }
    const fullName = `${employee.firstName} ${employee.lastName}`.trim();
    this.router.navigate(['/promotions'], {
      queryParams: { searchTerm: fullName, tab: PromotionFilterTab.All, hrManager: ALL_HR_MANAGERS },
    });
  }

  setTab(tab: DetailTab): void {
    this.activeTab = tab;
  }

  goBack(): void {
    this.location.back();
  }

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

  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(ToastService);

  readonly editingPdp = signal<boolean>(false);
  readonly pdpInput = signal<string>('');

  public startEditPdp(currentUrl: string | null): void {
    this.pdpInput.set(currentUrl ?? '');
    this.editingPdp.set(true);
  }

  public cancelEditPdp(): void {
    this.editingPdp.set(false);
  }

  public onPdpInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.pdpInput.set(input.value);
  }

  public savePdp(emp: Employee): void {
    const url = this.pdpInput().trim();
    const payloadUrl = url.length > 0 ? url : null;

    this.employeesService
      .updatePdpUrl(emp.id, payloadUrl)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.currentPdpUrl.set(payloadUrl);
          this.editingPdp.set(false);
          this.toastService.success('PDP link updated successfully.');
        },
        error: (err: HttpErrorResponse) => {
          let errorMessage = 'Failed to update PDP link.';

          if (err.status === 400 && err.error?.errors) {
            const validationErrors = err.error.errors;
            const firstKey = Object.keys(validationErrors)[0];
            if (firstKey && validationErrors[firstKey]?.length) {
              errorMessage = validationErrors[firstKey][0];
            }
          } else if (err.error?.detail) {
            errorMessage = err.error.detail;
          } else if (typeof err.error === 'string') {
            errorMessage = err.error;
          }

          this.toastService.error(errorMessage);
        },
      });
  }
}
