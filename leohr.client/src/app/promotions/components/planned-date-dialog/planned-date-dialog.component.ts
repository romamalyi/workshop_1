import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';

@Component({
  selector: 'app-planned-date-dialog',
  templateUrl: './planned-date-dialog.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlannedDateDialogComponent {
  readonly activityName = input('');
  readonly plannedAt = input.required<string>();
  readonly confirm = output<string>();
  readonly cancel = output<void>();

  private readonly today = new Date().toLocaleDateString('sv-SE');
  readonly date = linkedSignal(() => new Date(this.plannedAt()).toLocaleDateString('sv-SE'));
  readonly isFutureDate = computed(() => this.date() > this.today);
}
