import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { PROMOTION_STATUS, PromotionStatus } from '../../models/promotion-status.model';

@Component({
  selector: 'app-badge',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge badge--{{ modifier }}">{{ label }}</span>`,
})
export class BadgeComponent {
  @Input() status: PromotionStatus = PromotionStatus.Ready;

  get label(): string {
    return PROMOTION_STATUS[this.status].label;
  }

  get modifier(): string {
    return PROMOTION_STATUS[this.status].modifier;
  }
}
