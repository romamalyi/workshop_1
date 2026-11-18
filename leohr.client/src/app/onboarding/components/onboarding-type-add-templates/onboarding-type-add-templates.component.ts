import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TypeLibraryTemplate } from '../../models/onboarding-type.model';
import { PHASE_LABELS } from '../../onboarding-template.model';
import { formatShortDueRule } from '../../onboarding-template.util';

@Component({
  selector: 'app-onboarding-type-add-templates',
  templateUrl: './onboarding-type-add-templates.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTypeAddTemplatesComponent {
  readonly templates = input.required<TypeLibraryTemplate[]>();
  readonly typeName = input.required<string>();

  readonly added = output<number[]>();
  readonly closed = output<void>();

  readonly phaseLabels = PHASE_LABELS;
  readonly formatShortDueRule = formatShortDueRule;

  readonly query = signal('');
  readonly selected = signal<ReadonlySet<number>>(new Set());

  readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    if (!query) return this.templates();
    return this.templates().filter((t) =>
      [t.title, PHASE_LABELS[t.phase], t.responsibleRoleName ?? ''].some((text) => text.toLowerCase().includes(query)),
    );
  });

  readonly allListedSelected = computed(
    () => this.filtered().length > 0 && this.filtered().every((t) => this.selected().has(t.id)),
  );

  toggle(id: number): void {
    this.selected.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  }

  toggleAll(): void {
    if (this.allListedSelected()) {
      this.selected.set(new Set());
      return;
    }
    this.selected.update((ids) => new Set([...ids, ...this.filtered().map((t) => t.id)]));
  }

  add(): void {
    this.added.emit(
      this.templates()
        .filter((t) => this.selected().has(t.id))
        .map((t) => t.id),
    );
  }

  close(): void {
    this.closed.emit();
  }
}
