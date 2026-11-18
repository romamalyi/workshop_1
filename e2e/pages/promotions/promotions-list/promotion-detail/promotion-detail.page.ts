import { Page, Locator } from '@playwright/test';
import { ActivityPickerModalComponent } from './activity-picker-modal.component';

export class PromotionDetailPage {
  readonly page: Page;
  readonly employeeName: Locator;
  readonly planActivityButton: Locator;
  readonly activityLists: Locator;
  readonly activityPicker: ActivityPickerModalComponent;

  constructor(page: Page) {
    this.page = page;
    this.employeeName = page.locator('.promo-header__name');
    const activities = page.locator('app-activities-section');
    this.planActivityButton = activities.getByRole('button', { name: 'Plan activity' });
    this.activityLists = activities.locator('app-activity-list');
    this.activityPicker = new ActivityPickerModalComponent(page.locator('app-activity-picker-modal'));
  }

  plannedRow(activityName: string): Locator {
    const title = this.page.locator('.activity-group-title', { hasText: /^Planned$/ });
    const name = this.page.locator('.activity-name').and(this.page.getByText(activityName, { exact: true }));
    return this.activityLists.filter({ has: title }).locator('.activity-row').filter({ has: name });
  }

  async planFirstAvailableActivity(): Promise<string> {
    await this.planActivityButton.click();
    return this.activityPicker.planFirstAvailable();
  }

  async removePlannedActivity(activityName: string): Promise<void> {
    await this.plannedRow(activityName).getByRole('button', { name: 'Remove', exact: true }).click();
  }
}
