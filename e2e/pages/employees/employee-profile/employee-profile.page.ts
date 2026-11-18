import { Page, Locator } from '@playwright/test';
import { ActivitiesTabComponent } from './tabs/activities-tab.component';
import { HistoryTabComponent } from './tabs/history-tab.component';
import { DetailsTabComponent } from './tabs/details-tab.component';

export class EmployeeProfilePage {
  readonly activitiesTabButton: Locator;
  readonly historyTabButton: Locator;
  readonly detailsTabButton: Locator;
  readonly viewPromotionsButton: Locator;

  readonly activities: ActivitiesTabComponent;
  readonly history: HistoryTabComponent;
  readonly details: DetailsTabComponent;

  constructor(page: Page) {
    this.activitiesTabButton = page.getByRole('button', { name: 'Activities' });
    this.historyTabButton = page.getByRole('button', { name: 'History' });
    this.detailsTabButton = page.getByRole('button', { name: 'Details' });
    this.viewPromotionsButton = page.getByRole('button', { name: 'View promotions' });

    this.activities = new ActivitiesTabComponent(page);
    this.history = new HistoryTabComponent(page);
    this.details = new DetailsTabComponent(page);
  }

  async openActivitiesTab(): Promise<void> {
    await this.activitiesTabButton.click();
  }

  async openHistoryTab(): Promise<void> {
    await this.historyTabButton.click();
  }

  async openDetailsTab(): Promise<void> {
    await this.detailsTabButton.click();
  }
}
