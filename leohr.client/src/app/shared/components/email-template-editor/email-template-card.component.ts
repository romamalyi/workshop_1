import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EmailTemplateSettings,
  EmailTemplatesService,
  RECIPIENT_ROLE_LABELS,
  RecipientRole,
} from 'src/app/core/services/email-templates.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';

type RecipientField = 'to' | 'cc';

@Component({
  selector: 'app-email-template-card',
  standalone: false,
  templateUrl: './email-template-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailTemplateCardComponent {
  private readonly emailTemplates = inject(EmailTemplatesService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly template = input.required<EmailTemplateSettings>();

  public readonly subjectMaxLength = 300;
  public readonly roleLabels = RECIPIENT_ROLE_LABELS;

  public readonly saved = linkedSignal(() => this.template());
  public readonly subject = linkedSignal(() => this.template().subject);
  public readonly body = linkedSignal(() => this.template().body);
  public readonly to = linkedSignal(() => this.template().toRecipients);
  public readonly cc = linkedSignal(() => this.template().ccRecipients);
  public readonly saving = signal(false);
  public readonly showPreview = signal(false);

  public readonly subjectMissing = computed(() => !this.subject().trim());
  public readonly bodyMissing = computed(() => !this.body().trim());
  public readonly toMissing = computed(() => this.to().length === 0);

  public readonly isDirty = computed(() => {
    const saved = this.saved();
    return (
      this.subject() !== saved.subject ||
      this.body() !== saved.body ||
      !sameRoles(this.to(), saved.toRecipients) ||
      !sameRoles(this.cc(), saved.ccRecipients)
    );
  });

  public readonly canSave = computed(
    () => this.isDirty() && !this.subjectMissing() && !this.bodyMissing() && !this.toMissing() && !this.saving(),
  );

  isChecked(field: RecipientField, role: RecipientRole): boolean {
    return (field === 'to' ? this.to() : this.cc()).includes(role);
  }

  // A role is in To or CC, never both.
  setRecipient(field: RecipientField, role: RecipientRole, checked: boolean): void {
    const [target, other] = field === 'to' ? [this.to, this.cc] : [this.cc, this.to];
    target.update((roles) => (checked ? [...roles, role] : roles.filter((r) => r !== role)));
    if (checked) other.update((roles) => roles.filter((r) => r !== role));
  }

  reset(): void {
    const saved = this.saved();
    this.subject.set(saved.subject);
    this.body.set(saved.body);
    this.to.set(saved.toRecipients);
    this.cc.set(saved.ccRecipients);
  }

  save(): void {
    const saved = this.saved();
    if (!this.canSave()) return;
    const changes = { subject: this.subject(), body: this.body(), toRecipients: this.to(), ccRecipients: this.cc() };
    this.saving.set(true);
    this.emailTemplates
      .update(saved.id, changes)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saved.set({ ...saved, ...changes });
          this.saving.set(false);
          this.toastService.success(`${saved.label} email saved.`);
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.toastService.error(firstApiErrorMessage(err, `Failed to save the ${saved.label} email.`));
        },
      });
  }
}

function sameRoles(a: RecipientRole[], b: RecipientRole[]): boolean {
  return a.length === b.length && a.every((role) => b.includes(role));
}
