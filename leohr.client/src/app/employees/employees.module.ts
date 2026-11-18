import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { EmployeeActivityModalComponent } from './components/employee-detail/employee-activity-modal/employee-activity-modal.component';
import { EmployeeActivitiesTabComponent } from './components/employee-detail/employee-activities-tab/employee-activities-tab.component';
import { EmployeeDetailComponent } from './components/employee-detail/employee-detail.component';
import { EmployeeSettingsPageComponent } from './components/employee-settings-page/employee-settings-page.component';
import { EmployeesLayoutComponent } from './components/employees-layout/employees-layout.component';
import { EmployeesPageComponent } from './components/employees-page/employees-page.component';
import { EmployeesRoutingModule } from './employees-routing.module';

@NgModule({
  declarations: [
    EmployeesPageComponent,
    EmployeeDetailComponent,
    EmployeesLayoutComponent,
    EmployeeSettingsPageComponent,
  ],
  imports: [
    CommonModule,
    ScrollingModule,
    FormsModule,
    EmployeesRoutingModule,
    SharedModule,
    EmployeeActivitiesTabComponent,
    EmployeeActivityModalComponent,
  ],
})
export class EmployeesModule {}
