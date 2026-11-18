import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, map, of, switchMap } from 'rxjs';
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
  selector: 'app-knowledge-evaluations-page',
  templateUrl: './knowledge-evaluations-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KnowledgeEvaluationsPageComponent {
  private readonly service = inject(KnowledgeEvaluationsService);
  private readonly toastService = inject(ToastService);

  protected readonly statuses = KnowledgeEvaluationStatus;
  protected readonly statusMeta = KNOWLEDGE_EVALUATION_STATUS;
  protected readonly optionLabel = KNOWLEDGE_EVALUATION_OPTION_LABEL;
  protected readonly statusLabels = Object.values(KnowledgeEvaluationStatus).map(
    (value) => KNOWLEDGE_EVALUATION_STATUS[value].label,
  );

  // Empty selection means "All"; `statusesCleared` is "nothing checked", which shows nothing.
  // The page opens on the evaluations still open; finished ones are one filter change away.
  protected readonly selectedStatuses = signal<string[]>([
    KNOWLEDGE_EVALUATION_STATUS[KnowledgeEvaluationStatus.Planned].label,
  ]);
  protected readonly statusesCleared = signal(false);
  protected readonly items = signal<KnowledgeEvaluation[] | null>(null);
  protected readonly editing = signal<KnowledgeEvaluation | null>(null);
  private readonly refreshCount = signal(0);

  protected readonly visibleItems = computed(() => {
    const items = this.items();
    if (!items) return null;
    if (this.statusesCleared()) return [];
    const selected = this.selectedStatuses();
    return selected.length
      ? items.filter((e) => selected.includes(KNOWLEDGE_EVALUATION_STATUS[e.status].label))
      : items;
  });

  constructor() {
    toObservable(this.refreshCount)
      .pipe(
        switchMap(() =>
          this.service.getAll().pipe(
            map((items) => [...items].sort((a, b) => this.attentionRank(a) - this.attentionRank(b))),
            catchError((err) => {
              this.toastService.error(firstApiErrorMessage(err, 'Failed to load knowledge evaluations.'));
              return of([] as KnowledgeEvaluation[]);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((items) => this.items.set(items));
  }

  protected missingConductor(e: KnowledgeEvaluation): boolean {
    return e.status === KnowledgeEvaluationStatus.Planned && !e.conductedById;
  }

  protected missingDate(e: KnowledgeEvaluation): boolean {
    return e.status === KnowledgeEvaluationStatus.Planned && !e.evaluationDate;
  }

  protected needsScheduling(e: KnowledgeEvaluation): boolean {
    return this.missingConductor(e) || this.missingDate(e);
  }

  // Needs scheduling first, then overdue, then the rest.
  private attentionRank(e: KnowledgeEvaluation): number {
    if (this.needsScheduling(e)) return 0;
    return this.isOverdue(e) ? 1 : 2;
  }

  // The date is a calendar day (yyyy-MM-dd), so comparing it with the local day as text is enough.
  protected isOverdue(e: KnowledgeEvaluation): boolean {
    if (e.status !== KnowledgeEvaluationStatus.Planned || !e.evaluationDate) return false;
    const now = new Date();
    const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    return e.evaluationDate < today;
  }

  onSaved(): void {
    this.editing.set(null);
    this.refreshCount.update((count) => count + 1);
  }
}
