import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import {
  KNOWLEDGE_EVALUATION_OPTION_LABEL,
  KNOWLEDGE_EVALUATION_STATUS,
  KnowledgeEvaluation,
  KnowledgeEvaluationStatus,
} from '../../models/knowledge-evaluation.model';
import { KnowledgeEvaluationsService } from '../../services/knowledge-evaluations.service';

@Component({
  selector: 'app-knowledge-evaluation-card',
  templateUrl: './knowledge-evaluation-card.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KnowledgeEvaluationCardComponent {
  private readonly service = inject(KnowledgeEvaluationsService);
  private readonly toastService = inject(ToastService);

  public readonly promotionId = input.required<number>();
  public readonly canPlan = input(false);
  // "KE required" can be ticked by hand, with no request behind it.
  public readonly criterionMet = input(false);

  public readonly changed = output<void>();

  protected readonly optionLabel = KNOWLEDGE_EVALUATION_OPTION_LABEL;
  protected readonly evaluation = signal<KnowledgeEvaluation | null>(null);
  protected readonly loaded = signal(false);
  protected readonly modalOpen = signal(false);
  private readonly refreshCount = signal(0);

  protected readonly status = computed(() => {
    const evaluation = this.evaluation();
    return evaluation ? KNOWLEDGE_EVALUATION_STATUS[evaluation.status] : null;
  });

  // A planned or passed evaluation can't be requested again.
  protected readonly canRequest = computed(() => {
    const status = this.evaluation()?.status;
    return (
      this.canPlan() &&
      !this.criterionMet() &&
      this.loaded() &&
      status !== KnowledgeEvaluationStatus.Planned &&
      status !== KnowledgeEvaluationStatus.Successful
    );
  });

  // The request can still be changed until HR has set a date.
  protected readonly requestToEdit = computed(() => {
    const evaluation = this.evaluation();
    const editable =
      this.canPlan() && evaluation?.status === KnowledgeEvaluationStatus.Planned && !evaluation.evaluationDate;
    return editable ? evaluation : null;
  });

  constructor() {
    toObservable(computed(() => ({ id: this.promotionId(), refresh: this.refreshCount() })))
      .pipe(
        switchMap(({ id }) =>
          this.service.getLatest(id).pipe(
            catchError((err) => {
              this.toastService.error(firstApiErrorMessage(err, 'Failed to load the knowledge evaluation.'));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((evaluation) => {
        this.evaluation.set(evaluation);
        this.loaded.set(true);
      });
  }

  onRequested(): void {
    this.modalOpen.set(false);
    this.refreshCount.update((count) => count + 1);
    this.changed.emit();
  }
}
