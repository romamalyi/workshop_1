import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { httpResource } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { catchError, of } from 'rxjs';

import {
  ActivityPeriod,
  ActivityUsage,
  categoryBackground,
  GroupedPromotionActivity,
} from '../../../../activities/activity.model';
import { ActivitiesService } from '../../../../activities/services/activities.service';
import { PromotionDetail } from 'src/app/promotions/services/promotions.service';
import { EMPLOYEE_ACTIVITY_STATUS, EmployeeActivityStatus } from 'src/app/shared/models/employee-activity-status.model';
import {
  CreateEmployeeActivityRequest,
  EmployeeActivity,
  SelfReportRequest,
  UpdateEmployeeActivityRequest,
} from '../../../models/employee-activity.model';
import { EmployeeActivitiesService } from '../../../services/employee-activities.service';
import {
  ActivityModalMode,
  EmployeeActivityModalComponent,
} from '../employee-activity-modal/employee-activity-modal.component';
import { ApproveActivityDialogComponent } from '../../../../shared/components/approve-activity-dialog/approve-activity-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { AuthService } from 'src/app/core/services/auth.service';
import { DialogDirective } from 'src/app/shared/directives/dialog.directive';
import { TooltipDirective } from 'src/app/shared/directives/tooltip.directive';
import { ActionIconComponent } from 'src/app/shared/components/action-icon/action-icon.component';
import { DatePickerComponent } from 'src/app/shared/components/date-picker/date-picker.component';
import { CategoryLabelPipe } from 'src/app/shared/pipes/category-label.pipe';

interface SelfReportPrompt {
  id: number;
  accomplishedOn: string;
  note: string;
}

@Component({
  selector: 'app-employee-activities-tab',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ActionIconComponent,
    FormsModule,
    EmployeeActivityModalComponent,
    ApproveActivityDialogComponent,
    ConfirmDialogComponent,
    DialogDirective,
    TooltipDirective,
    DatePickerComponent,
    CategoryLabelPipe,
  ],
  templateUrl: './employee-activities-tab.component.html',
  styleUrl: './employee-activities-tab.component.scss',
})
export class EmployeeActivitiesTabComponent {
  readonly employeeId = input.required<string>();

