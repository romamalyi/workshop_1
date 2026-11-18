import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EmployeeDetailComponent } from './components/employee-detail/employee-detail.component';
import { EmployeeSettingsPageComponent } from './components/employee-settings-page/employee-settings-page.component';
import { EmployeesLayoutComponent } from './components/employees-layout/employees-layout.component';
import { EmployeesPageComponent } from './components/employees-page/employees-page.component';
import { canReadManage, canWriteManage } from '../core/guards/permission.guard';
import { AuthGuardService } from '../core/services/auth-guard.service';

const routes: Routes = [
  {
    path: '',
    component: EmployeesLayoutComponent,
    canActivate: [AuthGuardService, canReadManage],
    children: [
      { path: '', component: EmployeesPageComponent },
      { path: 'settings', component: EmployeeSettingsPageComponent, canActivate: [canWriteManage] },
    ],
  },
  { path: ':id', component: EmployeeDetailComponent, canActivate: [AuthGuardService, canReadManage] },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class EmployeesRoutingModule {}
