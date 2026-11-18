import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  map,
  Observable,
  of,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  take,
} from 'rxjs';
import {
  CreatePromotionCriteriaRequest,
  ENGLISH_LEVEL_LABELS,
  KE_REQUIREMENT_LABELS,
  PromotionCriteria,
  SENIORITY_LABELS,
  SUB_LEVEL_LABELS,
  SUB_LEVEL_SORT_ORDER,
  TECHNOLOGIES,
  UpdatePromotionCriteriaRequest,
} from '../../promotion-criteria.model';
import { PromotionCriteriaService } from '../../services/promotion-criteria.service';
import { ActivitiesService } from '../../../activities/services/activities.service';
import { Activity } from '../../../activities/activity.model';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';

@Component({
  selector: 'app-promotion-criteria-page',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './promotion-criteria-page.component.html',
})
export class PromotionCriteriaPageComponent implements OnInit {
  showInactive$ = new BehaviorSubject(false);
  activeTab$ = new BehaviorSubject<string>(TECHNOLOGIES[0]);
  private refresh$ = new Subject<void>();
  modalOpen$ = new BehaviorSubject(false);
  editingCriteria: PromotionCriteria | null = null;
  deletingCriteria$ = new BehaviorSubject<PromotionCriteria | null>(null);
  duplicateError$ = new BehaviorSubject(false);

  technologies = TECHNOLOGIES;
  criteria$!: Observable<PromotionCriteria[]>;
  activities$!: Observable<Activity[]>;

  constructor(
    private service: PromotionCriteriaService,
    private activitiesService: ActivitiesService,
    private toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.activities$ = this.activitiesService.getAll().pipe(shareReplay(1));

    this.criteria$ = combineLatest([this.showInactive$, this.activeTab$, this.refresh$.pipe(startWith(null))]).pipe(
      switchMap(([inactive, tech]) => this.service.getAll(tech, inactive)),
      map((list) =>
        [...list].sort((a, b) => {
          const si = ['Trainee', 'Junior', 'Intermediate', 'Senior', 'Lead'];
          const sd = si.indexOf(a.seniority) - si.indexOf(b.seniority);
          if (sd !== 0) return sd;
          return (SUB_LEVEL_SORT_ORDER[a.subLevel] ?? 0) - (SUB_LEVEL_SORT_ORDER[b.subLevel] ?? 0);
        }),
      ),
      shareReplay(1),
    );
  }

  seniorityLabel(v: string): string {
    return SENIORITY_LABELS[v] ?? v;
  }
  subLevelLabel(v: string): string {
    return SUB_LEVEL_LABELS[v] ?? v;
  }
  englishLevelLabel(v: string): string {
    return ENGLISH_LEVEL_LABELS[v] ?? v;
  }
  keRequirementLabel(v: string): string {
    return KE_REQUIREMENT_LABELS[v] ?? v;
  }

  workExperienceLabel(from: number | null, to: number | null): string {
    if (from == null && to == null) return '—';
    const fmt = (m: number) => (m < 12 ? `${m}m` : m % 12 === 0 ? `${m / 12}y` : `${+(m / 12).toFixed(1)}y`);
    if (from == null) return `up to ${fmt(to!)}`;
    if (to == null) return `${fmt(from)}+`;
    return `${fmt(from)}–${fmt(to)}`;
  }

  openCreate(): void {
    this.editingCriteria = null;
    this.duplicateError$.next(false);
    this.modalOpen$.next(true);
  }
  openEdit(c: PromotionCriteria): void {
    this.editingCriteria = c;
    this.duplicateError$.next(false);
    this.modalOpen$.next(true);
  }
  closeModal(): void {
    this.modalOpen$.next(false);
  }

  onSave(event: {
    request: CreatePromotionCriteriaRequest | UpdatePromotionCriteriaRequest;
    mode: 'Soft' | 'Hard';
  }): void {
    const isEdit = this.editingCriteria !== null;
    const obs$ = this.editingCriteria
      ? this.service.update(this.editingCriteria.id, event.request as UpdatePromotionCriteriaRequest, event.mode)
      : this.service.create(event.request as CreatePromotionCriteriaRequest);

    obs$
      .pipe(
        take(1),
        catchError((err) => {
          if (err?.error?.errors?.[0]?.errorCode === 'DUPLICATE_RESOURCE') this.duplicateError$.next(true);
          else this.toast.error(firstApiErrorMessage(err, 'Failed to save criteria.'));
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result) {
          this.closeModal();
          this.refresh$.next();
          this.toast.success(isEdit ? 'Criteria updated successfully.' : 'Criteria created successfully.');
        }
      });
  }

  confirmDelete(c: PromotionCriteria): void {
    this.deletingCriteria$.next(c);
  }

  executeDelete(): void {
    const c = this.deletingCriteria$.value;
    if (!c) return;
    this.service
      .delete(c.id)
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.deletingCriteria$.next(null);
          this.refresh$.next();
          this.toast.success('Criteria deleted successfully.');
        },
        error: (err) => {
          this.deletingCriteria$.next(null);
          this.toast.error(firstApiErrorMessage(err, 'Failed to delete criteria.'));
        },
      });
  }

  reactivate(c: PromotionCriteria): void {
    this.service
      .reactivate(c.id)
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.refresh$.next();
          this.toast.success('Criteria reactivated successfully.');
        },
        error: (err) => this.toast.error(firstApiErrorMessage(err, 'Failed to reactivate criteria.')),
      });
  }
}
