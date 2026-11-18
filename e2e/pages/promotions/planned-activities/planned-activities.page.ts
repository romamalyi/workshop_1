import { Page, Locator } from '@playwright/test';
import { MultiSelectFilterComponent } from '../components/multi-select-filter.component';
import { PlannedDateDialogComponent } from './planned-date-dialog.component';

export class PlannedActivitiesPage {
  readonly page: Page;
  readonly promotionsTab: Locator;
  readonly sideRailLink: Locator;
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly searchInput: Locator;
  readonly hrFilter: MultiSelectFilterComponent;
  readonly categoryFilter: MultiSelectFilterComponent;
  readonly columnHeaders: Locator;
  readonly rows: Locator;
  readonly plannedDateDialog: PlannedDateDialogComponent;
  readonly successToast: Locator;

  constructor(page: Page) {
    this.page = page;
    this.promotionsTab = page.getByRole('navigation', { name: 'Modules' }).getByRole('link', { name: 'Promotions' });
    this.sideRailLink = page.getByRole('link', { name: 'Planned activities' });
    this.heading = page.getByRole('heading', { name: 'Planned activities' });
    this.subtitle = page.locator('.page-subtitle');
    this.searchInput = page.getByPlaceholder('Search employee or activity…');
    this.hrFilter = new MultiSelectFilterComponent(page, 'HR');
    this.categoryFilter = new MultiSelectFilterComponent(page, 'Category');
    this.columnHeaders = page.getByRole('columnheader');
    this.rows = page.locator('.pl-row');
    this.plannedDateDialog = new PlannedDateDialogComponent(page.locator('app-planned-date-dialog'));
    this.successToast = page.locator('.toast-success');
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await this.page.locator('.employee-name').first().waitFor();
    await this.promotionsTab.click();
    await this.sideRailLink.click();
  }

  queryParamValues(name: 'hrManager' | 'category' | 'searchTerm'): string[] {
    return new URL(this.page.url()).searchParams.getAll(name);
  }

  rowFor(employeeName: string, activityName: string): Locator {
    const employee = this.page.locator('.pl-employee').and(this.page.getByText(employeeName, { exact: true }));
    const activity = this.page.locator('.pl-activity').and(this.page.getByText(activityName, { exact: true }));
    return this.rows.filter({ has: employee }).filter({ has: activity });
  }

  get firstRowWithHr(): Locator {
    return this.rows.filter({ hasNot: this.page.getByRole('cell', { name: '—', exact: true }) }).first();
  }

  rowsNotMatching(term: string): Locator {
    return this.rows.filter({ hasNotText: term });
  }

  rowsOutsideEmployee(employeeName: string): Locator {
    return this.rows.filter({ hasNot: this.page.locator('.pl-employee', { hasText: employeeName }) });
  }

  rowsOutsideCategory(category: string): Locator {
    return this.rows.filter({ hasNot: this.page.locator('.chip', { hasText: category }) });
  }

  rowsOutsideHr(hrName: string): Locator {
    return this.rows.filter({ hasNot: this.page.getByRole('cell', { name: hrName, exact: true }) });
  }

  rowEmployee(row: Locator): Locator {
    return row.locator('.pl-employee');
  }

  rowCategory(row: Locator): Locator {
    return row.locator('.chip');
  }

  rowPlannedDate(row: Locator): Locator {
    return row.locator('.pl-date .pl-muted');
  }

  rowEditButton(row: Locator): Locator {
    return row.getByRole('button', { name: 'Edit planned date' });
  }

  rowHr(row: Locator): Locator {
    return row.locator('.pl-hr');
  }
}
