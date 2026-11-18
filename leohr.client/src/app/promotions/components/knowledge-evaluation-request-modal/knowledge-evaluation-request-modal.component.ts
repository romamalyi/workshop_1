import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import {
  KNOWLEDGE_EVALUATION_OPTION_LABEL,
  KnowledgeEvaluation,
  KnowledgeEvaluationOption,
} from '../../models/knowledge-evaluation.model';
import { KnowledgeEvaluationsService } from '../../services/knowledge-evaluations.service';

@Component({
  selector: 'app-knowledge-evaluation-request-modal',
  templateUrl: './knowledge-evaluation-request-modal.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KnowledgeEvaluationRequestModalComponent implements OnInit {
  private readonly service = inject(KnowledgeEvaluationsService);
  private readonly toastService = inject(ToastService);

  public readonly promotionId = input.required<number>();
  // Set when changing an existing request instead of creating one.
  public readonly evaluation = input<KnowledgeEvaluation | null>(null);
  public readonly saved = output<void>();
  public readonly closeModal = output<void>();

  public readonly options = Object.values(KnowledgeEvaluationOption).map((value) => ({
    value,
    label: KNOWLEDGE_EVALUATION_OPTION_LABEL[value],
  }));

  public readonly date = signal('');
  public readonly option = signal(KnowledgeEvaluationOption.OnDate);
  public readonly comment = signal('');
  public readonly saving = signal(false);

  public readonly canSave = computed(() => !this.saving() && !!this.date());

  ngOnInit(): void {
    const evaluation = this.evaluation();
    if (!evaluation) return;
    this.date.set(evaluation.requestedDate);
    this.option.set(evaluation.requestedOption);
    this.comment.set(evaluation.comment ?? '');
  }

  // Closing mid-save would destroy the modal before `saved` reaches the page, leaving stale data.
  close(): void {
    if (!this.saving()) this.closeModal.emit();
  }

  save(): void {
    if (!this.canSave()) return;
    this.saving.set(true);
    const request = {
      requestedDate: this.date(),
      requestedOption: this.option(),
      comment: this.comment().trim() || null,
    };
    const existing = this.evaluation();
    const call = existing
      ? this.service.updateRequest(existing.id, request)
      : this.service.create(this.promotionId(), request);
    call.subscribe({
      next: () => {
        this.toastService.success(existing ? 'Request updated.' : 'Knowledge evaluation requested.');
        this.saved.emit();
      },
      error: (err) => {
        this.saving.set(false);
        this.toastService.error(
          firstApiErrorMessage(
            err,
            existing ? 'Failed to update the request.' : 'Failed to request the knowledge evaluation.',
          ),
        );
      },
    });
  }
}
