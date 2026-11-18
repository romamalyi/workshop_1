import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { EMPTY, catchError, filter, map, startWith, switchMap, take } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { PromotionStatus } from 'src/app/shared/models/promotion-status.model';
import { Promotion } from '../../models/promotion.model';
import { CelebrationSeenStore } from '../../services/celebration-seen.store';
import { PendingApprovalsStore } from '../../services/pending-approvals.store';
import { PlannedActivitiesStore } from '../../services/planned-activities.store';
import { PromotionsService } from '../../services/promotions.service';

const CELEBRATION_WINDOW_MS = 30 * 24 * 60 * 60 * 1000;

/**
 * Route shell of the Promotions module. Its pages are listed in the app navigation (app.ts);
 * the layout keeps the navigation badge counts fresh and shows the promotion celebration.
 */
@Component({
  selector: 'app-promotions-layout',
  templateUrl: './promotions-layout.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionsLayoutComponent {
  private readonly router = inject(Router);
  private readonly pendingApprovals = inject(PendingApprovalsStore);
  private readonly plannedActivities = inject(PlannedActivitiesStore);
  private readonly auth = inject(AuthService);
  private readonly promotionsService = inject(PromotionsService);
  private readonly celebrationSeen = inject(CelebrationSeenStore);

  readonly canReadManage = this.auth.canReadManage;
  readonly showCelebration = signal<boolean>(false);

  private readonly navigationEnd$ = this.router.events.pipe(
    filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    startWith(null),
  );

  private readonly url = toSignal(this.navigationEnd$.pipe(map(() => this.router.url)), {
    initialValue: this.router.url,
  });

  private readonly path = computed(() => this.url().split('?')[0]);

  constructor() {
    effect(() => {
      this.path();
      if (!this.canReadManage()) return;
      this.pendingApprovals.refresh();
      this.plannedActivities.refresh();
    });

    // For a manager the list holds everyone's promotions, so only employees are checked.
    toObservable(this.auth.isProfileLoaded)
      .pipe(
        filter((loaded) => loaded),
        take(1),
        filter(() => !this.canReadManage()),
        switchMap(() =>
          this.promotionsService
            .getAll({ status: PromotionStatus.Sent, pageNumber: 1, pageSize: 1 })
            .pipe(catchError(() => EMPTY)),
        ),
        map((page) => page.items[0]),
        filter((promotion): promotion is Promotion => !!promotion && this.isUncelebrated(promotion)),
        takeUntilDestroyed(),
      )
      .subscribe((promotion) => {
        this.celebrationSeen.add(promotion.id);
        this.showCelebration.set(true);
      });
  }

  private isUncelebrated(promotion: Promotion): boolean {
    if (!promotion.sentAt) return false;
    const recent = Date.now() - Date.parse(promotion.sentAt) <= CELEBRATION_WINDOW_MS;
    return recent && !this.celebrationSeen.has(promotion.id);
  }
}
