import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActivityCategory,
  ACTIVITY_USAGE,
  categoryLabel,
  GroupedPromotionActivity,
} from '../../../../activities/activity.model';
import { DialogDirective } from 'src/app/shared/directives/dialog.directive';
import { TooltipDirective } from 'src/app/shared/directives/tooltip.directive';
import {
  ADMIN_ASSIGNABLE_ACTIVITY_STATUSES,
  EMPLOYEE_ACTIVITY_STATUS,
  EmployeeActivityStatus,
} from 'src/app/shared/models/employee-activity-status.model';
import {
  CreateEmployeeActivityRequest,
  EmployeeActivity,
  UpdateEmployeeActivityRequest,
} from '../../../models/employee-activity.model';
import { DatePickerComponent } from 'src/app/shared/components/date-picker/date-picker.component';
import { PeriodLabelPipe } from 'src/app/shared/pipes/period-label.pipe';

export type ActivityModalMode = 'report' | 'admin' | 'edit';

interface ModalForm {
  activityId: number | null;
  status: EmployeeActivityStatus;
  accomplishedOn: string;
  note: string;
}

@Component({
  selector: 'app-employee-activity-modal',
  standalone: true,
  imports: [FormsModule, DatePickerComponent, DialogDirective, TooltipDirective, PeriodLabelPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './employee-activity-modal.component.html',
})
export class EmployeeActivityModalComponent {
  public readonly adminStatuses = ADMIN_ASSIGNABLE_ACTIVITY_STATUSES;
  readonly mode = input<ActivityModalMode>('report');
  readonly activity = input<EmployeeActivity | null>(null);
  readonly catalogActivities = input<GroupedPromotionActivity[]>([]);

  usageLabel(a: GroupedPromotionActivity): string {
    return a.usedIn ? ACTIVITY_USAGE[a.usedIn].label : '';
  }

  readonly save = output<CreateEmployeeActivityRequest | UpdateEmployeeActivityRequest>();
  readonly cancelled = output<void>();

  form: ModalForm = this.defaultForm();
  search = '';

  constructor() {
    effect(() => {
      const m = this.mode();
      const a = this.activity();
      if (m === 'edit' && a) {
        this.form = {
          activityId: a.activityId,
          status: a.status,
          accomplishedOn: a.accomplishedOn ? a.accomplishedOn.substring(0, 10) : '',
          note: a.note ?? '',
        };
      } else {
        this.form = this.defaultForm();
      }
      this.search = '';
    });
  }

  get title(): string {
    if (this.mode() === 'edit') return 'Edit Activity';
    if (this.mode() === 'admin') return 'Add Activity';
    return 'Report Activity';
  }

  get isEditMode(): boolean {
    return this.mode() === 'edit';
  }

  get isAdminMode(): boolean {
    return this.mode() === 'admin';
  }

  get filteredActivities(): GroupedPromotionActivity[] {
    const q = this.search.toLowerCase();
    return this.catalogActivities().filter(
      (a) => a.name.toLowerCase().includes(q) || categoryLabel(a.category).toLowerCase().includes(q),
    );
  }

  get groupedActivities(): { category: ActivityCategory; label: string; items: GroupedPromotionActivity[] }[] {
    const groups = new Map<ActivityCategory, GroupedPromotionActivity[]>();
    for (const a of this.filteredActivities) {
      if (!groups.has(a.category)) groups.set(a.category, []);
      groups.get(a.category)!.push(a);
    }
    return [...groups.entries()].map(([category, items]) => ({
      category,
      label: categoryLabel(category),
      items,
    }));
  }

  selectActivity(id: number): void {
    if (id !== 0 && this.catalogActivities().find((a) => a.id === id)?.usedIn) return;
    this.form = { ...this.form, activityId: id };
  }

  get recordsPastWork(): boolean {
    return EMPLOYEE_ACTIVITY_STATUS[this.form.status].recordsPastWork;
  }

  get isFutureDateInvalid(): boolean {
    const today = new Date().toLocaleDateString('sv-SE');
    return this.recordsPastWork && this.form.accomplishedOn > today;
  }

  isValid(): boolean {
    if (!this.form.activityId) return false;
    if (this.mode() !== 'edit' && !this.form.accomplishedOn) return false;
    if (
      this.isAdminMode &&
      EMPLOYEE_ACTIVITY_STATUS[this.form.status].requiresAccomplishedOn &&
      !this.form.accomplishedOn
    )
      return false;
    if (this.isFutureDateInvalid) return false;
    return true;
  }

  submit(): void {
    if (this.mode() === 'edit') {
      const req: UpdateEmployeeActivityRequest = {
        activityId: this.form.activityId ?? undefined,
        accomplishedOn: this.form.accomplishedOn || undefined,
        note: this.form.note || undefined,
      };
      this.save.emit(req);
      return;
    }

    const status: EmployeeActivityStatus = this.isAdminMode ? this.form.status : EmployeeActivityStatus.SelfReported;
    const req: CreateEmployeeActivityRequest = {
      activityId: this.form.activityId!,
      status,
      accomplishedOn: this.form.accomplishedOn || undefined,
      note: this.form.note || undefined,
    };
    this.save.emit(req);
  }

  statusLabel(status: EmployeeActivityStatus): string {
    return EMPLOYEE_ACTIVITY_STATUS[status].label;
  }

  selectedActivityName(): string {
    if (!this.form.activityId) return '';
    return this.catalogActivities().find((a) => a.id === this.form.activityId)?.name ?? '';
  }

  private defaultForm(): ModalForm {
    return { activityId: null, status: EmployeeActivityStatus.SelfReported, accomplishedOn: '', note: '' };
  }
}
