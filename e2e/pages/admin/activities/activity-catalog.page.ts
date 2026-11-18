import { Page, Locator } from '@playwright/test';

export type ActivityCategory =
  | 'Self-courses'
  | 'Certifications'
  | 'Mentorship'
  | 'Presentations'
  | 'Workshop'
  | 'SDO Activities'
  | 'R&D Activities'
  | 'KE Conduction'
  | 'Interview';

export class ActivityCatalogPage {
  readonly page: Page;
  readonly adminTab: Locator;
  readonly heading: Locator;
  readonly showInactiveCheckbox: Locator;
  readonly addButton: Locator;
  readonly cancelButton: Locator;
  readonly categoryChips: Locator;
  readonly sections: Locator;
  readonly rows: Locator;
  readonly statusBadge: Locator;
  readonly inactiveRows: Locator;

  constructor(page: Page) {
    this.page = page;
    this.adminTab = page
      .getByRole('navigation', { name: 'Modules' })
      .getByRole('link', { name: 'Admin', exact: true });
    this.heading = page.getByRole('heading', { name: 'Activity Catalog' });
    this.showInactiveCheckbox = page.getByRole('checkbox', { name: 'Show inactive' });
    this.addButton = page.getByRole('button', { name: '+ Add Activity' });
    this.cancelButton = page.getByRole('button', { name: 'Cancel', exact: true });
    this.categoryChips = page.locator('.category-filter__chip');
    this.sections = page.locator('.catalog-section');
    this.rows = page.locator('.catalog-row');
    // Only inactive rows carry a status badge
    this.statusBadge = page.locator('.badge');
    this.inactiveRows = this.rows.filter({ has: this.statusBadge });
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await this.adminTab.click();
  }

  async filterByCategory(name: 'All' | ActivityCategory): Promise<void> {
    await this.categoryChips.filter({ hasText: new RegExp(`^\\s*${name}\\s*$`) }).click();
  }

  sectionTitled(title: string): Locator {
    return this.sections.filter({ has: this.page.locator('.catalog-section__title', { hasText: title }) });
  }

  sectionHeader(section: Locator): Locator {
    return section.locator('.catalog-section__header');
  }

  sectionArrow(section: Locator): Locator {
    return section.locator('.catalog-section__collapse');
  }

  rowNamed(name: string): Locator {
    return this.rows.filter({ hasText: name });
  }

  activeRowNamed(name: string): Locator {
    return this.rowNamed(name).filter({ hasNot: this.statusBadge });
  }

  inactiveBadge(name: string): Locator {
    return this.rowNamed(name).locator(this.statusBadge);
  }

  duplicateNameError(name: string): Locator {
    return this.page.getByText(`An activity named '${name}' already exists.`);
  }

  async createActivity(activity: {
    category: ActivityCategory;
    name: string;
    period: string;
    technologyIndex: number;
    points: number;
  }): Promise<void> {
    // The modal's labels have no `for`, so selects are found through their field wrapper
    const field = (label: string) => this.page.locator('.form-field').filter({ hasText: label });

    await this.addButton.click();
    await field('Category').locator('select').selectOption({ label: activity.category });
    await this.page.getByPlaceholder('Activity name').fill(activity.name);
    await field('Within Period').locator('select').selectOption({ label: activity.period });
    await field('Technology').locator('select').selectOption({ index: activity.technologyIndex });
    await this.page.getByRole('spinbutton').fill(String(activity.points));
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
  }

  async editActivity(
    name: string,
    changes: { name: string; points: number; preserveHistory?: boolean },
  ): Promise<void> {
    await this.rowNamed(name).getByRole('button', { name: 'Edit' }).click();
    await this.page.getByPlaceholder('Activity name').fill(changes.name);
    await this.page.getByRole('spinbutton').fill(String(changes.points));
    if (changes.preserveHistory) {
      await this.page.getByRole('checkbox', { name: 'Preserve history' }).check();
    }
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
  }

  async deleteActivity(name: string): Promise<void> {
    await this.rowNamed(name).getByRole('button', { name: 'Delete' }).click();
    await this.page.locator('.modal-box--sm').getByRole('button', { name: 'Delete', exact: true }).click();
  }

  async reactivateActivity(name: string): Promise<void> {
    await this.rowNamed(name).getByRole('button', { name: 'Reactivate' }).click();
  }
}
