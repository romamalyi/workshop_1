import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EmailRecipient,
  EmailRecipients,
  RECIPIENT_ROLE_LABELS,
  RecipientRole,
} from 'src/app/core/services/email-templates.service';
import { Employee } from 'src/app/employees/models/employee.model';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { SearchOption } from 'src/app/shared/models/search-option.model';
import { PromotionsService } from '../../services/promotions.service';

@Component({
  selector: 'app-send-promotion-dialog',
  templateUrl: './send-promotion-dialog.component.html',
  styleUrl: './send-promotion-dialog.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SendPromotionDialogComponent implements OnInit {
  private readonly promotionsService = inject(PromotionsService);
  private readonly employeesService = inject(EmployeesService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly promotionId = input.required<number>();
  public readonly subject = input.required<string>();
  public readonly body = input.required<string>();
  public readonly confirmed = output<string[]>();
  public readonly cancelled = output<void>();

  public readonly levelConfirmed = signal<boolean>(false);
  public readonly recipients = signal<EmailRecipients | null>(null);
  public readonly recipientsFailed = signal<boolean>(false);
  public readonly addedCc = signal<Employee[]>([]);
  private readonly employees = signal<Employee[]>([]);

  public readonly ccCandidates = computed<SearchOption[]>(() => {
    const r = this.recipients();
    const listed = new Set(
      [...(r?.to ?? []), ...(r?.cc ?? []), ...this.addedCc()].map((person) => person.email.toLowerCase()),
    );
    return this.employees()
      .filter((e) => e.email?.trim() && !listed.has(e.email.toLowerCase()))
      .map((e) => ({ id: e.id, label: this.fullName(e) }));
  });

  public readonly canSend = computed(() => this.levelConfirmed() && (this.recipients()?.to.length ?? 0) > 0);

  ngOnInit(): void {
    this.promotionsService
      .getEmailRecipients(this.promotionId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipients) => this.recipients.set(recipients),
        error: () => this.recipientsFailed.set(true),
      });
    this.employeesService
      .getActiveEmployees()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (employees) => this.employees.set(employees), error: () => this.employees.set([]) });
  }

  roleLabel(role: RecipientRole): string {
    return RECIPIENT_ROLE_LABELS[role];
  }

  rolesOf(recipient: EmailRecipient): string {
    return recipient.roles.map((role) => RECIPIENT_ROLE_LABELS[role]).join(', ');
  }

  focusCcInput(event: Event): void {
    (event.currentTarget as HTMLElement).querySelector('input')?.focus();
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0].toUpperCase())
      .join('');
  }

  fullName(employee: Employee): string {
    return `${employee.firstName} ${employee.lastName}`.trim();
  }

  addCc(option: SearchOption | null): void {
    const employee = this.employees().find((e) => e.id === option?.id);
    if (employee) this.addedCc.update((added) => [...added, employee]);
  }

  removeCc(employeeId: string): void {
    this.addedCc.update((added) => added.filter((e) => e.id !== employeeId));
  }

  send(): void {
    if (!this.canSend()) return;
    this.confirmed.emit(this.addedCc().map((e) => e.id));
  }
}
