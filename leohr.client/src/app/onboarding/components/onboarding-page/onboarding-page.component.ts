import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ALL_HR_MANAGERS, DefaultHrFilterService } from '../../../employees/services/default-hr-filter.service';
import { SENIORITY_LABELS } from '../../../promotion-criteria/promotion-criteria.model';
import { ToastService } from '../../../shared/services/toast.service';
import { firstApiErrorMessage } from '../../../shared/utils/api-error';
import { FlowAttention, OnboardingFlowStatus } from '../../models/onboarding-flow.enums';
import {
  EMPLOYMENT_TYPE_LABELS,
  FLOW_ATTENTION_CLASSES,
  FLOW_ATTENTION_LABELS,
  FLOW_STATUS_LABELS,
  FLOW_WARNING_TITLES,
  OnboardingFlow,
} from '../../models/onboarding-flow.model';
import {
  avatarColor,
  currentPhase,
  doneTaskCount,
  flowAttention,
  initials,
  progressPercent,
  relativeStart,
  STARTING_SOON_DAYS,
  unassignedLabel,
  withinNextDays,
} from '../../onboarding-flow.util';
import { buildSummaryCards, flowsSubtitle, ITEM_SEPARATOR } from '../../onboarding-summary.util';
import { FlowNavigationState } from '../../models/onboarding-task-detail.model';
import { OnboardingFlowsService } from '../../services/onboarding-flows.service';

const MORE_LIST_MAX_HEIGHT = 320;
const VIEWPORT_MARGIN = 16;
const ALL_STATUSES = 'all';
const ACTIVE_STATUSES = [OnboardingFlowStatus.Created, OnboardingFlowStatus.CreatedUnresolved];
const STATUS_BY_LABEL = new Map(
  Object.entries(FLOW_STATUS_LABELS).map(([status, label]) => [label, status as OnboardingFlowStatus]),
);

// An empty selection is "All"; a cleared one ("nothing checked") matches nothing.
function matchesFilter(selected: string[], cleared: boolean, value: string | null): boolean {
  if (cleared) return false;
  return selected.length === 0 || (value !== null && selected.includes(value));
}

/** The visible area of the nearest ancestor that clips overflow (the scrolling content pane), within the viewport. */
function clippingBounds(el: HTMLElement): { top: number; bottom: number } {
  let top = 0;
  let bottom = window.innerHeight;
  for (let node = el.parentElement; node; node = node.parentElement) {
    if (getComputedStyle(node).overflowY !== 'visible') {
      const rect = node.getBoundingClientRect();
      top = Math.max(top, rect.top);
      bottom = Math.min(bottom, rect.bottom);
    }
  }
  return { top, bottom };
}

