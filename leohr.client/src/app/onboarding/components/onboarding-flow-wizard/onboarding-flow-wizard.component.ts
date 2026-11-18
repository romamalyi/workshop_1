import { formatDate } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';
import { Router } from '@angular/router';
import { Seniority } from '../../../promotion-criteria/promotion-criteria.enums';
import { SENIORITY_LABELS } from '../../../promotion-criteria/promotion-criteria.model';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingPhase, TemplateAppliesTo, DueAnchor, DueDirection } from '../../onboarding-template.enums';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { EmploymentType } from '../../models/onboarding-flow.enums';
import { CreateOnboardingFlowRequest } from '../../models/onboarding-flow.model';
import { OnboardingType, TypeLibraryTemplate } from '../../models/onboarding-type.model';
import { OnboardingPhases } from '../../models/onboarding-settings.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';
import { ResponsibleCandidate } from '../../models/responsible-roles.model';
import { unassignedLabel } from '../../onboarding-flow.util';
import { OnboardingAttentionService } from '../../services/onboarding-attention.service';
import { OnboardingFlowsService } from '../../services/onboarding-flows.service';
import { OnboardingTypesService } from '../../services/onboarding-types.service';
import { toSearchOptions } from '../../../shared/models/search-option.model';

type FormField = 'name' | 'type' | 'responsible' | 'startDate' | 'tasks';

interface PhaseCount {
  phase: OnboardingPhase;
  label: string;
  count: number;
  dates: string | null;
}

const START_DATE_WINDOW_YEARS = 1;
const MS_PER_DAY = 24 * 60 * 60 * 1000;
const PHASE_DATE_FORMAT = 'EEE d MMM';

// Clamped to the month's last day, matching DateOnly.AddMonths on the backend.
function addMonths(date: Date, months: number): Date {
  const year = date.getUTCFullYear();
  const month = date.getUTCMonth() + months;
  const lastDayOfMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  return new Date(Date.UTC(year, month, Math.min(date.getUTCDate(), lastDayOfMonth)));
}

// Mirrors ComputeDueDate in OnboardingFlowsService: Saturday moves back a day, Sunday forward.
function dueTime(template: TypeLibraryTemplate, start: Date, probationMonths: number): number {
  const anchor = template.dueAnchor === DueAnchor.EndOfProbation ? addMonths(start, probationMonths) : start;
  const offset = template.dueDirection === DueDirection.Before ? -template.dueOffsetDays : template.dueOffsetDays;
  const due = new Date(anchor.getTime() + offset * MS_PER_DAY);
  const weekday = due.getUTCDay();
  const weekendShift = weekday === 6 ? -1 : weekday === 0 ? 1 : 0;
  return due.getTime() + weekendShift * MS_PER_DAY;
}

function formatPhaseDate(time: number): string {
  return formatDate(time, PHASE_DATE_FORMAT, 'en-US', 'UTC');
}

// UTC and clamped to the month's last day, matching DateOnly.FromDateTime(DateTime.UtcNow).AddYears on the backend.
function shiftYears(years: number): string {
  const today = new Date();
  const year = today.getUTCFullYear() + years;
  const month = today.getUTCMonth();
  const lastDayOfMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  const day = Math.min(today.getUTCDate(), lastDayOfMonth);
  return new Date(Date.UTC(year, month, day)).toISOString().slice(0, 10);
}

