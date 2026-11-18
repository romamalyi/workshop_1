import { Page, Locator } from '@playwright/test';

export class ActivitiesTabComponent {
  readonly page: Page;
  readonly pendingPanelTitle: Locator;
  readonly approvedPanelTitle: Locator;
  readonly reportActivityButton: Locator;
  readonly addActivityButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // Scoped to the panel-name class so a same-named status badge is never mistaken for the panel title.
    this.pendingPanelTitle = page.locator('.ea-panel-name', { hasText: 'Pending' });
    this.approvedPanelTitle = page.locator('.ea-panel-name', { hasText: 'Approved' });
    this.reportActivityButton = page.getByRole('button', { name: '+ Report activity' });
    this.addActivityButton = page.getByRole('button', { name: '+ Add activity' });
  }

  pendingRowsNamed(activityName: string): Locator {
    const pendingPanel = this.page
      .locator('.ea-panel')
      .filter({ has: this.page.locator('.ea-panel-name', { hasText: 'Pending' }) });
    return pendingPanel.locator('.ea-row', { hasText: activityName });
  }

  async addFirstAvailableActivity(): Promise<string> {
    await this.addActivityButton.click();

    const activityOption = this.page.locator('.ea-catalog-row:not(.ea-catalog-row--disabled)').first();
    const activityName = (await activityOption.locator('.ea-catalog-name').innerText()).trim();
    await activityOption.click();

    await this.page.getByRole('button', { name: 'Select date...' }).click();
    await this.page.getByRole('button', { name: String(new Date().getDate()), exact: true }).click();

    await this.page.getByRole('button', { name: 'Save' }).click();

    return activityName;
  }

  async deleteNewestPendingActivity(activityName: string): Promise<void> {
    // Sorted newest-first server-side, so .first() is the one we just created — even if an older
    // activity with the same name already exists on this employee.
    await this.pendingRowsNamed(activityName).first().getByRole('button', { name: 'Delete' }).click();

    const confirmDialog = this.page.locator('.modal-box', {
      has: this.page.getByRole('heading', { name: 'Delete Activity' }),
    });
    await confirmDialog.getByRole('button', { name: 'Delete' }).click();
  }
}