@Component({
  selector: 'app-onboarding-page',
  templateUrl: './onboarding-page.component.html',
  styleUrl: './onboarding-page.component.css',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingPageComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly defaultHrFilter = inject(DefaultHrFilterService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly flowsService = inject(OnboardingFlowsService);
  private readonly auth = inject(AuthService);

  readonly Status = OnboardingFlowStatus;
  readonly Attention = FlowAttention;
  readonly attentionLabels = FLOW_ATTENTION_LABELS;
  readonly attentionClasses = FLOW_ATTENTION_CLASSES;
  readonly itemSeparator = ITEM_SEPARATOR;
  readonly statusLabels = FLOW_STATUS_LABELS;
  readonly employmentLabels = EMPLOYMENT_TYPE_LABELS;
  readonly levelLabels = SENIORITY_LABELS;
  readonly avatarColor = avatarColor;
  readonly initials = initials;
  readonly relativeStart = relativeStart;
  readonly currentPhase = currentPhase;
  readonly unassignedLabel = unassignedLabel;
  readonly flowAttention = flowAttention;
  readonly doneTaskCount = doneTaskCount;
  readonly progressPercent = progressPercent;
  readonly startsSoon = (startDate: string) => withinNextDays(startDate, STARTING_SOON_DAYS);

  readonly loading = signal(true);
  readonly flows = signal<OnboardingFlow[]>([]);
  readonly subtitle = computed(() => flowsSubtitle(this.flows()));
  readonly summaryCards = computed(() => buildSummaryCards(this.flows(), this.auth.currentUserId));

  // A missing param is the default (active statuses, the user's own flows); "all" in the URL keeps every option.
  private readonly queryParams = toSignal(this.route.queryParamMap, { requireSync: true });
  private readonly defaultResponsible = signal<string | null>(null);

  readonly statusOptions = Object.values(FLOW_STATUS_LABELS);
  readonly statusCleared = signal(false);
  readonly responsibleCleared = signal(false);

  readonly searchTerm = computed(() => this.queryParams().get('searchTerm') ?? '');
  readonly selectedStatuses = computed(() => {
    const params = this.queryParams();
    const statuses = params.has('status')
      ? params
          .getAll('status')
          .flatMap((status) => (Object.hasOwn(FLOW_STATUS_LABELS, status) ? [status as OnboardingFlowStatus] : []))
      : ACTIVE_STATUSES;
    return statuses.map((status) => FLOW_STATUS_LABELS[status]);
  });
  readonly responsibleOptions = computed(() =>
    [...new Set(this.flows().flatMap((f) => (f.responsiblePersonName ? [f.responsiblePersonName] : [])))].sort((a, b) =>
      a.localeCompare(b),
    ),
  );
  readonly selectedResponsible = computed(() => {
    const params = this.queryParams();
    if (params.has('responsible')) return params.getAll('responsible').filter((name) => name !== ALL_HR_MANAGERS);
    const fallback = this.defaultResponsible();
    return fallback ? [fallback] : [];
  });

  readonly visibleFlows = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    const statuses = this.selectedStatuses();
    const responsible = this.selectedResponsible();
    return this.flows().filter(
      (f) =>
        f.fullName.toLowerCase().includes(query) &&
        matchesFilter(statuses, this.statusCleared(), FLOW_STATUS_LABELS[f.status]) &&
        matchesFilter(responsible, this.responsibleCleared(), f.responsiblePersonName),
    );
  });

  readonly unresolved = computed(() => {
    const flows = this.flows().filter((f) => f.unassignedTaskCount > 0);
    return {
      flowCount: flows.length,
      taskCount: flows.reduce((sum, f) => sum + f.unassignedTaskCount, 0),
    };
  });

  // The server leaves a completed flow's warnings empty, so this counts the same flows the flow page flags.
  readonly needsReview = computed(() => this.flows().filter((f) => f.warnings.length > 0).length);

  readonly warningTooltip = (flow: OnboardingFlow) => flow.warnings.map((w) => FLOW_WARNING_TITLES[w.kind]).join('\n');

  constructor() {
    this.flowsService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (flows) => {
          this.flows.set(flows);
          this.resolveDefaultResponsible();
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.toast.error(firstApiErrorMessage(err, 'Could not load onboarding flows.'));
        },
      });
  }

  onSearchChange(search: string): void {
    this.updateUrl({ searchTerm: search || null });
  }

  // The status default is never empty, so "All" always stays in the URL.
  onStatusChange(labels: string[]): void {
    const statuses = labels.flatMap((label) => STATUS_BY_LABEL.get(label) ?? []);
    this.updateUrl({ status: statuses.length ? statuses : ALL_STATUSES });
  }

  onResponsibleChange(names: string[]): void {
    const keepAll = this.defaultResponsible() !== null;
    this.updateUrl({ responsible: names.length ? names : keepAll ? ALL_HR_MANAGERS : null });
  }

  // Keeps the list hidden until the default is known, so it never shows other HRs' flows first.
  private resolveDefaultResponsible(): void {
    this.defaultHrFilter
      .resolve(of(this.responsibleOptions()))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((name) => {
        this.defaultResponsible.set(name);
        this.loading.set(false);
      });
  }

  private updateUrl(queryParams: Record<string, string | string[] | null>): void {
    this.router.navigate([], { relativeTo: this.route, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
  }

  openFlow(flow: OnboardingFlow): void {
    const state: FlowNavigationState = { flowsQueryParams: this.route.snapshot.queryParams };
    this.router.navigate(['/onboarding/flows', flow.id], { state });
  }

  /** Fits the "+N more" list into the side with more room and rewinds it to the first item on every open. */
  fitMoreList(trigger: HTMLElement): void {
    const rect = trigger.getBoundingClientRect();
    const bounds = clippingBounds(trigger);
    const below = bounds.bottom - rect.bottom - VIEWPORT_MARGIN;
    const above = rect.top - bounds.top - VIEWPORT_MARGIN;
    const openUp = below < MORE_LIST_MAX_HEIGHT && above > below;
    const room = Math.min(MORE_LIST_MAX_HEIGHT, openUp ? above : below);
    trigger.classList.toggle('ob-more--up', openUp);
    trigger.style.setProperty('--ob-more-max-height', `${room}px`);
    trigger.querySelector('.ob-more-list')?.scrollTo({ top: 0 });
  }
}
