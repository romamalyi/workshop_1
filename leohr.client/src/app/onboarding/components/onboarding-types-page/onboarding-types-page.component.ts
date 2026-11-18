import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { OnboardingType } from '../../models/onboarding-type.model';
import { OnboardingTypesService } from '../../services/onboarding-types.service';

@Component({
  selector: 'app-onboarding-types-page',
  templateUrl: './onboarding-types-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingTypesPageComponent {
  private readonly toast = inject(ToastService);
  private readonly typesService = inject(OnboardingTypesService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  private readonly allTypes = signal<OnboardingType[]>([]);

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly libraryTemplateCount = signal(0);
  readonly pendingArchive = signal<OnboardingType | null>(null);
  readonly busyIds = signal<ReadonlySet<number>>(new Set());

  // Active types first, then archived; each group newest edit first.
  readonly types = computed(() =>
    [...this.allTypes()].sort(
      (a, b) =>
        Number(a.isArchived) - Number(b.isArchived) || (b.lastEditedAt ?? '').localeCompare(a.lastEditedAt ?? ''),
    ),
  );

  readonly subtitle = computed(() => {
    const total = this.allTypes().length;
    const active = this.allTypes().filter((t) => !t.isArchived).length;
    return `${total} ${total === 1 ? 'type' : 'types'} · ${active} active · every type draws from the same template library`;
  });

  constructor() {
    this.load();
  }

  flowsLabel(count: number): string {
    return `${count} ${count === 1 ? 'flow' : 'flows'}`;
  }

  openEditor(typeId: number | null): void {
    this.router.navigate(['/onboarding/types', typeId ?? 'new']);
  }

  duplicate(type: OnboardingType): void {
    this.run(type.id, this.typesService.duplicate(type.id), 'Could not duplicate the type.', (copy) => {
      this.toast.success(`“${copy.name}” created`);
      this.openEditor(copy.id);
    });
  }

  confirmArchive(type: OnboardingType): void {
    this.pendingArchive.set(null);
    this.setArchived(type, true);
  }

  restore(type: OnboardingType): void {
    this.setArchived(type, false);
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.typesService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.allTypes.set(result.types);
          this.libraryTemplateCount.set(result.libraryTemplateCount);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.loadFailed.set(true);
          this.toast.error(firstApiErrorMessage(err, 'Could not load onboarding types.'));
        },
      });
  }

  private setArchived(type: OnboardingType, isArchived: boolean): void {
    const fallback = isArchived ? 'Could not archive the type.' : 'Could not restore the type.';
    this.run(type.id, this.typesService.setArchived(type.id, isArchived), fallback, (updated) => {
      this.allTypes.update((list) => list.map((t) => (t.id === updated.id ? updated : t)));
      this.toast.success(isArchived ? 'Type archived' : 'Type restored');
    });
  }

  private run(
    id: number,
    call: Observable<OnboardingType>,
    fallbackError: string,
    onSuccess: (type: OnboardingType) => void,
  ): void {
    if (this.busyIds().has(id)) {
      return;
    }
    this.setBusy(id, true);
    call
      .pipe(
        finalize(() => this.setBusy(id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: onSuccess,
        error: (err: unknown) => this.toast.error(firstApiErrorMessage(err, fallbackError)),
      });
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
}
