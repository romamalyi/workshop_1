import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, Observable, of, Subject, take } from 'rxjs';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingTemplate, SaveTemplateRequest } from '../../onboarding-template.model';
import { OnboardingTemplatesService } from '../../services/onboarding-templates.service';
import { OnboardingTemplateEditorComponent } from '../onboarding-template-editor/onboarding-template-editor.component';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error';

@Component({
  selector: 'app-onboarding-template-page',
  templateUrl: './onboarding-template-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTemplatePageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly templatesService = inject(OnboardingTemplatesService);
  private readonly destroyRef = inject(DestroyRef);

  readonly template = signal<OnboardingTemplate | null>(null);
  readonly state = signal<LoadState>('loading');
  readonly saving = signal(false);
  readonly leavePrompt = signal<Subject<boolean> | null>(null);

  private readonly editor = viewChild(OnboardingTemplateEditorComponent);
  private saved = false;

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.state.set('notFound');
      return;
    }

    this.templatesService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (template) => {
          this.template.set(template);
          this.state.set('ready');
        },
        error: (err: unknown) =>
          this.state.set(err instanceof HttpErrorResponse && err.status === 404 ? 'notFound' : 'error'),
      });
  }

  save(request: SaveTemplateRequest): void {
    const template = this.template();
    if (!template) return;

    this.saving.set(true);
    this.templatesService
      .update(template.id, request)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.saved = true;
          this.toast.success('Template updated');
          this.back();
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Could not save the template.')),
      });
  }

  // The list opens its prefilled copy dialog for this template.
  duplicate(): void {
    this.router.navigate(['/onboarding/templates'], { state: { duplicateTemplateId: this.template()?.id } });
  }

  // Asked by the route's canDeactivate guard before leaving the page.
  canLeave(): Observable<boolean> {
    if (this.saved || !this.editor()?.isDirty()) return of(true);

    const answer = new Subject<boolean>();
    this.leavePrompt.set(answer);
    return answer.pipe(take(1));
  }

  answerLeavePrompt(leave: boolean): void {
    this.leavePrompt()?.next(leave);
    this.leavePrompt.set(null);
  }

  back(): void {
    this.router.navigate(['/onboarding/templates']);
  }
}
