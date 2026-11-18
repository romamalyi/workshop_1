import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';
import { DialogDirective } from '../../directives/dialog.directive';
import { DatePickerComponent } from '../date-picker/date-picker.component';

@Component({
  selector: 'app-approve-activity-dialog',
  templateUrl: './approve-activity-dialog.component.html',
  standalone: true,
  imports: [DatePickerComponent, DialogDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApproveActivityDialogComponent {
  readonly activityName = input('');
  readonly accomplishedOn = input<string | null>(null);
  readonly note = input<string | null>(null);
  readonly confirm = output<string>();
  readonly cancel = output<void>();

  private readonly today = new Date().toLocaleDateString('sv-SE');
  readonly date = linkedSignal(() => this.accomplishedOn() ?? this.today);
  readonly isFutureDate = computed(() => this.date() > this.today);
}
