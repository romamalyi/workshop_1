import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import {
  BehaviorSubject,
  combineLatest,
  map,
  Observable,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  take,
} from 'rxjs';
import {
  Activity,
  ActivityCategory,
  ACTIVITY_CATEGORY,
  CreateActivityRequest,
  UpdateActivityRequest,
} from '../../activity.model';
import { ActivitiesService } from '../../services/activities.service';
import { ToastService } from '../../../shared/services/toast.service';

interface GroupedCategory {
  key: string;
  label: string;
  activities: Activity[];
  collapsed: boolean;
}

const REQUIREMENTS_GROUP = 'promotion-requirements';

@Component({
  selector: 'app-activities-page',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './activities-page.component.html',
})
export class ActivitiesPageComponent implements OnInit {
  showInactive$ = new BehaviorSubject(false);
  categoryFilter$ = new BehaviorSubject<ActivityCategory | null>(null);
  private refresh$ = new Subject<void>();
  modalOpen$ = new BehaviorSubject(false);
  editingActivity: Activity | null = null;
  deletingActivity$ = new BehaviorSubject<Activity | null>(null);

  readonly allCategories = Object.values(ActivityCategory);
  collapsedCategories$ = new BehaviorSubject<Set<string>>(new Set());

  activities$!: Observable<Activity[]>;
  groups$!: Observable<GroupedCategory[]>;

  private service = inject(ActivitiesService);
  private toast = inject(ToastService);

  ngOnInit(): void {
    this.activities$ = combineLatest([
      this.showInactive$,
      this.categoryFilter$,
      this.refresh$.pipe(startWith(null)),
    ]).pipe(
      switchMap(([inactive, category]) => this.service.getAll(category ?? undefined, inactive)),
      shareReplay(1),
    );

    this.groups$ = combineLatest([this.activities$, this.collapsedCategories$]).pipe(
      map(([activities, collapsed]) => {
        const isRequirement = (a: Activity): boolean =>
          a.technology != null && a.points === 0 && a.isPromotionRequirement;

        const groups: GroupedCategory[] = this.allCategories
          .map((key) => ({
            key,
            label: ACTIVITY_CATEGORY[key].label,
            activities: activities.filter((a) => a.category === key && !isRequirement(a)),
            collapsed: collapsed.has(key),
          }))
          .filter((g) => g.activities.length > 0);

        const requirements = activities.filter(isRequirement);
        if (requirements.length > 0) {
          groups.push({
            key: REQUIREMENTS_GROUP,
            label: 'Promotion requirement activities',
            activities: requirements,
            collapsed: collapsed.has(REQUIREMENTS_GROUP),
          });
        }
        return groups;
      }),
    );
  }

  setCategoryFilter(cat: ActivityCategory | null): void {
    this.categoryFilter$.next(cat);
  }

  toggleCollapse(key: string): void {
    const next = new Set(this.collapsedCategories$.value);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.collapsedCategories$.next(next);
  }

  openCreate(): void {
    this.editingActivity = null;
    this.modalOpen$.next(true);
  }
  openEdit(a: Activity): void {
    this.editingActivity = a;
    this.modalOpen$.next(true);
  }
  closeModal(): void {
    this.modalOpen$.next(false);
  }

  onSave(event: { request: CreateActivityRequest | UpdateActivityRequest; mode: 'Soft' | 'Hard' }): void {
    const isEdit = this.editingActivity !== null;
    const obs$ = this.editingActivity
      ? this.service.update(this.editingActivity.id, event.request as UpdateActivityRequest, event.mode)
      : this.service.create(event.request as CreateActivityRequest);

    obs$.pipe(take(1)).subscribe({
      next: () => {
        this.closeModal();
        this.refresh$.next();
        this.toast.success(isEdit ? 'Activity updated successfully.' : 'Activity created successfully.');
      },
      error: (err) => this.toast.error(this.errorMessage(err, 'Failed to save activity.')),
    });
  }

  confirmDelete(a: Activity): void {
    this.deletingActivity$.next(a);
  }

  reactivate(a: Activity): void {
    this.service
      .reactivate(a.id)
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.refresh$.next();
          this.toast.success('Activity reactivated successfully.');
        },
        error: (err) => this.toast.error(this.errorMessage(err, 'Failed to reactivate activity.')),
      });
  }

  executeDelete(): void {
    const activity = this.deletingActivity$.value;
    if (!activity) return;
    this.service
      .delete(activity.id)
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.deletingActivity$.next(null);
          this.refresh$.next();
          this.toast.success('Activity deleted successfully.');
        },
        error: (err) => {
          this.deletingActivity$.next(null);
          this.toast.error(this.errorMessage(err, 'Failed to delete activity.'));
        },
      });
  }

  private errorMessage(err: unknown, fallback: string): string {
    const response = err as { error?: { errors?: { errorMessage?: string }[] } };
    return response?.error?.errors?.[0]?.errorMessage ?? fallback;
  }
}
