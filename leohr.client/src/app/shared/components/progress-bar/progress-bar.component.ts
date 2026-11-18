import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-progress-bar',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="progress"
      role="progressbar"
      aria-valuemin="0"
      [attr.aria-label]="label()"
      [attr.aria-valuemax]="max()"
      [attr.aria-valuenow]="value()"
    >
      @if (planned() > 0) {
        <div class="progress__planned" [style.width.%]="plannedPct()"></div>
      }
      <div class="progress__fill" [style.width.%]="pct()"></div>
    </div>
  `,
})
export class ProgressBarComponent {
  readonly label = input.required<string>();
  readonly value = input(0);
  readonly max = input(4);
  readonly planned = input(0);

  readonly pct = computed(() => this.share(this.value()));
  readonly plannedPct = computed(() => this.share(this.value() + this.planned()));

  private share(points: number): number {
    return this.max() > 0 ? Math.min(100, (points / this.max()) * 100) : 0;
  }
}
