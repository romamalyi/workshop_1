import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

@Component({
  selector: 'app-promotions-settings-page',
  templateUrl: './promotions-settings-page.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromotionsSettingsPageComponent {
  public readonly activeTab = signal<'holder' | 'email'>('holder');
}