  private readonly svc = inject(EmployeeActivitiesService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  public readonly authService = inject(AuthService);

  // Inactive versions included: a record can still point at an old version or a deleted activity.
  readonly catalog = toSignal(
    inject(ActivitiesService)
      .getAll(undefined, true)
      .pipe(
        catchError((err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Failed to load activities.'));
          return of([]);
        }),
      ),
    { initialValue: [] },
  );
  protected readonly categoryBackground = categoryBackground;
  protected readonly canWriteManage = this.authService.canWriteManage;

  private readonly activePromotion = httpResource<PromotionDetail | null>(
    () => `/api/promotions/active/${this.employeeId()}`,
  );

  private withUsage(a: GroupedPromotionActivity, usedIn: ActivityUsage | null): GroupedPromotionActivity {
    return { ...a, usedIn };
  }

  readonly editCatalog = computed<GroupedPromotionActivity[]>(() => this.catalog().map((a) => this.withUsage(a, null)));

  readonly modalCatalog = computed<GroupedPromotionActivity[]>(() => {
    const all = this.catalog();
    const promo = this.activePromotion.value();

    const blockedMap = this.collectBlockedActivityMap(this.activities(), all, promo, this.locallyDeleted());

    const requiredIds = new Set(promo?.criteria?.requiredActivities.map((r) => r.id));

    return all
      .filter(
        (a) =>
          a.isActive &&
          !(a.technology != null && a.points === 0 && a.isPromotionRequirement) &&
          (promo == null || a.technology == null || a.technology === promo.technology),
      )
      .map((a) => {
        const usage = blockedMap.get(a.id) ?? (requiredIds.has(a.id) ? ('required' as ActivityUsage) : null);

        return this.withUsage(a, usage);
      });
  });

  readonly isOwnProfile = computed(() => this.employeeId() === this.authService.userId());

  canDelete(a: EmployeeActivity): boolean {
    return !a.createdById || this.canWriteManage() || a.createdById === this.authService.userId();
  }

  private readonly activitiesResource = httpResource<EmployeeActivity[]>(
    () => `/api/employees/${this.employeeId()}/activities`,
  );

  // Rows removed by the user this session — dropped locally so a delete doesn't need a full refetch.
  private readonly locallyDeleted = signal<Set<number>>(new Set());

  private readonly activities = computed(() => {
    const deleted = this.locallyDeleted();
    return (this.activitiesResource.value() ?? []).filter((a) => !deleted.has(a.id)).map((a) => this.enrichActivity(a));
  });

  private readonly logUnrecognizedStatus = effect(() => {
    for (const a of this.activities()) {
      if (!EMPLOYEE_ACTIVITY_STATUS[a.status]) {
        console.warn(`Employee activity ${a.id} has an unrecognized status`, a.status);
      }
    }
  });

  // True only on the first fetch — a reload keeps the list (and scroll position) on screen.
  readonly initialLoading = computed(
    () => this.activitiesResource.isLoading() && this.activitiesResource.value() === undefined,
  );

  readonly pending = computed(() =>
    this.activities().filter((a) => EMPLOYEE_ACTIVITY_STATUS[a.status]?.pending ?? false),
  );
  readonly approved = computed(() => this.activities().filter((a) => a.status === EmployeeActivityStatus.Approved));
  readonly totalApprovedPoints = computed(() => this.approved().reduce((sum, a) => sum + (a.pointsSnapshot ?? 0), 0));

  public readonly ActivityPeriod = ActivityPeriod;

  public readonly EmployeeActivityStatus = EmployeeActivityStatus;
  readonly modalMode = signal<ActivityModalMode>('report');
  readonly modalOpen = signal(false);
  readonly editingActivity = signal<EmployeeActivity | null>(null);

  readonly selfReportPromptId = signal<number | null>(null);
  readonly selfReportForm = signal<SelfReportPrompt>({ id: 0, accomplishedOn: '', note: '' });

  readonly approvingActivity = signal<EmployeeActivity | null>(null);

  readonly confirmDeleteId = signal<number | null>(null);

  openReportModal(): void {
    this.modalMode.set('report');
    this.editingActivity.set(null);
    this.modalOpen.set(true);
  }

  openAdminModal(): void {
    this.modalMode.set('admin');
    this.editingActivity.set(null);
    this.modalOpen.set(true);
  }

  openEditModal(activity: EmployeeActivity): void {
    this.modalMode.set('edit');
    this.editingActivity.set(activity);
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.editingActivity.set(null);
  }

  catalogActivity(activityId: number) {
    return this.catalog().find((a) => a.id === activityId);
  }

  onModalSave(req: CreateEmployeeActivityRequest | UpdateEmployeeActivityRequest): void {
    const editing = this.editingActivity();
    const isEdit = this.modalMode() === 'edit' && editing;
    const obs = isEdit
      ? this.svc.update(this.employeeId(), editing.id, req as UpdateEmployeeActivityRequest)
      : this.svc.create(this.employeeId(), req as CreateEmployeeActivityRequest);

    obs.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.closeModal();
        this.activitiesResource.reload();
        this.toast.success(isEdit ? 'Activity updated successfully.' : 'Activity added successfully.');
      },
      error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Failed to save activity.')),
    });
  }

  openSelfReportPrompt(id: number): void {
    this.selfReportPromptId.set(id);
    this.selfReportForm.set({ id, accomplishedOn: '', note: '' });
  }

  closeSelfReportPrompt(): void {
    this.selfReportPromptId.set(null);
  }

  // A self-reported activity records something already done, so the date can't be in the future.
  readonly isFutureSelfReportDate = computed(() => {
    const date = this.selfReportForm().accomplishedOn;
    return !!date && date > new Date().toLocaleDateString('sv-SE');
  });

  submitSelfReport(): void {
    const form = this.selfReportForm();
    if (!form.accomplishedOn || this.isFutureSelfReportDate()) return;
    const req: SelfReportRequest = {
      accomplishedOn: form.accomplishedOn,
      note: form.note || undefined,
    };
    this.svc
      .selfReport(this.employeeId(), this.selfReportPromptId()!, req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeSelfReportPrompt();
          this.activitiesResource.reload();
          this.toast.success('Activity reported successfully.');
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Failed to self-report activity.')),
      });
  }

  approve(id: number, accomplishedOn: string): void {
    this.approvingActivity.set(null);
    this.svc
      .approve(this.employeeId(), id, accomplishedOn)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.activitiesResource.reload();
          this.toast.success('Activity approved successfully.');
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Failed to approve activity.')),
      });
  }

  confirmDelete(id: number): void {
    this.confirmDeleteId.set(id);
  }

  cancelDelete(): void {
    this.confirmDeleteId.set(null);
  }

  deleteActivity(): void {
    const id = this.confirmDeleteId();
    if (id === null) return;

    const targetActivity = this.activities().find((a) => a.id === id);

    this.svc
      .delete(this.employeeId(), id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.confirmDeleteId.set(null);

          this.locallyDeleted.update((s) => new Set(s).add(id));

          if (targetActivity) {
            const currentPromo = this.activePromotion.value();
            if (currentPromo) {
              const updatedPromo: PromotionDetail = {
                ...currentPromo,
                activities: currentPromo.activities?.filter((a) => a.activityId !== targetActivity.activityId),
                plannedActivities: currentPromo.plannedActivities?.filter(
                  (a) => a.activityId !== targetActivity.activityId,
                ),
                selfReportedActivities: currentPromo.selfReportedActivities?.filter(
                  (a) => a.activityId !== targetActivity.activityId,
                ),
              };

              this.activePromotion.value.set(updatedPromo);
            }
          }

          this.toast.success('Activity deleted successfully.');
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Failed to delete activity.')),
      });
  }

  statusLabel(s: EmployeeActivityStatus): string {
    return EMPLOYEE_ACTIVITY_STATUS[s].label;
  }

  statusModifier(s: EmployeeActivityStatus): string {
    return EMPLOYEE_ACTIVITY_STATUS[s].modifier;
  }

  private enrichActivity(a: EmployeeActivity): EmployeeActivity {
    const catalogActivity = this.catalog().find((c) => c.id === a.activityId);
    if (!catalogActivity) return { ...a, expiresOn: null, expiryKind: null };
    const employeeActivity = structuredClone(a);
    const period = catalogActivity.withinPeriod;
    employeeActivity.points = catalogActivity.points;
    if (!employeeActivity.accomplishedOn || !period) {
      return { ...employeeActivity, expiresOn: null, expiryKind: null };
    }
    const base = new Date(employeeActivity.accomplishedOn);
    if (period === ActivityPeriod.SixMonths) {
      base.setMonth(base.getMonth() + 6);
      return { ...employeeActivity, expiresOn: base.toISOString().slice(0, 10), expiryKind: 'expires' };
    }
    if (period === ActivityPeriod.TwelveMonths) {
      base.setMonth(base.getMonth() + 12);
      return { ...employeeActivity, expiresOn: base.toISOString().slice(0, 10), expiryKind: 'expires' };
    }
    if (period === ActivityPeriod.OverallActive) {
      return { ...employeeActivity, expiresOn: null, expiryKind: 'lifetime' };
    }
    if (period === ActivityPeriod.OverallInLevel) {
      return { ...employeeActivity, expiresOn: null, expiryKind: 'in-level' };
    }
    return { ...employeeActivity, expiresOn: null, expiryKind: null };
  }

  private collectBlockedActivityMap(
    existingActivities: EmployeeActivity[],
    catalog: GroupedPromotionActivity[],
    promo: PromotionDetail | null | undefined,
    locallyDeleted: Set<number>,
  ): Map<number, ActivityUsage> {
    const usageMap = new Map<number, ActivityUsage>();

    const getPeriod = (activityId: number) => catalog.find((c) => Number(c.id) === Number(activityId))?.withinPeriod;

    const isLifetime = (period: ActivityPeriod | undefined) =>
      period === ActivityPeriod.OverallActive || period === ActivityPeriod.OverallInLevel;

    promo?.activities?.forEach((a) => {
      if (isLifetime(getPeriod(a.activityId))) usageMap.set(Number(a.activityId), 'approved' as ActivityUsage);
    });

    promo?.plannedActivities?.forEach((a) => {
      if (isLifetime(getPeriod(a.activityId))) usageMap.set(Number(a.activityId), 'planned' as ActivityUsage);
    });

    promo?.selfReportedActivities?.forEach((a) => {
      if (isLifetime(getPeriod(a.activityId))) usageMap.set(Number(a.activityId), 'selfReported' as ActivityUsage);
    });

    for (const a of existingActivities) {
      if (locallyDeleted.has(a.id)) continue;

      const catalogItem = catalog.find((c) => Number(c.id) === Number(a.activityId));
      if (!catalogItem) continue;

      const actId = Number(a.activityId);
      if (isLifetime(catalogItem.withinPeriod)) {
        if (a.status === EmployeeActivityStatus.Approved) usageMap.set(actId, 'approved' as ActivityUsage);
        else if (a.status === EmployeeActivityStatus.Planned) usageMap.set(actId, 'planned' as ActivityUsage);
        else if (a.status === EmployeeActivityStatus.SelfReported) usageMap.set(actId, 'selfReported' as ActivityUsage);
      }
    }

    return usageMap;
  }
}
