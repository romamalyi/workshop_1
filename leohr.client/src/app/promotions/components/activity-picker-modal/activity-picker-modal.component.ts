import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { BehaviorSubject, combineLatest, map, Observable, shareReplay } from 'rxjs';
import {
  Activity,
  ActivityCategory,
  ACTIVITY_USAGE,
  activityUsage,
  CategorizedActivity,
  GroupedPromotionActivity,
} from '../../../activities/activity.model';
import { ActivitiesService } from '../../../activities/services/activities.service';
import { PromotionDetail } from '../../services/promotions.service';
import { EMPLOYEE_ACTIVITY_STATUS, EmployeeActivityStatus } from 'src/app/shared/models/employee-activity-status.model';

@Component({
  selector: 'app-activity-picker-modal',
  templateUrl: './activity-picker-modal.component.html',
  styleUrls: ['./activity-picker-modal.component.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityPickerModalComponent {
  private readonly activitiesService = inject(ActivitiesService);

  readonly promotion = input.required<PromotionDetail>();

  readonly selected = output<Activity[]>();
  readonly cancelled = output<void>();

  private readonly catalog$: Observable<CategorizedActivity[]> = this.activitiesService
    .getGrouped()
    .pipe(shareReplay(1));
  private readonly activeCategory$ = new BehaviorSubject<ActivityCategory | null>(null);
  private readonly search$ = new BehaviorSubject<string>('');

  readonly basket = signal<GroupedPromotionActivity[]>([]);
  readonly basketIds = computed(() => new Set(this.basket().map((a) => a.id)));

  readonly categories$: Observable<ActivityCategory[]> = this.catalog$.pipe(
    map((activities) => [...new Set(activities.map((a) => a.category))]),
  );

  public isTitleSpaceNeeded = computed(() => this.lockedSections().some((s) => s.activities.length > 0));
  public isNoActivities = computed(
    () => this.lockedSections().every((s) => s.activities.length === 0) && !this.basket().length,
  );

  protected readonly lockedSections = computed(() => [
    { status: EmployeeActivityStatus.Approved, activities: this.promotion().activities },
    { status: EmployeeActivityStatus.Planned, activities: this.promotion().plannedActivities },
    { status: EmployeeActivityStatus.SelfReported, activities: this.promotion().selfReportedActivities },
  ]);

  lockedSectionTitle(status: EmployeeActivityStatus): string {
    return `${EMPLOYEE_ACTIVITY_STATUS[status].label} · locked`;
  }

  readonly projected = computed(() => {
    const achieved = this.promotion().pointsAchieved ?? 0;
    const basketPts = this.basket().reduce((s, a) => s + a.points, 0);
    const plannedPts = this.promotion().pointsPlanned ?? 0;
    return achieved + plannedPts + basketPts;
  });

  readonly overplannedBy = computed(() => {
    const required = this.promotion().pointsRequired ?? 0;
    if (!required) return 0;
    const over = this.projected() - required;
    return over > 0 ? over : 0;
  });

  readonly filtered$: Observable<CategorizedActivity[]> = combineLatest([
    this.catalog$,
    this.activeCategory$,
    this.search$,
  ]).pipe(
    map(([categories, category, search]) => {
      const promotion = this.promotion();
      const isSelectable = (a: Activity): boolean =>
        !(a.technology != null && a.points === 0 && a.isPromotionRequirement) &&
        (a.technology == null || a.technology === promotion.technology);
      let result = (category ? categories.filter((c) => c.category === category) : categories)
        .map((g) => ({ ...g, activities: g.activities.filter(isSelectable) }))
        .filter((g) => g.activities.length > 0);
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        result = result
          .map((r) => ({ ...r, activities: r.activities.filter((a) => a.name.toLowerCase().includes(q)) }))
          .filter((g) => g.activities.length > 0);
      }
      return result.map((g) => ({
        ...g,
        activities: g.activities.map((a) => ({
          ...a,
          usedIn: activityUsage(a.id, promotion),
        })),
      }));
    }),
  );

  readonly activeCategory = this.activeCategory$;
  readonly search = this.search$;

  usageTooltip(activity: GroupedPromotionActivity): string {
    return activity.usedIn ? ACTIVITY_USAGE[activity.usedIn].shortLabel : '';
  }

  selectCategory(category: ActivityCategory | null): void {
    this.activeCategory$.next(category);
  }

  onSearch(value: string): void {
    this.search$.next(value);
  }

  toggleInBasket(activity: GroupedPromotionActivity): void {
    if (activity.usedIn) return;
    if (this.basketIds().has(activity.id)) {
      this.removeFromBasket(activity);
    } else {
      this.basket.update((b) => [...b, activity]);
    }
  }

  removeFromBasket(activity: Activity): void {
    this.basket.update((b) => b.filter((a) => a.id !== activity.id));
  }

  confirm(): void {
    this.selected.emit(this.basket());
  }

  close(): void {
    this.cancelled.emit();
  }
}
