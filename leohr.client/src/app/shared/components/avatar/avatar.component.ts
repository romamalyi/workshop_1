import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-avatar',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="avatar-circle"
      [style.background-color]="color"
      [style.width.px]="size"
      [style.height.px]="size"
      [style.font-size.px]="size >= 56 ? 18 : 13"
      [style.font-weight]="size >= 56 ? 700 : 600"
    >
      {{ initials }}
    </div>
  `,
})
export class AvatarComponent {
  @Input() initials = '';
  @Input() color = '#A8D5C2';
  @Input() size = 36;
}
