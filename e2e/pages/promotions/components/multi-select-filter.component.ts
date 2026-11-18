import { Page, Locator } from '@playwright/test';

export class MultiSelectFilterComponent {
  readonly root: Locator;
  readonly trigger: Locator;
  readonly badge: Locator;
  readonly allCheckbox: Locator;

  constructor(page: Page, label: string) {
    this.root = page
      .locator('app-multi-select-filter')
      .filter({ has: page.locator('.emp-filter-trigger', { hasText: label }) });
    this.trigger = this.root.locator('.emp-filter-trigger');
    this.badge = this.root.locator('.emp-filter-badge');
    this.allCheckbox = this.root.getByRole('checkbox', { name: 'All', exact: true });
  }

  async selectOnly(name: string): Promise<void> {
    await this.trigger.click();
    await this.allCheckbox.check();
    await this.allCheckbox.uncheck();
    await this.root.getByRole('checkbox', { name, exact: true }).check();
    await this.trigger.click();
  }

  async resetToAll(): Promise<void> {
    await this.trigger.click();
    await this.allCheckbox.check();
    await this.trigger.click();
  }
}
