import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  Injector,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { Seniority } from '../../../promotion-criteria/promotion-criteria.enums';
import { SENIORITY_LABELS } from '../../../promotion-criteria/promotion-criteria.model';
import { PromotionCriteriaService } from '../../../promotion-criteria/services/promotion-criteria.service';
import {
  DueAnchor,
  DueDirection,
  OnboardingPhase,
  TemplateAppliesTo,
  TemplateFieldType,
} from '../../onboarding-template.enums';
import {
  APPLIES_TO_LABELS,
  DUE_ANCHOR_LABELS,
  FIELD_TYPE_LABELS,
  MilestoneOption,
  OnboardingTemplate,
  PHASE_LABELS,
  SaveTemplateRequest,
} from '../../onboarding-template.model';
import { formatDueRule } from '../../onboarding-template.util';
import { ResponsibleRoleOption } from '../../models/responsible-roles.model';
import { OnboardingSettingsService } from '../../services/onboarding-settings.service';
import { OnboardingFlowStatus } from '../../models/onboarding-flow.enums';
import { OnboardingFlowsService } from '../../services/onboarding-flows.service';
import { OnboardingTemplatesService } from '../../services/onboarding-templates.service';

interface DraftChecklistItem {
  uid: number;
  id?: number;
  text: string;
}

interface DraftField {
  uid: number;
  id?: number;
  fieldType: TemplateFieldType;
  label: string;
  hint: string;
  personRestrictionKey: string | null;
  checklistItems: DraftChecklistItem[];
}

const MAX_DUE_OFFSET_DAYS = 365;
const MAX_TITLE_LENGTH = 200;
const SENIORITY_ORDER = Object.values(Seniority);

let nextUid = 1;

