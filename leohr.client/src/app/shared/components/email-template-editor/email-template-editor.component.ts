import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EmailTemplateCategory,
  EmailTemplateSettings,
  EmailTemplatesService,
} from 'src/app/core/services/email-templates.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';

@Component({
  selector: 'app-email-template-editor',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p class="loading-text" role="status">Loading the emails…</p>
    } @else if (loadFailed()) {
      <p class="error-text" role="alert">Couldn't load the emails. Reload the page to try again.</p>
    } @else {
      <p class="mb-5 max-w-[72ch] text-sm leading-normal text-(--ink-secondary)">
        All HR users share these emails. A saved change applies to the next email sent; emails already sent stay as they
        were.
      </p>
      <div class="flex flex-col gap-8">
        @for (template of templates(); track template.id) {
          <app-email-template-card [template]="template" />
        }
      </div>
    }
  `,
})
export class EmailTemplateEditorComponent implements OnInit {
  private readonly emailTemplates = inject(EmailTemplatesService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly category = input.required<EmailTemplateCategory>();

  public readonly loading = signal(true);
  public readonly loadFailed = signal(false);
  public readonly templates = signal<EmailTemplateSettings[]>([]);

  ngOnInit(): void {
    this.emailTemplates
      .getByCategory(this.category())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (templates) => {
          this.templates.set(templates);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.loadFailed.set(true);
          this.toastService.error(firstApiErrorMessage(err, 'Failed to load the emails.'));
        },
      });
  }
}
