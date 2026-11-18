import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { SENIORITY_LABELS } from '../../../promotion-criteria/promotion-criteria.model';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { ToastService } from '../../../shared/services/toast.service';
import { AffectedType } from '../../models/onboarding-type.model';
import { OnboardingPhase, TemplateAppliesTo } from '../../onboarding-template.enums';
import {
  APPLIES_TO_LABELS,
  OnboardingTemplate,
  PHASE_HINTS,
  PHASE_LABELS,
  SaveTemplateRequest,
} from '../../onboarding-template.model';
import { formatFieldSummary, formatTemplateDueRule } from '../../onboarding-template.util';
import { OnboardingTemplatesService } from '../../services/onboarding-templates.service';

interface TemplateSection {
  key: string;
  title: string;
  hint: string;
  templates: OnboardingTemplate[];
}

// A deactivation or deletion waiting for the user's confirmation; `types` are the types it would archive.
interface PendingChange {
  kind: 'deactivate' | 'delete';
  template: OnboardingTemplate;
  types: AffectedType[];
}

@Component({
  selector: 'app-onboarding-templates-page',
  templateUrl: './onboarding-templates-page.component.html',
  styleUrl: './onboarding-templates-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTemplatesPageComponent {
  private readonly toast = inject(ToastService);
  private readonly datePipe = inject(DatePipe);
  private readonly templatesService = inject(OnboardingTemplatesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  // Set when the template page asks for its copy; the router state is only readable during construction.
  private readonly duplicateTemplateId: number | undefined =
    this.router.currentNavigation()?.extras.state?.['duplicateTemplateId'];

  private readonly templates = signal<OnboardingTemplate[]>([]);

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly saving = signal(false);
  readonly editorOpen = signal(false);
  readonly editorTemplate = signal<OnboardingTemplate | null>(null);
  readonly pendingChange = signal<PendingChange | null>(null);
  readonly busyIds = signal<ReadonlySet<number>>(new Set());

  private readonly activeTemplates = computed(() => this.templates().filter((t) => t.isActive));

  private readonly inactiveTemplates = computed(() => this.templates().filter((t) => !t.isActive));

  private readonly phaseSections = computed<TemplateSection[]>(() => {
    const active = this.activeTemplates();
    return Object.values(OnboardingPhase).map((phase) => ({
      key: phase,
      title: PHASE_LABELS[phase],
      hint: PHASE_HINTS[phase],
      templates: active.filter((t) => t.phase === phase),
    }));
  });

  readonly sections = computed<TemplateSection[]>(() => {
    const inactive = this.inactiveTemplates();
    if (inactive.length === 0) return this.phaseSections();
    return [
      ...this.phaseSections(),
      { key: 'inactive', title: 'Inactive', hint: 'Not used when generating new flows', templates: inactive },
    ];
  });

  readonly totalCount = computed(() => this.templates().length);

  readonly activeCount = computed(() => this.activeTemplates().length);

  readonly phaseCount = computed(() => this.phaseSections().filter((s) => s.templates.length > 0).length);

  readonly everyHireTaskCount = computed(
    () => this.activeTemplates().filter((t) => t.appliesTo === TemplateAppliesTo.AllHires).length,
  );

  private readonly lastEdited = computed(() =>
    this.templates()
      .filter((t) => t.updatedAt)
      .sort((a, b) => (a.updatedAt! < b.updatedAt! ? 1 : -1))
      .at(0),
  );

  readonly lastEditedAtLabel = computed(() => this.datePipe.transform(this.lastEdited()?.updatedAt, 'd MMM y'));

  readonly lastEditedBy = computed(() => this.lastEdited()?.updatedByName ?? null);

  readonly isEmptyLibrary = computed(() => !this.loading() && this.totalCount() === 0);

  constructor() {
    this.load();
  }

  phaseLabel(phase: OnboardingPhase): string {
    return PHASE_LABELS[phase];
  }

  formatDueRule(template: OnboardingTemplate): string {
    return formatTemplateDueRule(template);
  }

  formatFieldSummary(template: OnboardingTemplate): string {
    return formatFieldSummary(
      template.fields.map((field) => ({ fieldType: field.fieldType, checklistItemCount: field.checklistItems.length })),
    );
  }

  isOptional(template: OnboardingTemplate): boolean {
    return template.appliesTo === TemplateAppliesTo.Optional;
  }

  chipLabel(template: OnboardingTemplate): string {
    const appliesTo = APPLIES_TO_LABELS[template.appliesTo];
    if (!this.isOptional(template) || template.suggestedFor.length === 0) {
      return appliesTo;
    }
    const levels = template.suggestedFor.map((level) => SENIORITY_LABELS[level]).join(', ');
    return `${appliesTo} · ${levels}`;
  }

  newTemplate(): void {
    this.editorTemplate.set(null);
    this.editorOpen.set(true);
  }

  openTemplate(template: OnboardingTemplate): void {
    this.router.navigate(['/onboarding/templates', template.id]);
  }

  duplicateTemplate(template: OnboardingTemplate): void {
    this.editorTemplate.set({ ...template, id: 0, title: `${template.title} (copy)` });
    this.editorOpen.set(true);
  }

  closeEditor(): void {
    this.editorOpen.set(false);
    this.editorTemplate.set(null);
  }

  saveTemplate(request: SaveTemplateRequest): void {
    this.saving.set(true);
    this.templatesService
      .create(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.toast.success('Template created');
          this.upsertTemplate(saved);
          this.closeEditor();
        },
        error: (err: unknown) => {
          this.saving.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not save the template.'));
        },
      });
  }

  toggleActive(template: OnboardingTemplate): void {
    if (this.busyIds().has(template.id)) {
      return;
    }
    if (!template.isActive) {
      this.changeStatus(template, 0);
      return;
    }
    this.checkArchiveImpact(template, (types) => {
      if (types.length === 0) {
        this.changeStatus(template, 0);
      } else {
        this.pendingChange.set({ kind: 'deactivate', template, types });
      }
    });
  }

  requestDelete(template: OnboardingTemplate): void {
    if (this.busyIds().has(template.id)) {
      return;
    }
    this.checkArchiveImpact(template, (types) => this.pendingChange.set({ kind: 'delete', template, types }));
  }

  confirmChange(change: PendingChange): void {
    this.pendingChange.set(null);
    if (this.busyIds().has(change.template.id)) {
      return;
    }
    if (change.kind === 'delete') {
      this.deleteTemplate(change.template, change.types.length);
    } else {
      this.changeStatus(change.template, change.types.length);
    }
  }

  changeMessage(change: PendingChange): string {
    const title = `“${change.template.title}”`;
    const deleting = change.kind === 'delete';
    if (change.types.length === 0) {
      return `${title} and its fields will be deleted. This can’t be undone.`;
    }
    const single = change.types.length === 1;
    const typeNames = `${single ? 'onboarding type' : 'onboarding types'} ${change.types.map((t) => `“${t.name}”`).join(', ')}`;
    return `${title} is the last enabled template in the ${typeNames}. ${deleting ? 'Deleting' : 'Deactivating'} it will archive ${single ? 'it' : 'them'}.`;
  }

  private checkArchiveImpact(template: OnboardingTemplate, onChecked: (types: AffectedType[]) => void): void {
    this.setBusy(template.id, true);
    this.templatesService
      .getArchiveImpact(template.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (types) => {
          this.setBusy(template.id, false);
          onChecked(types);
        },
        error: (err: unknown) => {
          this.setBusy(template.id, false);
          this.toast.error(firstApiErrorMessage(err, 'Could not check the template.'));
        },
      });
  }

  private changeStatus(template: OnboardingTemplate, archivedTypes: number): void {
    this.setBusy(template.id, true);
    this.templatesService
      .setStatus(template.id, !template.isActive)
      .pipe(
        finalize(() => this.setBusy(template.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.upsertTemplate(updated);
          this.toast.success(
            `${updated.isActive ? 'Template activated' : 'Template deactivated'}${this.archivedNote(archivedTypes)}`,
          );
        },
        error: (err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Could not change the template status.'));
        },
      });
  }

  private deleteTemplate(template: OnboardingTemplate, archivedTypes: number): void {
    this.setBusy(template.id, true);
    this.templatesService
      .delete(template.id)
      .pipe(
        finalize(() => this.setBusy(template.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.templates.update((list) => list.filter((t) => t.id !== template.id));
          this.toast.success(`Template deleted${this.archivedNote(archivedTypes)}`);
        },
        error: (err: unknown) => {
          this.toast.error(firstApiErrorMessage(err, 'Could not delete the template.'));
        },
      });
  }

  private archivedNote(count: number): string {
    return count === 0 ? '' : ` · ${count} onboarding ${count === 1 ? 'type' : 'types'} archived`;
  }

  private setBusy(id: number, busy: boolean): void {
    this.busyIds.update((ids) => {
      const next = new Set(ids);
      if (busy) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  private upsertTemplate(saved: OnboardingTemplate): void {
    this.templates.update((list) =>
      list.some((t) => t.id === saved.id) ? list.map((t) => (t.id === saved.id ? saved : t)) : [...list, saved],
    );
  }

  private openRequestedDuplicate(templates: OnboardingTemplate[]): void {
    const source = templates.find((t) => t.id === this.duplicateTemplateId);
    if (source) this.duplicateTemplate(source);
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.templatesService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (templates) => {
          this.templates.set(templates);
          this.loading.set(false);
          this.openRequestedDuplicate(templates);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.loadFailed.set(true);
          this.toast.error(firstApiErrorMessage(err, 'Could not load task templates.'));
        },
      });
  }
}