@Component({
  selector: 'app-onboarding-template-editor',
  templateUrl: './onboarding-template-editor.component.html',
  styleUrl: './onboarding-template-editor.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTemplateEditorComponent implements OnInit {
  private readonly criteriaService = inject(PromotionCriteriaService);
  private readonly settingsService = inject(OnboardingSettingsService);
  private readonly templatesService = inject(OnboardingTemplatesService);
  private readonly flowsService = inject(OnboardingFlowsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** The template to edit; null or id 0 means it hasn't been saved yet. */
  readonly template = input<OnboardingTemplate | null>(null);
  readonly saving = input(false);
  // Null until loaded, and stays null if the flows can't be read.
  readonly runningFlowCount = signal<number | null>(null);

  readonly layout = input<'modal' | 'page'>('modal');

  readonly save = output<SaveTemplateRequest>();
  readonly closed = output<void>();
  readonly duplicate = output<void>();

  readonly FieldType = TemplateFieldType;
  readonly phases = Object.values(OnboardingPhase);
  readonly directions = Object.values(DueDirection);
  readonly anchors = Object.values(DueAnchor);
  readonly appliesToOptions = Object.values(TemplateAppliesTo);
  readonly fieldTypes = Object.values(TemplateFieldType);
  readonly maxDueOffsetDays = MAX_DUE_OFFSET_DAYS;
  readonly phaseLabels = PHASE_LABELS;
  readonly anchorLabels = DUE_ANCHOR_LABELS;
  readonly appliesToLabels = APPLIES_TO_LABELS;
  readonly levelLabels = SENIORITY_LABELS;
  readonly fieldTypeLabels = FIELD_TYPE_LABELS;

  readonly title = signal('');
  readonly phase = signal(OnboardingPhase.BeforeFirstDay);
  readonly milestoneId = signal<number | null>(null);
  readonly milestones = signal<MilestoneOption[]>([]);
  readonly responsibleRoleKey = signal<string | null>(null);
  readonly description = signal('');
  readonly dueOffsetDays = signal(1);
  readonly dueDirection = signal(DueDirection.Before);
  readonly dueAnchor = signal(DueAnchor.StartDate);
  readonly appliesTo = signal(TemplateAppliesTo.AllHires);
  private readonly suggestedFor = signal<Seniority[]>([]);
  readonly fields = signal<DraftField[]>([]);

  readonly pendingFieldRemoval = signal<DraftField | null>(null);
  readonly showUnsavedConfirm = signal(false);

  readonly responsibleRoles = signal<ResponsibleRoleOption[]>([]);
  readonly personRestrictions = signal<ResponsibleRoleOption[]>([]);
  private readonly activeLevels = signal<Seniority[]>([]);

  private baseline = '';

  readonly pageSubtitle = computed(() =>
    [this.phaseLabels[this.phase()], this.responsibleRoleName(), this.appliesToLabels[this.appliesTo()]]
      .filter(Boolean)
      .join(' · '),
  );

  readonly responsibleRoleName = computed(
    () => this.responsibleRoles().find((role) => role.key === this.responsibleRoleKey())?.name ?? null,
  );

  readonly isOptional = computed(() => this.appliesTo() === TemplateAppliesTo.Optional);

  readonly levelOptions = computed(() => {
    const available = new Set([...this.activeLevels(), ...this.suggestedFor()]);
    return SENIORITY_ORDER.filter((level) => available.has(level));
  });

  private readonly offsetInvalid = computed(() => {
    const offset = this.dueOffsetDays();
    return !Number.isInteger(offset) || offset < 0 || offset > MAX_DUE_OFFSET_DAYS;
  });

  readonly isOnAnchorDay = computed(() => this.dueOffsetDays() === 0);

  readonly duePreview = computed(() =>
    this.offsetInvalid() ? '' : formatDueRule(this.dueOffsetDays(), this.dueDirection(), this.dueAnchor()),
  );

  private readonly suggestedLevelMissing = computed(() => this.isOptional() && this.suggestedFor().length === 0);

  private readonly emptyChecklistLabel = computed(
    () =>
      this.fields().find((f) => f.fieldType === TemplateFieldType.Checklist && this.nonEmptyItems(f).length === 0)
        ?.label ?? null,
  );

  private readonly unlabelledField = computed(() => this.fields().some((f) => !f.label.trim()));

  readonly validationHint = computed(() => {
    if (!this.title().trim()) return 'A title is required.';
    if (this.milestoneId() === null) return 'A milestone is required.';
    if (this.title().trim().length > MAX_TITLE_LENGTH)
      return `The title can be at most ${MAX_TITLE_LENGTH} characters.`;
    if (this.offsetInvalid()) return `The offset must be a whole number from 0 to ${MAX_DUE_OFFSET_DAYS}.`;
    if (this.suggestedLevelMissing()) return 'Pick at least one suggested level.';
    if (this.unlabelledField()) return 'Every field needs a label.';
    const emptyChecklist = this.emptyChecklistLabel();
    if (emptyChecklist !== null) return `Checklist "${emptyChecklist}" needs at least one item.`;
    return null;
  });

  readonly canSave = computed(() => this.validationHint() === null && !this.saving());

  readonly isDirty = computed(() => this.snapshot() !== this.baseline);

  constructor() {
    this.criteriaService
      .getAll()
      .pipe(
        map((criteria) => {
          const seniorities = new Set<string>(criteria.map((c) => c.seniority));
          return SENIORITY_ORDER.filter((level) => seniorities.has(level));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((levels) => this.activeLevels.set(levels));

    this.settingsService
      .getTemplateRoleOptions()
      .pipe(takeUntilDestroyed())
      .subscribe((roles) => this.responsibleRoles.set(roles));

    this.templatesService
      .getMilestones()
      .pipe(takeUntilDestroyed())
      .subscribe((options) => this.milestones.set(options));

    this.templatesService
      .getPersonRestrictionOptions()
      .pipe(takeUntilDestroyed())
      .subscribe((options) => this.personRestrictions.set(options));
  }

  ngOnInit(): void {
    this.hydrate(this.template());

    if (this.layout() === 'page') {
      this.flowsService
        .getAll()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (flows) =>
            this.runningFlowCount.set(flows.filter((f) => f.status !== OnboardingFlowStatus.Completed).length),
          error: () => this.runningFlowCount.set(null),
        });
    }
  }

  hintPlaceholder(type: TemplateFieldType): string {
    return type === TemplateFieldType.Note ? 'Placeholder (optional)' : 'Hint (optional)';
  }

  selectAppliesTo(value: TemplateAppliesTo): void {
    this.appliesTo.set(value);
    if (value === TemplateAppliesTo.AllHires) {
      this.suggestedFor.set([]);
    }
  }

  toggleLevel(level: Seniority): void {
    this.suggestedFor.update((levels) =>
      levels.includes(level) ? levels.filter((l) => l !== level) : [...levels, level],
    );
  }

  isLevelSelected(level: Seniority): boolean {
    return this.suggestedFor().includes(level);
  }

  addField(type: TemplateFieldType): void {
    const field: DraftField = {
      uid: nextUid++,
      fieldType: type,
      label: '',
      hint: '',
      personRestrictionKey: null,
      checklistItems: type === TemplateFieldType.Checklist ? [{ uid: nextUid++, text: '' }] : [],
    };
    this.fields.update((fields) => [...fields, field]);
  }

  requestRemoveField(field: DraftField): void {
    if (this.isFieldConfigured(field)) {
      this.pendingFieldRemoval.set(field);
      return;
    }
    this.removeField(field);
  }

  confirmRemoveField(): void {
    const field = this.pendingFieldRemoval();
    if (field) this.removeField(field);
    this.pendingFieldRemoval.set(null);
  }

  moveField(field: DraftField, delta: number): void {
    this.fields.update((fields) => {
      const index = fields.indexOf(field);
      const target = index + delta;
      if (index < 0 || target < 0 || target >= fields.length) return fields;
      const next = [...fields];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  }

  updateFieldLabel(field: DraftField, label: string): void {
    this.patchField(field, { label });
  }

  updateFieldHint(field: DraftField, hint: string): void {
    this.patchField(field, { hint });
  }

  updateFieldRestriction(field: DraftField, personRestrictionKey: string | null): void {
    this.patchField(field, { personRestrictionKey });
  }

  addChecklistItem(field: DraftField): void {
    this.patchField(field, { checklistItems: [...field.checklistItems, { uid: nextUid++, text: '' }] });
  }

  addChecklistItemAfter(field: DraftField, item: DraftChecklistItem, event: Event): void {
    // A composing IME uses Enter to confirm the text, not to end the item.
    if ((event as KeyboardEvent).isComposing) return;
    event.preventDefault();
    // Empty items are dropped on save, so Enter on one would only stack blank rows.
    if (item.text.trim().length === 0) return;
    const added: DraftChecklistItem = { uid: nextUid++, text: '' };
    const index = field.checklistItems.indexOf(item) + 1;
    this.patchField(field, {
      checklistItems: [...field.checklistItems.slice(0, index), added, ...field.checklistItems.slice(index)],
    });
    afterNextRender(() => document.getElementById(`ote-item-${added.uid}`)?.focus(), { injector: this.injector });
  }

  updateChecklistItem(field: DraftField, item: DraftChecklistItem, text: string): void {
    this.patchField(field, {
      checklistItems: field.checklistItems.map((i) => (i.uid === item.uid ? { ...i, text } : i)),
    });
  }

  removeChecklistItem(field: DraftField, item: DraftChecklistItem): void {
    this.patchField(field, { checklistItems: field.checklistItems.filter((i) => i.uid !== item.uid) });
  }

  nonEmptyItems(field: DraftField): DraftChecklistItem[] {
    return field.checklistItems.filter((i) => i.text.trim().length > 0);
  }

  requestClose(): void {
    if (this.saving()) return;
    if (this.isDirty()) {
      this.showUnsavedConfirm.set(true);
      return;
    }
    this.closed.emit();
  }

  confirmDiscard(): void {
    this.showUnsavedConfirm.set(false);
    this.closed.emit();
  }

  submit(): void {
    if (!this.canSave()) return;

    this.save.emit({
      title: this.title().trim(),
      phase: this.phase(),
      milestoneId: this.milestoneId()!,
      responsibleRoleKey: this.responsibleRoleKey(),
      description: this.description().trim() || null,
      dueOffsetDays: this.dueOffsetDays(),
      dueDirection: this.dueDirection(),
      dueAnchor: this.dueAnchor(),
      appliesTo: this.appliesTo(),
      suggestedFor: this.isOptional() ? this.suggestedFor() : [],
      fields: this.fields().map((field) => ({
        id: field.id,
        fieldType: field.fieldType,
        label: field.label.trim(),
        hint: field.hint.trim() || null,
        personRestrictionKey: field.fieldType === TemplateFieldType.EmployeePicker ? field.personRestrictionKey : null,
        checklistItems:
          field.fieldType === TemplateFieldType.Checklist
            ? this.nonEmptyItems(field).map((i) => ({ id: i.id, text: i.text.trim() }))
            : [],
      })),
    });
  }

  private removeField(field: DraftField): void {
    this.fields.update((fields) => fields.filter((f) => f.uid !== field.uid));
  }

  private isFieldConfigured(field: DraftField): boolean {
    return field.label.trim().length > 0 || field.hint.trim().length > 0 || this.nonEmptyItems(field).length > 0;
  }

  private patchField(field: DraftField, patch: Partial<DraftField>): void {
    this.fields.update((fields) => fields.map((f) => (f.uid === field.uid ? { ...f, ...patch } : f)));
  }

  private hydrate(template: OnboardingTemplate | null): void {
    this.title.set(template?.title ?? '');
    this.phase.set(template?.phase ?? OnboardingPhase.BeforeFirstDay);
    this.milestoneId.set(template?.milestoneId ?? null);
    this.responsibleRoleKey.set(template?.responsibleRoleKey ?? null);
    this.description.set(template?.description ?? '');
    this.dueOffsetDays.set(template?.dueOffsetDays ?? 1);
    this.dueDirection.set(template?.dueDirection ?? DueDirection.Before);
    this.dueAnchor.set(template?.dueAnchor ?? DueAnchor.StartDate);
    this.appliesTo.set(template?.appliesTo ?? TemplateAppliesTo.AllHires);
    this.suggestedFor.set([...(template?.suggestedFor ?? [])]);
    this.fields.set(
      (template?.fields ?? []).map((field) => ({
        uid: nextUid++,
        id: field.id,
        fieldType: field.fieldType,
        label: field.label,
        hint: field.hint ?? '',
        personRestrictionKey: field.personRestrictionKey,
        checklistItems: field.checklistItems.map((item) => ({ uid: nextUid++, id: item.id, text: item.text })),
      })),
    );

    this.baseline = this.snapshot();
  }

  private snapshot(): string {
    return JSON.stringify({
      title: this.title().trim(),
      phase: this.phase(),
      milestoneId: this.milestoneId(),
      responsibleRoleKey: this.responsibleRoleKey(),
      description: this.description().trim(),
      dueOffsetDays: this.dueOffsetDays(),
      dueDirection: this.dueDirection(),
      dueAnchor: this.dueAnchor(),
      appliesTo: this.appliesTo(),
      suggestedFor: [...this.suggestedFor()].sort(),
      fields: this.fields().map((f) => ({
        fieldType: f.fieldType,
        label: f.label.trim(),
        hint: f.hint.trim(),
        personRestrictionKey: f.personRestrictionKey,
        items: f.checklistItems.map((i) => i.text.trim()),
      })),
    });
  }
}
