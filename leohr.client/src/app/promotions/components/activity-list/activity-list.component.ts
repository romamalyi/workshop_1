import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { categoryBackground } from 'src/app/activities/activity.model';
import { PromotionActivity } from '../../services/promotions.service';
import { ACTIVITY_LIST_TYPE, ActivityListType } from '../../models/activity-list-type.model';

@Component({
  selector: 'app-activity-list',
  standalone: false,
  templateUrl: './activity-list.component.html',
  styleUrls: ['./activity-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityListComponent {
  readonly title = input.required<string>();
  readonly activities = input.required<PromotionActivity[]>();
  readonly listType = input.required<ActivityListType>();

  readonly canApproveFn = input<(activity: PromotionActivity) => boolean>();
  readonly canRemoveFn = input<(activity: PromotionActivity) => boolean>();
  readonly canReportFn = input<(activity: PromotionActivity) => boolean>();
  readonly canRevertFn = input<(activity: PromotionActivity) => boolean>();

  readonly approve = output<number>();
  readonly remove = output<number>();
  readonly markReported = output<number>();
  readonly revert = output<number>();

  private readonly meta = computed(() => ACTIVITY_LIST_TYPE[this.listType()]);

  protected readonly categoryBackground = categoryBackground;

  readonly isPending = computed(() => this.listType() === ActivityListType.Pending);
  readonly isEarned = computed(() => this.meta().earned);
  readonly statusBadgeClass = computed(() => `badge--${this.meta().modifier}`);
  readonly statusText = computed(() => this.meta().statusText);
  readonly approveLabel = computed(() => this.meta().approveLabel);

  canApprove(activity: PromotionActivity): boolean {
    const fn = this.canApproveFn();
    return fn ? fn(activity) : false;
  }

  canReport(activity: PromotionActivity): boolean {
    const fn = this.canReportFn();
    return fn ? fn(activity) : false;
  }

  canRevert(activity: PromotionActivity): boolean {
    const fn = this.canRevertFn();
    return fn ? fn(activity) : false;
  }

  canRemove(activity: PromotionActivity): boolean {
    const fn = this.canRemoveFn();
    return fn ? fn(activity) : false;
  }
}
