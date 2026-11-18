import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Route shell of the Employees module; its pages are listed in the app navigation (app.ts). */
@Component({
  selector: 'app-employees-layout',
  standalone: false,
  templateUrl: './employees-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeesLayoutComponent {}
