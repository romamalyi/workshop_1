import { Page, Locator, expect } from '@playwright/test';
import { ApproveActivityDialogComponent } from './approve-activity-dialog.component';

export class PendingApprovalsPage {
  readonly page: Page;

  readonly promotionsTab: Locator;
  readonly heading: Locator;
  readonly subtitle: Locator;

  readonly navLink: Locator;
  readonly navBadge: Locator;

  readonly hrFilter: Locator;
  readonly hrFilterAllCheckbox: Locator;
  readonly hrOptions: Locator;
  readonly hrFilterBadge: Locator;

  readonly columnHeaders: Locator;
  readonly rows: Locator;
  readonly emptyRow: Locator;

  readonly approveDialog: ApproveActivityDialogComponent;

  constructor(page: Page) {
    this.page = page;

    this.promotionsTab = page
      .getByRole('navigation', { name: 'Modules' })
      .getByRole('link', { name: 'Promotions', exact: true });
    this.heading = page.getByRole('heading', { name: 'Pending approvals' });
    this.subtitle = page.locator('.page-subtitle');

    this.navLink = page
      .getByRole('navigation', { name: 'Pages' })
      .getByRole('link', { name: 'Pending approvals' });
    this.navBadge = this.navLink.locator('.badge');

    this.hrFilter = page.locator('app-multi-select-filter');
    this.hrFilterAllCheckbox = this.hrFilter.getByRole('checkbox', { name: 'All', exact: true });
    this.hrOptions = this.hrFilter.locator('.emp-filter-item:not(.emp-filter-item--all)');
    this.hrFilterBadge = this.hrFilter.locator('.emp-filter-badge');

    this.columnHeaders = page.getByRole('columnheader');
    this.rows = page.locator('.pa-row');
    this.emptyRow = page.locator('.pa-empty-row');

    this.approveDialog = new ApproveActivityDialogComponent(page);
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await this.page.locator('.employee-name').first().waitFor();
    await this.promotionsTab.click();
    await this.navLink.click();
  }

  // Only a row tied to a promotion shows "<from role> → <to role>"
  get promotionRows(): Locator {
    return this.rows.filter({ has: this.page.locator('.pa-role-arrow') });
  }

  selectedHrManagersInUrl(): string[] {
    return new URL(this.page.url()).searchParams.getAll('hrManager');
  }

  async openHrFilter(): Promise<void> {
    await this.hrFilter.getByRole('button').click();
  }

  // Leaves the dropdown open
  async selectAllHr(): Promise<void> {
    await this.openHrFilter();
    await this.hrFilterAllCheckbox.check();
  }

  // Checks All first, since a default HR may be preselected. Leaves the dropdown open
  async clearHrFilter(): Promise<void> {
    await this.selectAllHr();
    await this.hrFilterAllCheckbox.uncheck();
  }

  // Closes the dropdown afterwards, which would otherwise cover the Actions column
  async resetHrFilterToAll(): Promise<void> {
    await this.selectAllHr();
    await this.openHrFilter();
  }

  // Waits until the list has as many rows as the badge shows, so it has fully loaded
  async loadedPendingCount(): Promise<number> {
    await expect(this.navBadge).toBeVisible();
    const count = Number(await this.navBadge.innerText());
    await expect(this.rows).toHaveCount(count);
    return count;
  }

  rowFor(activityName: string): Locator {
    return this.rows.filter({ has: this.page.locator('.pa-activity', { hasText: activityName }) });
  }

  approveButton(row: Locator): Locator {
    return row.getByRole('button', { name: 'Approve' });
  }

  async openApproveDialog(activityName: string): Promise<void> {
    // Earlier runs leave rows with the same name, and the newest one is listed last
    await this.approveButton(this.rowFor(activityName).last()).click();
  }

  async approveActivity(activityName: string, day?: number): Promise<void> {
    await this.openApproveDialog(activityName);
    await this.approveDialog.approve(day);
  }

  // The badge is hidden at 0, and the subtitle pluralises
  async expectPendingCount(count: number): Promise<void> {
    if (count === 0) {
      await expect(this.navBadge).toBeHidden();
    } else {
      await expect(this.navBadge).toHaveText(String(count));
    }
    await expect(this.subtitle).toHaveText(
      count === 1 ? '1 activity reported by employees. Approve to count the points.' : `${count} activities reported by employees. Approve to count the points.`,
    );
  }
}
