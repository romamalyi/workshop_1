import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { Employee } from 'src/app/employees/models/employee.model';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SearchOption, toEmployeeOptions } from 'src/app/shared/models/search-option.model';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import {
  KNOWLEDGE_EVALUATION_STATUS,
  KnowledgeEvaluation,
  KnowledgeEvaluationOption,
  KnowledgeEvaluationStatus,
} from '../../models/knowledge-evaluation.model';
import { KnowledgeEvaluationsService } from '../../services/knowledge-evaluations.service';

@Component({
  selector: 'app-knowledge-evaluation-edit-modal',
  templateUrl: './knowledge-evaluation-edit-modal.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KnowledgeEvaluationEditModalComponent implements OnInit {
  private readonly service = inject(KnowledgeEvaluationsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly toastService = inject(ToastService);

  public readonly evaluation = input.required<KnowledgeEvaluation>();
  public readonly saved = output<void>();
  public readonly closeModal = output<void>();

  private readonly employees = toSignal(
    this.employeesService.getActiveEmployees().pipe(
      catchError((err) => {
        this.toastService.error(firstApiErrorMessage(err, 'Failed to load employees.'));
        return of([] as Employee[]);
      }),
    ),
    { initialValue: [] as Employee[] },
  );

  public readonly statuses = Object.values(KnowledgeEvaluationStatus).map((value) => ({
    value,
    label: KNOWLEDGE_EVALUATION_STATUS[value].label,
  }));

  public readonly status = signal(KnowledgeEvaluationStatus.Planned);
  public readonly conductorId = signal<string | null>(null);
  public readonly date = signal('');
  public readonly link = signal('');
  public readonly saving = signal(false);

  // A planned KE can't be scheduled in the past; a recorded result keeps the date it was actually held.
  public readonly isPlanned = computed(() => this.status() === KnowledgeEvaluationStatus.Planned);

  public readonly requestedOnDate = computed(
    () => this.evaluation().requestedOption === KnowledgeEvaluationOption.OnDate,
  );

  public readonly conductorName = computed(() => {
    const conductor = this.employees().find((e) => e.id === this.conductorId());
    if (conductor) return `${conductor.firstName} ${conductor.lastName}`;
    return this.conductorId() === this.evaluation().conductedById ? (this.evaluation().conductedByName ?? '') : '';
  });

  public readonly conductorOptions = computed(() =>
    toEmployeeOptions(this.employees().filter((e) => e.id !== this.evaluation().employeeId)),
  );

  ngOnInit(): void {
    const evaluation = this.evaluation();
    this.status.set(evaluation.status);
    this.conductorId.set(evaluation.conductedById);
    this.date.set(evaluation.evaluationDate ?? '');
    this.link.set(evaluation.link ?? '');
  }

  selectConductor(option: SearchOption | null): void {
    this.conductorId.set(option?.id ?? null);
  }

  // Closing mid-save would destroy the modal before `saved` reaches the page, leaving stale data.
  close(): void {
    if (!this.saving()) this.closeModal.emit();
  }

  save(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.service
      .update(this.evaluation().id, {
        status: this.status(),
        conductedById: this.conductorId(),
        evaluationDate: this.date() || null,
        link: this.link().trim() || null,
      })
      .subscribe({
        next: () => {
          this.toastService.success('Knowledge evaluation updated.');
          this.saved.emit();
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to update the knowledge evaluation.'));
        },
      });
  }
}
