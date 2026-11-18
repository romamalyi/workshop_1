import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, EMPTY, finalize, forkJoin, Observable, of, Subject, switchMap, take, tap } from 'rxjs';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import {
  OnboardingTypeDetail,
  SaveTypeRequest,
  TypeLibraryTemplate,
  TypeTemplateSelection,
} from '../../models/onboarding-type.model';
import { OnboardingPhase } from '../../onboarding-template.enums';
import { PHASE_HINTS, PHASE_LABELS } from '../../onboarding-template.model';
import {
  appliesBadge,
  appliesTooltip,
  formatFieldSummary,
  formatShortDueRule,
  pluralize,
} from '../../onboarding-template.util';
import { OnboardingTypesService } from '../../services/onboarding-types.service';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error';

const NAME_MAX_LENGTH = 100;
const DESCRIPTION_MAX_LENGTH = 200;

interface TemplateRow {
  template: TypeLibraryTemplate;
  isEnabled: boolean;
  isDeactivated: boolean;
}

interface PhaseGroup {
  phase: OnboardingPhase;
  label: string;
  hint: string;
  rows: TemplateRow[];
  activeCount: number;
  enabledCount: number;
}

type PendingRemoval = { kind: 'phase'; group: PhaseGroup } | { kind: 'all' };