@Component({
  selector: 'app-onboarding-flow-wizard',
  templateUrl: './onboarding-flow-wizard.component.html',
  styleUrl: './onboarding-flow-wizard.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingFlowWizardComponent {
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly flowsService = inject(OnboardingFlowsService);
  private readonly typesService = inject(OnboardingTypesService);
  private readonly attention = inject(OnboardingAttentionService);
  private readonly settingsService = inject(OnboardingSettingsService);

  private readonly phasesResource = httpResource<OnboardingPhases>(() => this.settingsService.phasesEndpoint);
  private readonly probationMonths = computed(() =>
    this.phasesResource.hasValue() ? this.phasesResource.value().probationMonths : null,
  );

  readonly EmploymentType = EmploymentType;
  readonly levelOptions = Object.values(Seniority);
  readonly levelLabels = SENIORITY_LABELS;
  readonly phaseLabels = PHASE_LABELS;

  readonly loadingTemplates = signal(true);
  readonly typesLoading = signal(true);
  readonly typesFailed = signal(false);
  readonly submitting = signal(false);

  private readonly templates = signal<TypeLibraryTemplate[]>([]);
  private readonly types = signal<OnboardingType[]>([]);
  readonly activeTypes = computed(() => this.types().filter((type) => !type.isArchived));
  private readonly responsiblePersonCandidates = signal<ResponsibleCandidate[]>([]);

  readonly fullName = signal('');
  readonly position = signal('');
  readonly employmentType = signal(EmploymentType.FullTime);
  readonly startDate = signal('');
  readonly startDateMin = shiftYears(-START_DATE_WINDOW_YEARS);
  readonly startDateMax = shiftYears(START_DATE_WINDOW_YEARS);
  readonly startDateOutOfRange = computed(() => {
    const value = this.startDate();
    return !!value && (value.length !== 10 || value < this.startDateMin || value > this.startDateMax);
  });
  readonly typeId = signal<number | null>(null);
  readonly selectedType = computed(() => this.activeTypes().find((type) => type.id === this.typeId()) ?? null);
  readonly level = signal<Seniority | null>(null);
  readonly responsiblePersonId = signal<string | null>(null);
  readonly optionalTicked = signal<ReadonlySet<number>>(new Set());
  readonly pendingSkip = signal<TypeLibraryTemplate | null>(null);
  private readonly typeSelections = signal<ReadonlyMap<number, boolean> | null>(null);
  private ticksTouched = false;
  private typeLoad?: Subscription;
  private readonly touched = signal<ReadonlySet<FormField>>(new Set());

  readonly responsibleCandidates = computed(() => {
    const hireName = this.fullName().trim().toLowerCase();
    return this.responsiblePersonCandidates().filter((c) => !hireName || c.fullName.toLowerCase() !== hireName);
  });

  readonly responsibleOptions = computed(() => toSearchOptions(this.responsibleCandidates()));

  readonly selectedResponsible = computed(
    () => this.responsibleCandidates().find((c) => c.id === this.responsiblePersonId()) ?? null,
  );

  private readonly typeTemplates = computed(() => {
    const selections = this.typeSelections();
    return selections ? this.templates().filter((t) => t.isActive && selections.get(t.id) === true) : [];
  });

  private readonly activeAllHires = computed(() =>
    this.typeTemplates().filter((t) => t.appliesTo === TemplateAppliesTo.AllHires),
  );

  readonly activeOptional = computed(() =>
    this.typeTemplates().filter((t) => t.appliesTo === TemplateAppliesTo.Optional),
  );

  readonly suggestedTemplates = computed(() => {
    const level = this.level();
    return level ? this.activeOptional().filter((t) => t.suggestedFor.includes(level)) : [];
  });

  readonly suggestedTitles = computed(() =>
    this.suggestedTemplates()
      .map((t) => t.title)
      .join(', '),
  );

  private readonly suggestedOptionalIds = computed(() => new Set(this.suggestedTemplates().map((t) => t.id)));

  private readonly selectedTemplates = computed(() => {
    const ticked = this.optionalTicked();
    return [...this.activeAllHires(), ...this.activeOptional().filter((t) => ticked.has(t.id))];
  });

  readonly phaseCounts = computed<PhaseCount[]>(() => {
    const generated = this.selectedTemplates();
    return Object.values(OnboardingPhase)
      .map((phase) => {
        const templates = generated.filter((t) => t.phase === phase);
        return {
          phase,
          label: this.phaseLabels[phase],
          count: templates.length,
          dates: this.phaseDates(phase, templates),
        };
      })
      .filter((p) => p.count > 0);
  });

  private phaseDates(phase: OnboardingPhase, templates: TypeLibraryTemplate[]): string | null {
    const startDate = this.startDate();
    if (!startDate || this.startDateOutOfRange()) return null;

    // A yyyy-MM-dd string parses as UTC midnight; all date math below stays in UTC.
    const start = new Date(startDate);
    const probationMonths = this.probationMonths();
    const dueTimes = templates
      .filter((t) => t.dueAnchor !== DueAnchor.EndOfProbation || probationMonths !== null)
      .map((t) => dueTime(t, start, probationMonths ?? 0));
    if (dueTimes.length === 0) return null;

    const first = Math.min(...dueTimes);
    const last = Math.max(...dueTimes);
    if (phase === OnboardingPhase.BeforeFirstDay && last >= Date.now()) return `now → ${formatPhaseDate(last)}`;
    if (first === last) return formatPhaseDate(first);
    const sameMonth = new Date(first).getUTCMonth() === new Date(last).getUTCMonth();
    const from = sameMonth ? formatDate(first, 'EEE d', 'en-US', 'UTC') : formatPhaseDate(first);
    return `${from} → ${formatPhaseDate(last)}`;
  }

  readonly totalTaskCount = computed(() => this.selectedTemplates().length);

  private readonly validationIssues = computed(() => {
    const issues: { field: FormField; message: string }[] = [];
    if (this.typeId() === null) issues.push({ field: 'type', message: 'Onboarding type is required.' });
    if (this.fullName().trim().length === 0) issues.push({ field: 'name', message: 'Full name is required.' });
    if (!this.selectedResponsible()) issues.push({ field: 'responsible', message: 'Responsible person is required.' });
    if (!this.startDate()) issues.push({ field: 'startDate', message: 'Start date is required.' });
    else if (this.startDateOutOfRange())
      issues.push({ field: 'startDate', message: 'Start date must be within one year of today.' });
    if (this.typeSelections() !== null && this.totalTaskCount() === 0)
      issues.push({ field: 'tasks', message: 'Select at least one task' });
    return issues;
  });

  // The REQUIRED badges mark empty fields from the start; a message shows once its field was touched.
  readonly validationMessages = computed(() => {
    const touched = this.touched();
    return this.validationIssues()
      .filter((issue) => touched.has(issue.field))
      .map((issue) => issue.message);
  });

  readonly canSubmit = computed(
    () => this.validationIssues().length === 0 && !this.submitting() && this.typeSelections() !== null,
  );

  constructor() {
    this.typesService
      .getLibrary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (templates) => {
          this.templates.set(templates);
          this.loadingTemplates.set(false);
        },
        error: (err: unknown) => {
          this.loadingTemplates.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not load task templates.'));
        },
      });

    this.typesService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ types }) => {
          this.types.set(types);
          this.typesLoading.set(false);
        },
        error: (err: unknown) => {
          this.typesLoading.set(false);
          this.typesFailed.set(true);
          this.toast.error(firstApiErrorMessage(err, 'Could not load onboarding types.'));
        },
      });

    this.flowsService
      .getResponsibleCandidates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (candidates) => this.responsiblePersonCandidates.set(candidates),
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, 'Could not load responsible people.')),
      });
  }

  touch(field: FormField): void {
    this.touched.update((touched) => (touched.has(field) ? touched : new Set(touched).add(field)));
  }

  selectType(id: number | null): void {
    this.touch('type');
    this.touch('tasks');
    if (id === this.typeId()) return;

    if (this.typeId() !== null && this.ticksTouched) {
      this.toast.warning('Optional tasks were reset for the new type.');
    }
    this.ticksTouched = false;
    this.optionalTicked.set(new Set());
    this.typeId.set(id);
    this.typeSelections.set(null);
    this.typeLoad?.unsubscribe();
    if (id === null) return;

    this.typeLoad = this.typesService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (type) => {
          this.typeSelections.set(new Map(type.templates.map((t) => [t.templateId, t.isEnabled])));
          this.optionalTicked.set(new Set(this.suggestedOptionalIds()));
        },
        error: (err: unknown) => {
          this.typeId.set(null);
          this.toast.error(firstApiErrorMessage(err, 'Could not load the tasks of this type.'));
        },
      });
  }

  selectEmploymentType(value: EmploymentType): void {
    this.employmentType.set(value);
  }

  selectLevel(level: Seniority | null): void {
    this.level.set(level);
    this.ticksTouched = false;
    // Re-apply suggestions for the new level; any manual overrides for the previous level are reset.
    this.optionalTicked.set(new Set(this.suggestedOptionalIds()));
  }

  isSuggested(template: TypeLibraryTemplate): boolean {
    return this.suggestedOptionalIds().has(template.id);
  }

  isTicked(template: TypeLibraryTemplate): boolean {
    return this.optionalTicked().has(template.id);
  }

  showOverrideWarning(template: TypeLibraryTemplate): boolean {
    return this.isSuggested(template) && !this.isTicked(template);
  }

  toggleOptional(template: TypeLibraryTemplate): void {
    if (this.isTicked(template) && this.isSuggested(template)) {
      this.pendingSkip.set(template);
      return;
    }
    this.flipOptional(template);
  }

  confirmSkip(): void {
    const template = this.pendingSkip();
    this.pendingSkip.set(null);
    if (template) this.flipOptional(template);
  }

  keepTask(): void {
    this.pendingSkip.set(null);
  }

  private flipOptional(template: TypeLibraryTemplate): void {
    this.ticksTouched = true;
    this.touch('tasks');
    this.optionalTicked.update((ticked) => {
      const next = new Set(ticked);
      if (next.has(template.id)) {
        next.delete(template.id);
      } else {
        next.add(template.id);
      }
      return next;
    });
  }

  submit(): void {
    if (!this.canSubmit()) return;

    const request: CreateOnboardingFlowRequest = {
      fullName: this.fullName().trim(),
      position: this.position().trim() || null,
      employmentType: this.employmentType(),
      startDate: this.startDate(),
      level: this.level(),
      responsiblePersonId: this.responsiblePersonId()!,
      onboardingTypeId: this.typeId()!,
      optionalTemplateIds: [...this.optionalTicked()],
    };

    this.submitting.set(true);
    this.flowsService
      .create(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (flow) => {
          this.submitting.set(false);
          this.toast.success(`Onboarding flow for ${flow.fullName} created — ${flow.tasks.length} tasks generated`);
          if (flow.unassignedTaskCount > 0) {
            this.toast.warning(`${unassignedLabel(flow.unassignedTaskCount)} — assign an owner in Unassigned tasks`);
            this.attention.refresh().subscribe({ error: () => undefined });
          }
          this.router.navigate(['/onboarding/flows', flow.id]);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not create the onboarding flow.'));
        },
      });
  }

  cancel(): void {
    this.router.navigate(['/onboarding/flows']);
  }
}
