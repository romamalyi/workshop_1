import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { BehaviorSubject, Observable, catchError, filter, finalize, map, of, switchMap, tap } from 'rxjs';
import { Activity } from '../../../activities/activity.model';
import { PromotionActivity, PromotionDetail, PromotionsService } from '../../services/promotions.service';
import { AuthService } from '../../../core/services/auth.service';
import { FeatureFlagsService } from '../../../core/services/feature-flags.service';
import { EmailTemplate, EmailTemplatesService } from '../../../core/services/email-templates.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { firstApiErrorMessage } from 'src/app/shared/utils/api-error';
import { PROMOTION_STATUS, PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { KnowledgeEvaluationStatus } from '../../models/knowledge-evaluation.model';
import { AddActivitiesResult, PROMOTION_EMAIL_TEMPLATE_ID } from '../../models/promotion.model';
import { CriterionChange } from '../criteria-panel/criteria-panel.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PendingApprovalsStore } from '../../services/pending-approvals.store';

const ACTIVITY_SCORE_LABEL = 'Activity score required';

const AVATAR_COLORS = ['#A8D5C2', '#F4C4A1', '#B8C9E8', '#F0D4A8', '#C4B8E8', '#E8B8C4', '#B8E8C4', '#D4E8A8'];
// Company IDs are email prefixes, which are never all digits.
const PROMOTION_ID = /^\d+$/;

interface DetailState {
  loading: boolean;
  detail: PromotionDetail | null;
  error: string | null;
}

interface SendModalState {
  promotionId: number;
  subject: string;
  body: string;
}

@Component({
  selector: 'app-promotion-detail',
  templateUrl: './promotion-detail.component.html',
  styleUrls: ['./promotion-detail.component.css'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionDetailComponent {
  private readonly location = inject(Location);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly promotionsService = inject(PromotionsService);
  private readonly emailTemplates = inject(EmailTemplatesService);
  public readonly authService = inject(AuthService);
  private readonly featureFlags = inject(FeatureFlagsService);
  private readonly toastService = inject(ToastService);
  private readonly pendingApprovals = inject(PendingApprovalsStore);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new BehaviorSubject<void>(undefined);
  public readonly PromotionStatus = PromotionStatus;
  public readonly PROMOTION_STATUS = PROMOTION_STATUS;
  public readonly isMenuExpanded = signal<boolean>(false);
  private readonly loadedDetail = signal<PromotionDetail | null>(null);
  readonly lockedCriteriaLabels = computed(() =>
    this.loadedDetail()?.keStatus === KnowledgeEvaluationStatus.Successful ? ['KE required'] : [],
  );
  readonly automaticCriteriaLabels = [ACTIVITY_SCORE_LABEL];
  readonly sendModal = signal<SendModalState | null>(null);
  readonly showCelebration = signal<boolean>(false);
  readonly levelsModalOpen = signal<boolean>(false);
  readonly approving = signal<{ promotionId: number; activity: PromotionActivity } | null>(null);
  readonly editModalOpen = signal<boolean>(false);
  readonly verifying = signal<boolean>(false);
  private readonly verificationRequested = new Set<number>();

  readonly state$: Observable<DetailState> = this.route.paramMap.pipe(
    switchMap((params) => {
      const key = params.get('id') ?? '';
      return this.reload$.pipe(
        switchMap(() =>
          this.loadDetail(key).pipe(
            map((detail) => {
              const labels = new Set(detail.approvedCriteriaLabels ?? []);
              this.checkedCriteriaLabels$.next(labels);
              this.loadedDetail.set(detail);
              this.verifyCriteriaOnce(detail);
              return { loading: false, detail, error: null } as DetailState;
            }),
            catchError(() => of({ loading: false, detail: null, error: 'Failed to load promotion.' } as DetailState)),
          ),
        ),
      );
    }),
  );

  private loadDetail(key: string): Observable<PromotionDetail> {
    if (PROMOTION_ID.test(key)) return this.promotionsService.getDetail(Number(key));
    // Without an open promotion the promotions page shows the no-promotion state.
    return this.promotionsService.getByCompanyId(key).pipe(
      tap((detail) => {
        if (!detail) this.router.navigate(['/promotions'], { replaceUrl: true });
      }),
      filter((detail): detail is PromotionDetail => detail !== null),
    );
  }

  readonly checkedCriteriaLabels$ = new BehaviorSubject<Set<string>>(new Set());

  onCriterionChange({ label, met, comment }: CriterionChange, promotionId: number): void {
    this.promotionsService.setCriterion(promotionId, label, met, comment).subscribe({
      next: () => this.reload$.next(),
      error: (errorResponse) => {
        this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to save the criterion.'));
        this.reload$.next();
      },
    });
  }

  // An employee has no list to return to: /promotions sends them to their current promotion.
  goBack(): void {
    if (this.authService.canReadManage()) this.location.back();
    else this.router.navigate(['/promotions']);
  }

  public navigateToPromotion(id: number) {
    this.router.navigate(['/promotions', id]);
  }

  expandMenu() {
    this.isMenuExpanded.update((val) => !val);
  }

  openSendModal(d: PromotionDetail): void {
    this.emailTemplates
      .get(PROMOTION_EMAIL_TEMPLATE_ID)
      .pipe(
        catchError(() => of(null)),
        map((t: EmailTemplate | null) => ({
          promotionId: d.id,
          subject: t ? this.fillTemplate(t.subject, d) : '',
          body: t ? this.fillTemplate(t.body, d) : '',
        })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((modal) => this.sendModal.set(modal));
  }

  closeSendModal(): void {
    this.sendModal.set(null);
  }

  confirmSend(extraCcEmployeeIds: string[]): void {
    const modal = this.sendModal();
    if (!modal) return;
    this.sendModal.set(null);
    this.promotionsService.sendPromotionEmail(modal.promotionId, extraCcEmployeeIds).subscribe({
      next: () => {
        this.toastService.success('Promotion email sent.');
        this.showCelebration.set(true);
        const leftoverPoints = this.getLeftoverPoints(modal.promotionId);
        if (leftoverPoints > 0) {
          this.toastService.success(
            `You have ${leftoverPoints} leftover points — they will be carried over to the next period.`,
          );
        }
        this.reload$.next();
      },
      error: (errorResponse) => {
        this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to send the promotion email.'));
      },
    });
  }

  private getLeftoverPoints(promotionId: number): number {
    const detail = this.loadedDetail();
    if (!detail || detail.id !== promotionId || detail.pointsRequired == null) return 0;
    return Math.max(0, detail.pointsAchieved - detail.pointsRequired);
  }

  onPromotionEdited(): void {
    this.editModalOpen.set(false);
    this.reload$.next();
  }

  private isCriteriaClosed(detail: PromotionDetail, labels: Set<string>): boolean {
    const required = detail.requiredCriteriaLabels;
    const replaced = new Set(detail.criteriaOverrides.map((o) => o.label));
    return required.length > 0 && required.every((label) => labels.has(label) || replaced.has(label));
  }

  // Points drop as activities leave their window, and stored rows can carry a hand mark, so opening re-syncs the score.
  private isScoreOutOfSync(detail: PromotionDetail): boolean {
    if (detail.status === PromotionStatus.Sent || detail.pointsRequired == null) return false;
    const ticked = (detail.approvedCriteriaLabels ?? []).includes(ACTIVITY_SCORE_LABEL);
    const marked = detail.criteriaOverrides.some((o) => o.label === ACTIVITY_SCORE_LABEL);
    return marked || ticked !== detail.pointsAchieved >= detail.pointsRequired;
  }

  private verifyCriteriaOnce(detail: PromotionDetail): void {
    const closedInProgress =
      detail.status === PromotionStatus.InProgress &&
      this.isCriteriaClosed(detail, new Set(detail.approvedCriteriaLabels ?? []));
    if (
      !(detail.needsCriteriaVerification || closedInProgress || this.isScoreOutOfSync(detail)) ||
      this.verificationRequested.has(detail.id)
    )
      return;
    if (!this.authService.canWriteManage() && detail.employeeId !== this.authService.userId()) return;
    this.verificationRequested.add(detail.id);
    this.verifying.set(true);
    this.promotionsService
      .verifyCriteria(detail.id)
      .pipe(
        finalize(() => this.verifying.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.reload$.next(),
        error: (errorResponse) =>
          this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to verify the criteria.')),
      });
  }

  addActivity(activities: Activity[], promotionId: number): void {
    this.promotionsService
      .addActivityRange(
        activities.map((a) => a.id),
        promotionId,
      )
      .pipe(
        tap((res: AddActivitiesResult) => {
          if (res.added.length > 0)
            this.toastService.success(
              res.added.length === 1
                ? 'Activity added to the plan.'
                : `${res.added.length} activities added to the plan.`,
            );
          this.showNotAddedActivityToaster(res);
          this.reload$.next();
          if (this.authService.canReadManage()) this.pendingApprovals.refresh();
        }),
        catchError((errorResponse) => {
          this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to add activities.'));
          return of(null);
        }),
      )
      .subscribe();
  }

  private showNotAddedActivityToaster(res: AddActivitiesResult): void {
    if (res.notFound.length > 0)
      this.toastService.error(`Activities with ids ${res.notFound.join(', ')} were not found`);
    if (res.alreadyExists.length > 0)
      this.toastService.error(`Activities with ids ${res.alreadyExists.join(', ')} are already in your promotion`);
  }

  removeActivity(promotionId: number, employeeActivityId: number): void {
    this.promotionsService
      .removeActivity(promotionId, employeeActivityId)
      .pipe(
        tap(() => {
          this.toastService.success('Activity removed from the promotion.');
          this.reload$.next();
          if (this.authService.canReadManage()) this.pendingApprovals.refresh();
        }),
        catchError((errorResponse) => {
          this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to remove the activity.'));
          return of(null);
        }),
      )
      .subscribe();
  }

  startApprove(detail: PromotionDetail, employeeActivityId: number): void {
    const activity = [...detail.plannedActivities, ...detail.selfReportedActivities].find(
      (a) => a.employeeActivityId === employeeActivityId,
    );
    if (activity) this.approving.set({ promotionId: detail.id, activity });
  }

  approveActivity(promotionId: number, employeeActivityId: number, accomplishedOn: string): void {
    this.approving.set(null);
    const approvedById = this.authService.currentUserId ?? undefined;
    this.promotionsService.approveActivity(promotionId, employeeActivityId, accomplishedOn, approvedById).subscribe({
      next: () => {
        this.toastService.success('Activity approved successfully.');
        this.reload$.next();
        if (this.authService.canReadManage()) this.pendingApprovals.refresh();
      },
      error: (errorResponse) =>
        this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to approve the activity.')),
    });
  }

  revertApproval(promotionId: number, employeeActivityId: number): void {
    this.promotionsService.revertActivityApproval(promotionId, employeeActivityId).subscribe({
      next: () => {
        this.toastService.success('Approval reverted.');
        this.reload$.next();
        if (this.authService.canReadManage()) this.pendingApprovals.refresh();
      },
      error: (errorResponse) =>
        this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to revert the approval.')),
    });
  }

  selfReportActivity(
    employeeId: string,
    payload: { employeeActivityId: number; accomplishedOn: string; note?: string },
  ): void {
    this.promotionsService
      .selfReportActivity(employeeId, payload.employeeActivityId, {
        accomplishedOn: payload.accomplishedOn,
        note: payload.note,
      })
      .pipe(
        catchError((errorResponse) => {
          this.toastService.error(firstApiErrorMessage(errorResponse, 'Failed to mark activity as reported.'));
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) return;
        this.toastService.success('Activity marked as reported.');
        this.reload$.next();
      });
  }

  keBlocksSend(d: PromotionDetail): boolean {
    return d.keStatus === KnowledgeEvaluationStatus.Planned;
  }

  onKnowledgeEvaluationChanged(): void {
    this.reload$.next();
  }

  showKeCard(d: PromotionDetail): boolean {
    return this.featureFlags.isEnabled('knowledgeEvaluation') && d.requiredCriteriaLabels.includes('KE required');
  }

  canPlanKe(d: PromotionDetail): boolean {
    return d.status !== PromotionStatus.Sent && d.employeeId === this.authService.userId();
  }

  getInitials(d: PromotionDetail): string {
    return ((d.firstName[0] ?? '') + (d.lastName[0] ?? '')).toUpperCase();
  }

  getAvatarColor(d: PromotionDetail): string {
    const key = d.firstName + d.lastName;
    let hash = 0;
    for (let i = 0; i < key.length; i++) {
      hash = (hash * 31 + key.charCodeAt(i)) & 0xffffff;
    }
    return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length];
  }

  private fillTemplate(text: string, d: PromotionDetail): string {
    return this.emailTemplates.fill(text, {
      firstName: d.firstName,
      lastName: d.lastName,
      fromRole: d.fromRole,
      toRole: d.toRole,
    });
  }
}
