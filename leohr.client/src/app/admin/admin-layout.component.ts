import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Route shell of the Admin module; its pages are listed in the app navigation (app.ts). */
@Component({
  selector: 'app-admin-layout',
  templateUrl: './admin-layout.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLayoutComponent {}
