import { Page, Locator } from '@playwright/test';

export type EmployeeFilterName = 'Seniority' | 'Technology' | 'Project';

export class EmployeesListPage {
  readonly page: Page;
  readonly table: Locator;
  readonly nameColumnHeader: Locator;
  readonly noMatchesMessage: Locator;
  readonly rows: Locator;
  readonly seniorityCells: Locator;
  readonly technologyCells: Locator;
  readonly nameCells: Locator;
  readonly searchInput: Locator;
  readonly initialLoading: Locator;

  constructor(page: Page) {
    this.page = page;
    this.table = page.getByRole('table');
    this.nameColumnHeader = page.getByRole('columnheader', { name: 'Name' });
    this.noMatchesMessage = page.getByText('No employees match the selected filters.', { exact: true });
    this.rows = this.table.locator('.emp-row');
    this.seniorityCells = this.rows.locator('[role="cell"]:nth-child(2)');
    this.technologyCells = this.rows.locator('[role="cell"]:nth-child(3)');
    this.nameCells = this.rows.locator('.employee-name');
    this.searchInput = page.getByPlaceholder('Search by name...');
    this.initialLoading = page.getByRole('status').filter({ hasText: 'Loading...' });
  }

  // Holds every list search request until the returned release function is called.
  async holdSearchRequests(): Promise<() => void> {
    let release!: () => void;
    const released = new Promise<void>((resolve) => (release = resolve));
    await this.page.route(
      (url) => url.pathname === '/api/employees/search',
      async (route) => {
        await released;
        await route.continue();
      },
    );
    return release;
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
  }

  async openFirstEmployee(): Promise<void> {
    await this.table.getByRole('row').nth(1).click();
  }

  async searchByName(name: string): Promise<void> {
    await this.searchInput.fill(name);
  }

  async openFilterDropdown(filterName: EmployeeFilterName): Promise<void> {
    await this.page.getByRole('button', { name: filterName }).click();
  }

  async deselectAllInFilter(filterName: EmployeeFilterName): Promise<void> {
    await this.openFilterDropdown(filterName);
    await this.page.getByRole('checkbox', { name: 'All', exact: true }).click();
  }

  async selectOnlyFirstOptionIn(filterName: EmployeeFilterName): Promise<string> {
    await this.deselectAllInFilter(filterName);
    const firstOption = this.page.locator('.emp-filter-item:not(.emp-filter-item--all)').first();
    const value = (await firstOption.locator('span').innerText()).trim();
    await firstOption.click();
    return value;
  }


  async projectTagsPerRow(): Promise<{ tags: string[]; hasMore: boolean }[]> {
    const rows = await this.rows.all();
    const result: { tags: string[]; hasMore: boolean }[] = [];
    for (const row of rows) {
      const tags = await row.locator('.emp-project-tag:not(.emp-project-tag--more)').allTextContents();
      const hasMore = (await row.locator('.emp-project-tag--more').count()) > 0;
      result.push({ tags: tags.map((t) => t.trim()), hasMore });
    }
    return result;
  }
}
