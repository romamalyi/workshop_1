import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import {
  Activity,
  ActivityCategory,
  ActivityPeriod,
  CreateActivityRequest,
  UpdateActivityRequest,
} from '../../activity.model';
import { TECHNOLOGIES } from '../../../promotion-criteria/promotion-criteria.model';

@Component({
  selector: 'app-activity-modal',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './activity-modal.component.html',
})
export class ActivityModalComponent {
  readonly activity = input<Activity | null>(null);

  readonly save = output<{ request: CreateActivityRequest | UpdateActivityRequest; mode: 'Soft' | 'Hard' }>();
  readonly cancel = output<void>();

  readonly categories = Object.values(ActivityCategory);
  readonly periods = Object.values(ActivityPeriod);
  technologies = TECHNOLOGIES;
  preserveHistory = false;

  form: CreateActivityRequest = {
    category: ActivityCategory.SelfCourses,
    name: '',
    withinPeriod: ActivityPeriod.TwelveMonths,
    points: 0,
    technology: null,
  };

  constructor() {
    effect(() => {
      const currentActivity = this.activity();

      if (currentActivity) {
        this.form = {
          category: currentActivity.category,
          name: currentActivity.name,
          withinPeriod: currentActivity.withinPeriod,
          points: currentActivity.points,
          technology: currentActivity.technology,
        };
      } else {
        this.form = {
          category: ActivityCategory.SelfCourses,
          name: '',
          withinPeriod: ActivityPeriod.TwelveMonths,
          points: 0,
          technology: null,
        };
      }
      this.preserveHistory = false;
    });
  }

  isValid(): boolean {
    return this.form.name.trim().length > 0 && this.form.points >= 0 && this.form.points <= 1000;
  }

  submit(): void {
    this.save.emit({ request: this.form, mode: this.preserveHistory ? 'Soft' : 'Hard' });
  }
}
