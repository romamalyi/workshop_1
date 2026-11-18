import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { OAuthService } from 'angular-oauth2-oidc';
import { NavModule, NavPage, User } from 'ngx-leobit-components';
import { environment } from 'src/environments/environment';
import { AuthService } from './core/services/auth.service';
import { FeatureFlagsService } from './core/services/feature-flags.service';
import { OnboardingAttentionService } from './onboarding/services/onboarding-attention.service';
import { OnboardingMyTasksService } from './onboarding/services/onboarding-my-tasks.service';
import { PendingApprovalsStore } from './promotions/services/pending-approvals.store';
import { PlannedActivitiesStore } from './promotions/services/planned-activities.store';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly oauth = inject(OAuthService);
  private readonly featureFlags = inject(FeatureFlagsService);
  // Badge counts are loaded by the feature layouts (promotions, onboarding); the navigation only reads them.
  private readonly pendingApprovals = inject(PendingApprovalsStore);
  private readonly plannedActivities = inject(PlannedActivitiesStore);
  private readonly onboardingMyTasks = inject(OnboardingMyTasksService);
  private readonly onboardingAttention = inject(OnboardingAttentionService);

  readonly navigationEnv = environment.navigationData;

  private readonly userProfile = toSignal(this.authService.userProfile$, { initialValue: null });
  readonly navUser = computed<User | null>(() => {
    const profile = this.userProfile();
    if (!profile) {
      return null;
    }
    return {
      name: profile.name,
      email: profile.email,
      picture: profile.picture || '/default-avatar.svg',
    };
  });

  readonly modules = computed<NavModule[]>(() => {
    const auth = this.authService;
    const modules: NavModule[] = [];

    if (auth.canReadManage()) {
      const pages: NavPage[] = [{ id: 'list', label: 'Employees', route: '/employees' }];
      if (auth.canWriteManage()) {
        pages.push({ id: 'settings', label: 'Settings', route: '/employees/settings' });
      }
      modules.push({
        id: 'employees',
        label: 'Employees',
        icon: 'users',
        route: '/employees',
        activeRoute: '/employees',
        groups: [{ label: 'Employees', pages }],
      });
    }

    // Users without Manage read access only see the promotions list, so there are no pages to switch between.
    const promotionPages: NavPage[] = [];
    if (auth.canReadManage()) {
      promotionPages.push(
        // The list stays active on a promotion's detail page (/promotions/:id).
        { id: 'list', label: 'Promotions', route: '/promotions/list', activeRoutes: ['/promotions'] },
        {
          id: 'approvals',
          label: 'Pending approvals',
          route: '/promotions/approvals',
          badge: this.pendingApprovals.count(),
        },
        {
          id: 'planned',
          label: 'Planned activities',
          route: '/promotions/planned',
          badge: this.plannedActivities.count(),
        },
      );
      if (auth.canWriteManage()) {
        if (this.featureFlags.isEnabled('knowledgeEvaluation')) {
          promotionPages.push({
            id: 'knowledge-evaluation',
            label: 'Knowledge evaluation',
            route: '/promotions/knowledge-evaluation',
          });
        }
        promotionPages.push({ id: 'settings', label: 'Settings', route: '/promotions/settings' });
      }
    }
    modules.push({
      id: 'promotions',
      label: 'Promotions',
      icon: 'trending',
      route: '/promotions/list',
      activeRoute: '/promotions',
      groups: promotionPages.length ? [{ label: 'Promotions', pages: promotionPages }] : [],
    });

    // Birthdays module hidden for now; the /birthdays route is still registered.

    if (auth.hasOnboardingAccess()) {
      modules.push(this.onboardingModule());
    }

    if (auth.canManageAdmin()) {
      modules.push({
        id: 'admin',
        label: 'Admin',
        icon: 'sliders',
        route: '/admin/activities',
        activeRoute: '/admin',
        groups: [
          {
            label: 'Admin',
            pages: [
              { id: 'activities', label: 'Activities', route: '/admin/activities' },
              { id: 'promotion-criteria', label: 'Promotion Criteria', route: '/admin/promotion-criteria' },
            ],
          },
        ],
      });
    }

    return modules;
  });

  constructor() {
    this.authService.bootstrapAuthentication();
  }

  signIn(): void {
    this.oauth.initLoginFlow();
  }

  signOut(): void {
    this.oauth.logOut();
  }

  private onboardingModule(): NavModule {
    const canWrite = this.authService.canWriteOnboarding();
    const isAdmin = this.authService.isOnboardingAdmin();

    const onboardingPages: NavPage[] = [
      {
        id: 'my-tasks',
        label: canWrite ? 'Tasks' : 'My Tasks',
        route: '/onboarding/my-tasks',
        // A task opened from the list (/onboarding/tasks/:id) keeps the list active.
        activeRoutes: ['/onboarding/tasks'],
        badge: this.onboardingMyTasks.openCount(),
      },
    ];
    if (canWrite) {
      onboardingPages.push(
        {
          id: 'needs-attention',
          label: 'Unassigned tasks',
          route: '/onboarding/needs-attention',
          badge: this.onboardingAttention.unassignedTasks().length,
        },
        { id: 'flows', label: 'Flows', route: '/onboarding/flows' },
      );
    }

    const configurationPages: NavPage[] = [];
    if (canWrite) {
      configurationPages.push({ id: 'types', label: 'Onboarding types', route: '/onboarding/types' });
    }
    if (isAdmin) {
      configurationPages.push({ id: 'templates', label: 'Task templates', route: '/onboarding/templates' });
    }
    if (canWrite) {
      configurationPages.push({ id: 'settings', label: 'Settings', route: '/onboarding/settings' });
    }

    return {
      id: 'onboarding',
      label: 'Onboarding',
      icon: 'userplus',
      route: canWrite ? '/onboarding/flows' : '/onboarding/my-tasks',
      activeRoute: '/onboarding',
      groups: [
        { label: 'Onboarding', pages: onboardingPages },
        ...(configurationPages.length ? [{ label: 'Configuration', pages: configurationPages }] : []),
      ],
    };
  }
}
