import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Activity } from '../../../activities/activity.model';
import { PromotionActivity, PromotionDetail } from '../../services/promotions.service';
import { AuthService } from 'src/app/core/services/auth.service';
import { PROMOTION_STATUS, PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { ActivityListType } from '../../models/activity-list-type.model';

@Component({
  selector: 'app-activities-section',
  templateUrl: './activities-section.component.html',
  styleUrls: ['./activities-section.component.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivitiesSectionComponent {
  readonly activities = input<PromotionActivity[]>([]);
  readonly plannedActivities = input<PromotionActivity[]>([]);
  readonly selfReportedActivities = input<PromotionActivity[]>([]);
  readonly pointsAchieved = input(0);
  readonly pointsPlanned = input(0);
  readonly pointsRequired = input<number | null>(null);
  readonly promotionId = input.required<number>();
  readonly employeeId = input.required<string>();
  readonly promotion = input.required<PromotionDetail>();

  readonly planActivity = output<Activity[]>();
  readonly approveActivity = output<{ employeeId: string; employeeActivityId: number }>();
  readonly removeActivity = output<number>();
  readonly revertApproval = output<number>();
  readonly reportActivity = output<{ employeeActivityId: number; accomplishedOn: string; note?: string }>();

  protected readonly ActivityListType = ActivityListType;

  readonly showPicker$ = new BehaviorSubject(false);
  public readonly authService = inject(AuthService);

  readonly reportPromptId = signal<number | null>(null);
  readonly reportForm = signal<{ accomplishedOn: string; note: string }>({ accomplishedOn: '', note: '' });

  public canEditActivities = computed(
    () =>
      PROMOTION_STATUS[this.promotion().status].activitiesEditable &&
      (this.authService.canWriteManage() || this.employeeId() === this.authService.userId()),
  );
  readonly carriedActivities = computed(() => this.activities().filter((a) => a.isCarriedOver));
  readonly earnedActivities = computed(() => this.activities().filter((a) => !a.isCarriedOver));
  readonly hasActivities = computed(
    () => this.activities().length + this.plannedActivities().length + this.selfReportedActivities().length > 0,
  );
  readonly carriedPoints = computed(() => this.carriedActivities().reduce((sum, a) => sum + a.pointsSnapshot, 0));

  canApprove(_a: PromotionActivity): boolean {
    return this.authService.canWriteManage() && this.promotion().status !== PromotionStatus.Sent;
  }

  canRevert(_a: PromotionActivity): boolean {
    return this.authService.canWriteManage() && this.promotion().status !== PromotionStatus.Sent;
  }

  canRemove(a: PromotionActivity): boolean {
    return (
      this.canEditActivities() &&
      (this.authService.canWriteManage() || !a.createdById || a.createdById === this.authService.userId())
    );
  }

  canReport(_a: PromotionActivity): boolean {
    return !this.authService.canWriteManage() && this.canEditActivities();
  }

  openReportPrompt(employeeActivityId: number): void {
    this.reportForm.set({ accomplishedOn: '', note: '' });
    this.reportPromptId.set(employeeActivityId);
  }

  closeReportPrompt(): void {
    this.reportPromptId.set(null);
  }

  submitReport(): void {
    const id = this.reportPromptId();
    const form = this.reportForm();
    if (id === null || !form.accomplishedOn) return;
    this.reportActivity.emit({
      employeeActivityId: id,
      accomplishedOn: form.accomplishedOn,
      note: form.note || undefined,
    });
    this.reportPromptId.set(null);
  }

  openPicker(): void {
    this.showPicker$.next(true);
  }

  closePicker(): void {
    this.showPicker$.next(false);
  }
}
