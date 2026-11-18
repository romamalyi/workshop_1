import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DialogDirective } from '../../directives/dialog.directive';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [DialogDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal-backdrop" appDialog (dialogClose)="dismissed.emit()">
      <div class="modal-box modal-box--sm" role="dialog">
        <h2 class="modal-title">{{ title() }}</h2>
        <p class="modal-body">{{ message() }}</p>
        <div class="modal-actions">
          <button class="btn btn--secondary" (click)="dismissed.emit()">
            {{ cancelLabel() }}
          </button>
          <button
            class="btn"
            [class.btn--primary]="confirmVariant() === 'primary'"
            [class.btn--danger]="confirmVariant() !== 'primary'"
            (click)="confirm.emit()"
          >
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    </div>
  `,
})
export class ConfirmDialogComponent {
  readonly title = input('Are you sure?');
  readonly message = input('');
  readonly confirmLabel = input('Delete');
  readonly cancelLabel = input('Cancel');
  readonly confirmVariant = input<'danger' | 'primary'>('danger');
  readonly confirm = output<void>();
  readonly dismissed = output<void>();
}
