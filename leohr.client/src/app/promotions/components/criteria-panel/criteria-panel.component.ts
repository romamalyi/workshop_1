import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { PromotionCriteria } from '../../../promotion-criteria/promotion-criteria.model';
import { CriterionOverride } from '../../models/promotion.model';
import { buildCriteriaChecklist, CriteriaChecklistItem } from '../../../promotion-criteria/promotion-criteria.util';

export interface CriteriaCheckItem extends CriteriaChecklistItem {
  done: boolean;
  comment: string | null;
  locked: boolean;
  automatic: boolean;
  revertible: boolean;
}

export interface CriterionChange {
  label: string;
  met: boolean;
  comment: string | null;
}

@Component({
  selector: 'app-criteria-panel',
  templateUrl: './criteria-panel.component.html',
  styleUrls: ['./criteria-panel.component.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaPanelComponent {
  public criteria = input<PromotionCriteria | null>(null);
  public checkedLabels = input<Set<string>>(new Set());
  public disableAll = input<boolean>(false);
  public verifying = input<boolean>(false);
  public overrides = input<CriterionOverride[]>([]);
  // Ticked criteria that can't be unticked by hand.
  public lockedLabels = input<string[]>([]);
  // Criteria the server ticks and unticks itself; never marked or reverted by hand.
  public automaticLabels = input<string[]>([]);
  public criterionChange = output<CriterionChange>();

  public readonly marking = signal<CriteriaCheckItem | null>(null);
  public readonly comment = signal<string>('');

  public readonly items = computed<CriteriaCheckItem[]>(() =>
    buildCriteriaChecklist(this.criteria()).map((item) => {
      const mark = this.overrides().find((o) => o.label === item.label);
      const comment = mark?.text || null;
      // A promotion sent with an unmet criterion can carry a mark for it without the tick.
      const done = this.checkedLabels().has(item.label) || mark !== undefined;
      const automatic = this.automaticLabels().includes(item.label);
      const locked = done && this.lockedLabels().includes(item.label);
      return {
        ...item,
        done,
        comment,
        locked,
        automatic,
        revertible: !this.disableAll() && done && !locked && !automatic,
      };
    }),
  );

  public readonly checkedCount = computed(() => this.items().filter((i) => i.done).length);

  startMark(item: CriteriaCheckItem): void {
    if (!this.isMarkable(item)) return;
    this.comment.set('');
    this.marking.set(item);
  }

  cancelMark(): void {
    this.marking.set(null);
  }

  confirmMark(): void {
    const item = this.marking();
    if (!item) return;
    this.criterionChange.emit({ label: item.label, met: true, comment: this.comment().trim() || null });
    this.marking.set(null);
  }

  isMarkable(item: CriteriaCheckItem): boolean {
    return !this.disableAll() && !item.done && !item.automatic;
  }

  revert(item: CriteriaCheckItem): void {
    if (!item.revertible) return;
    this.criterionChange.emit({ label: item.label, met: false, comment: null });
  }
}
