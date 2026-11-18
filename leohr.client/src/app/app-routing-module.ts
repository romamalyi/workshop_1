import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuardService } from './core/services/auth-guard.service';
import { LoginCallbackComponent } from './login-callback/login-callback.component';
import { canManageAdmin, canWriteManage, hasOnboardingAccess } from './core/guards/permission.guard';

const routes: Routes = [
  { path: '', redirectTo: 'employees', pathMatch: 'full' },
  {
    path: 'employees',
    loadChildren: () => import('./employees/employees.module').then((m) => m.EmployeesModule),
    canActivate: [AuthGuardService],
  },
  {
    path: 'promotions',
    loadChildren: () => import('./promotions/promotions.module').then((m) => m.PromotionsModule),
    canActivate: [AuthGuardService],
  },
  {
    path: 'birthdays',
    loadChildren: () => import('./birthdays/birthdays.module').then((m) => m.BirthdaysModule),
    canActivate: [AuthGuardService, canWriteManage],
  },
  {
    path: 'onboarding',
    loadChildren: () => import('./onboarding/onboarding.module').then((m) => m.OnboardingModule),
    canActivate: [AuthGuardService, hasOnboardingAccess],
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.module').then((m) => m.AdminModule),
    canActivate: [AuthGuardService, canManageAdmin],
  },
  { path: 'login', component: LoginCallbackComponent },
  { path: '**', redirectTo: 'employees' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { initialNavigation: 'disabled', scrollPositionRestoration: 'enabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
