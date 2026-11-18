import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type ActionIconName = 'check' | 'edit' | 'trash' | 'restore' | 'send' | 'copy' | 'archive';

@Component({
  selector: 'app-action-icon',
  templateUrl: './action-icon.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActionIconComponent {
  public readonly name = input.required<ActionIconName>();
}