@Component({
  selector: 'app-onboarding-type-page',
  templateUrl: './onboarding-type-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTypePageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly typesService = inject(OnboardingTypesService);
  private readonly destroyRef = inject(DestroyRef);

  readonly appliesBadge = appliesBadge;
  readonly appliesTooltip = appliesTooltip;
  readonly formatFieldSummary = formatFieldSummary;
  readonly formatShortDueRule = formatShortDueRule;

  readonly state = signal<LoadState>('loading');
  readonly saving = signal(false);
  readonly addOpen = signal(false);
  readonly pendingRemoval = signal<PendingRemoval | null>(null);
  readonly leavePrompt = signal<Subject<boolean> | null>(null);

  readonly typeId = signal<number | null>(null);
  readonly isNew = computed(() => this.typeId() === null);

  private readonly library = signal<TypeLibraryTemplate[]>([]);

  readonly name = signal('');
  readonly description = signal('');
  readonly isActive = signal(false);
  private readonly selections = signal<TypeTemplateSelection[]>([]);

  private activeTouched = false;
  private readonly baseline = signal('');
  private readonly nameTouched = signal(false);
  private readonly nameTaken = signal(false);

  private readonly activeLibrary = computed(() => this.library().filter((t) => t.isActive));

  private readonly rows = computed<TemplateRow[]>(() => {
    const libraryOrder = new Map(this.library().map((t, index) => [t.id, index]));
    return this.selections()
      .flatMap((selection) => {
        const template = this.library()[libraryOrder.get(selection.templateId) ?? -1];
        return template ? [{ template, isEnabled: selection.isEnabled, isDeactivated: !template.isActive }] : [];
      })
      .sort((a, b) => libraryOrder.get(a.template.id)! - libraryOrder.get(b.template.id)!);
  });

  readonly groups = computed<PhaseGroup[]>(() =>
    Object.values(OnboardingPhase)
      .map((phase) => {
        const rows = this.rows().filter((row) => row.template.phase === phase);
        const active = rows.filter((row) => !row.isDeactivated);
        return {
          phase,
          label: PHASE_LABELS[phase],
          hint: PHASE_HINTS[phase],
          rows,
          activeCount: active.length,
          enabledCount: active.filter((row) => row.isEnabled).length,
        };
      })
      .filter((group) => group.rows.length > 0),
  );

  private readonly activeRows = computed(() => this.rows().filter((row) => !row.isDeactivated));
  readonly templateCount = computed(() => this.activeRows().length);
  readonly enabledCount = computed(() => this.activeRows().filter((row) => row.isEnabled).length);
  readonly missingCount = computed(() => this.activeLibrary().length - this.templateCount());

  readonly addable = computed(() => {
    const inType = new Set(this.selections().map((s) => s.templateId));
    return this.activeLibrary().filter((t) => !inType.has(t.id));
  });

  readonly summaryLine = computed(
    () =>
      `${this.templateCount()} ${pluralize(this.templateCount(), 'template')} in this type · ${this.enabledCount()} enabled`,
  );

  readonly nameError = computed(() => {
    const name = this.name().trim();
    if (!name) return 'Name is required';
    if (name.length > NAME_MAX_LENGTH) return `Name can be at most ${NAME_MAX_LENGTH} characters`;
    return this.nameTaken() ? 'A type with this name already exists' : null;
  });

  readonly nameRequired = computed(() => !this.name().trim());

  // "Too long" shows while typing; other name errors wait until the field is touched.
  readonly nameErrorShown = computed(
    () => (this.nameTouched() || this.name().trim().length > NAME_MAX_LENGTH) && !!this.nameError(),
  );

  readonly descriptionError = computed(() =>
    this.description().trim().length > DESCRIPTION_MAX_LENGTH
      ? `Description can be at most ${DESCRIPTION_MAX_LENGTH} characters`
      : null,
  );

  readonly descriptionErrorShown = computed(() => !!this.descriptionError());

  readonly activeHelper = computed(() => {
    if (this.enabledCount() === 0) return 'Enable at least one template to save the type';
    return this.isActive()
      ? 'Offered in the New Flow wizard.'
      : 'Archived — hidden from the wizard, existing flows keep running.';
  });

  readonly isDirty = computed(() => this.snapshot() !== this.baseline());
  readonly canSave = computed(
    () =>
      !this.saving() &&
      this.enabledCount() > 0 &&
      !this.nameError() &&
      !this.descriptionError() &&
      (this.isNew() || this.isDirty()),
  );
  // An archived type is not offered for duplication.
  readonly showDuplicate = computed(() => this.isActive());
  readonly canDuplicate = computed(() => !this.isNew() && !this.isDirty() && !this.saving());

  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) => this.load(params.get('id'))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  setName(name: string): void {
    this.name.set(name);
    this.nameTaken.set(false);
  }

  touchName(): void {
    this.nameTouched.set(true);
  }

  toggleTemplate(templateId: number): void {
    this.selections.update((list) =>
      list.map((s) => (s.templateId === templateId ? { ...s, isEnabled: !s.isEnabled } : s)),
    );
    this.syncActive();
  }

  removeTemplate(templateId: number): void {
    this.selections.update((list) => list.filter((s) => s.templateId !== templateId));
    this.syncActive();
  }

  addTemplates(templateIds: number[]): void {
    this.selections.update((list) => [...list, ...templateIds.map((templateId) => ({ templateId, isEnabled: true }))]);
    this.addOpen.set(false);
    this.syncActive();
  }

  confirmRemoval(): void {
    const removal = this.pendingRemoval();
    this.pendingRemoval.set(null);
    if (!removal) return;

    const removed = new Set((removal.kind === 'all' ? this.rows() : removal.group.rows).map((row) => row.template.id));
    this.selections.update((list) => list.filter((s) => !removed.has(s.templateId)));
    this.syncActive();
  }

  toggleActive(): void {
    if (this.enabledCount() === 0) return;
    this.activeTouched = true;
    this.isActive.update((active) => !active);
  }

  save(): void {
    if (!this.canSave()) return;

    const id = this.typeId();
    const request: SaveTypeRequest = {
      name: this.name().trim(),
      description: this.description().trim() || null,
      isArchived: !this.isActive(),
      templates: this.selections(),
    };

    this.saving.set(true);
    (id === null ? this.typesService.create(request) : this.typesService.update(id, request))
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (saved) => {
          this.baseline.set(this.snapshot());
          this.toast.success('Type saved');
          if (id === null) this.router.navigate(['/onboarding/types', saved.id], { replaceUrl: true });
        },
        error: (err: unknown) => {
          if (err instanceof HttpErrorResponse && err.status === 409) {
            this.nameTaken.set(true);
            this.nameTouched.set(true);
            return;
          }
          this.toast.error(firstApiErrorMessage(err, 'Could not save the type.'));
        },
      });
  }

  duplicate(): void {
    const id = this.typeId();
    if (id === null || !this.canDuplicate()) return;

    this.typesService
      .duplicate(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (copy) => {
          this.toast.success(`“${copy.name}” created`);
          this.router.navigate(['/onboarding/types', copy.id]);
        },
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Could not duplicate the type.')),
      });
  }

  openTemplateLibrary(): void {
    this.router.navigate(['/onboarding/templates']);
  }

  canLeave(): Observable<boolean> {
    if (!this.isDirty()) return of(true);

    const answer = new Subject<boolean>();
    this.leavePrompt.set(answer);
    return answer.pipe(take(1));
  }

  answerLeavePrompt(leave: boolean): void {
    this.leavePrompt()?.next(leave);
    this.leavePrompt.set(null);
  }

  @HostListener('window:beforeunload', ['$event'])
  warnBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.isDirty()) event.preventDefault();
  }

  private load(idParam: string | null): Observable<unknown> {
    const id = idParam === null ? null : Number(idParam);
    if (id !== null && !Number.isInteger(id)) {
      this.state.set('notFound');
      return EMPTY;
    }

    this.state.set('loading');
    return forkJoin({
      library: this.typesService.getLibrary(),
      type: id === null ? of(null) : this.typesService.getById(id),
    }).pipe(
      tap(({ library, type }) => {
        this.library.set(library);
        this.typeId.set(id);
        this.hydrate(type);
        this.state.set('ready');
      }),
      catchError((err: unknown) => {
        this.state.set(err instanceof HttpErrorResponse && err.status === 404 ? 'notFound' : 'error');
        return EMPTY;
      }),
    );
  }

  private hydrate(type: OnboardingTypeDetail | null): void {
    this.activeTouched = false;
    this.nameTouched.set(false);
    this.nameTaken.set(false);
    this.name.set(type?.name ?? '');
    this.description.set(type?.description ?? '');
    this.isActive.set(type ? !type.isArchived : false);
    this.selections.set(type?.templates ?? []);
    this.baseline.set(this.snapshot());
  }

  private syncActive(): void {
    if (this.enabledCount() === 0) this.isActive.set(false);
    else if (this.isNew() && !this.activeTouched) this.isActive.set(true);
  }

  private snapshot(): string {
    return JSON.stringify({
      name: this.name().trim(),
      description: this.description().trim(),
      isActive: this.isActive(),
      selections: [...this.selections()].sort((a, b) => a.templateId - b.templateId),
    });
  }
}
