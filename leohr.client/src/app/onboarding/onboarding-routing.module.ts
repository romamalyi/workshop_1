import { inject, NgModule } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { CanActivateFn, CanDeactivateFn, Router, RouterModule, Routes } from '@angular/router';
import { catchError, filter, map, of, take, timeout } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { taskResolver } from './onboarding-task.resolver';
import { OnboardingLayoutComponent } from './components/onboarding-layout/onboarding-layout.component';
import { OnboardingPageComponent } from './components/onboarding-page/onboarding-page.component';
import { OnboardingFlowWizardComponent } from './components/onboarding-flow-wizard/onboarding-flow-wizard.component';
import { OnboardingTemplatesPageComponent } from './components/onboarding-templates-page/onboarding-templates-page.component';
import { OnboardingTemplatePageComponent } from './components/onboarding-template-page/onboarding-template-page.component';
import { OnboardingSettingsPageComponent } from './components/onboarding-settings-page/onboarding-settings-page.component';
import { OnboardingTypePageComponent } from './components/onboarding-type-page/onboarding-type-page.component';
import { OnboardingTypesPageComponent } from './components/onboarding-types-page/onboarding-types-page.component';
import { OnboardingFlowOverviewComponent } from './components/onboarding-flow-overview/onboarding-flow-overview.component';
import { NeedsAttentionPageComponent } from './components/needs-attention-page/needs-attention-page.component';
import { onboardingAdmin, canWriteOnboarding } from '../core/guards/permission.guard';
import { OnboardingTaskDetailComponent } from './components/onboarding-task-detail/onboarding-task-detail.component';
import { MyTasksPageComponent } from './components/my-tasks-page/my-tasks-page.component';

const PROFILE_LOAD_TIMEOUT_MS = 4000;

const confirmTypeLeave: CanDeactivateFn<OnboardingTypePageComponent> = (page) => page.canLeave();

const confirmTemplateLeave: CanDeactivateFn<OnboardingTemplatePageComponent> = (page) => page.canLeave();

// The write access is known only once the profile has loaded, which can be after the first navigation.
const openOnboardingHome: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return toObservable(auth.isProfileLoaded).pipe(
    filter(Boolean),
    take(1),
    timeout(PROFILE_LOAD_TIMEOUT_MS),
    map(() => router.createUrlTree(['/onboarding', auth.canWriteOnboarding() ? 'flows' : 'my-tasks'])),
    catchError(() => of(router.createUrlTree(['/login']))),
  );
};

const routes: Routes = [
  {
    path: '',
    component: OnboardingLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', canActivate: [openOnboardingHome], children: [] },
      { path: 'my-tasks', component: MyTasksPageComponent },
      { path: 'tasks/:id', component: OnboardingTaskDetailComponent, resolve: { result: taskResolver } },
      { path: 'flows', component: OnboardingPageComponent, canActivate: [canWriteOnboarding] },
      { path: 'flows/new', component: OnboardingFlowWizardComponent, canActivate: [canWriteOnboarding] },
      { path: 'flows/:id', component: OnboardingFlowOverviewComponent, canActivate: [canWriteOnboarding] },
      { path: 'needs-attention', component: NeedsAttentionPageComponent, canActivate: [canWriteOnboarding] },
      { path: 'types', component: OnboardingTypesPageComponent, canActivate: [canWriteOnboarding] },
      {
        path: 'types/new',
        component: OnboardingTypePageComponent,
        canActivate: [canWriteOnboarding],
        canDeactivate: [confirmTypeLeave],
      },
      {
        path: 'types/:id',
        component: OnboardingTypePageComponent,
        canActivate: [canWriteOnboarding],
        canDeactivate: [confirmTypeLeave],
      },
      { path: 'templates', component: OnboardingTemplatesPageComponent, canActivate: [onboardingAdmin] },
      {
        path: 'templates/:id',
        component: OnboardingTemplatePageComponent,
        canActivate: [onboardingAdmin],
        canDeactivate: [confirmTemplateLeave],
      },
      { path: 'settings', component: OnboardingSettingsPageComponent, canActivate: [canWriteOnboarding] },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class OnboardingRoutingModule {}
