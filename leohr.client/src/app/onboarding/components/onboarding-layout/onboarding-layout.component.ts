import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { EMPTY, catchError, filter, fromEvent, interval, merge, switchMap, take } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { OnboardingAttentionService } from '../../services/onboarding-attention.service';
import { OnboardingMyTasksService } from '../../services/onboarding-my-tasks.service';

const MY_TASKS_REFRESH_MS = 60_000;

/**
 * Route shell of the Onboarding module. Its pages are listed in the app navigation (app.ts);
 * the layout keeps the navigation badge counts fresh.
 */
@Component({
  selector: 'app-onboarding-layout',
  templateUrl: './onboarding-layout.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingLayoutComponent {
  private readonly attention = inject(OnboardingAttentionService);
  private readonly myTasks = inject(OnboardingMyTasksService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);

  private readonly canWriteOnboarding = computed(() => this.auth.canWriteOnboarding());

  constructor() {
    // The badges are hints only; each page reports its own load errors.
    // Other users change tasks too, so the list reloads on navigation and on a timer while the tab is visible.
    const isVisible = () => this.document.visibilityState === 'visible';
    merge(
      inject(Router).events.pipe(filter((e) => e instanceof NavigationEnd)),
      interval(MY_TASKS_REFRESH_MS).pipe(filter(isVisible)),
      fromEvent(this.document, 'visibilitychange').pipe(filter(isVisible)),
    )
      .pipe(
        switchMap(() => this.myTasks.refresh().pipe(catchError(() => EMPTY))),
        takeUntilDestroyed(),
      )
      .subscribe();

    // The Unassigned tasks endpoint needs Onboarding write access, and the claim can arrive after the layout is created.
    toObservable(this.canWriteOnboarding)
      .pipe(
        filter(Boolean),
        take(1),
        switchMap(() => this.attention.refresh()),
        catchError(() => EMPTY),
        takeUntilDestroyed(),
      )
      .subscribe();
  }
}
