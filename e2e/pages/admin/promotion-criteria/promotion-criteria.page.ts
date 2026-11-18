import { Page, Locator, expect } from '@playwright/test';
import { CriteriaModalComponent } from './criteria-modal.component';

export interface CriteriaData {
  technology: string;
  seniority: string;
  subLevel: string;
  englishLevel: string;
  score: number;
}

const escapeRegExp = (text: string): string => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

export class PromotionCriteriaPage {
  readonly page: Page;
  readonly adminTab: Locator;
  readonly promotionCriteriaLink: Locator;
  readonly heading: Locator;
  readonly showInactiveCheckbox: Locator;
  readonly addButton: Locator;
  readonly technologyChips: Locator;
  readonly activeTechnologyChip: Locator;
  readonly headerRow: Locator;
  readonly rows: Locator;
  readonly statusBadge: Locator;
  readonly inactiveRows: Locator;
  readonly modal: CriteriaModalComponent;

  constructor(page: Page) {
    this.page = page;
    this.adminTab = page
      .getByRole('navigation', { name: 'Modules' })
      .getByRole('link', { name: 'Admin', exact: true });
    this.promotionCriteriaLink = page
      .getByRole('navigation', { name: 'Pages' })
      .getByRole('link', { name: 'Promotion Criteria' });
    this.heading = page.getByRole('heading', { name: 'Promotion Criteria' });
    this.showInactiveCheckbox = page.getByRole('checkbox', { name: 'Show inactive' });
    this.addButton = page.getByRole('button', { name: '+ Add Criteria' });
    this.technologyChips = page.locator('.category-filter__chip');
    this.activeTechnologyChip = this.technologyChips.and(page.locator('.active'));
    this.headerRow = page.locator('.pc-row-header');
    this.rows = page.locator('.pc-row');
    // Only inactive rows carry a status badge
    this.statusBadge = page.locator('.badge');
    this.inactiveRows = this.rows.filter({ has: this.statusBadge });
    this.modal = new CriteriaModalComponent(page);
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await this.adminTab.click();
    await this.promotionCriteriaLink.click();
  }

  async filterByTechnology(name: string): Promise<void> {
    await this.technologyChips.filter({ hasText: new RegExp(`^\\s*${escapeRegExp(name)}\\s*$`) }).click();
  }

  // Level cell reads "<Seniority> <Sub-level>", e.g. "Middle Strong"
  rowForLevel(level: string): Locator {
    return this.rows.filter({
      has: this.page.locator('.pc-col-level', { hasText: new RegExp(`^\\s*${escapeRegExp(level)}\\s*$`) }),
    });
  }

  activeRowForLevel(level: string): Locator {
    return this.rowForLevel(level).filter({ hasNot: this.statusBadge });
  }

  inactiveBadge(level: string): Locator {
    return this.rowForLevel(level).locator(this.statusBadge);
  }

  async openAddModal(): Promise<void> {
    await this.addButton.click();
  }

  async fillCriteriaForm(criteria: Partial<CriteriaData>): Promise<void> {
    if (criteria.technology) {
      await this.modal.technologySelect.selectOption({ label: criteria.technology });
    }
    if (criteria.seniority) {
      await this.modal.senioritySelect.selectOption({ label: criteria.seniority });
    }
    if (criteria.subLevel) {
      await this.modal.subLevelSelect.selectOption({ label: criteria.subLevel });
    }
    if (criteria.englishLevel) {
      await this.modal.englishLevelSelect.selectOption({ label: criteria.englishLevel });
    }
    if (criteria.score !== undefined) {
      await this.modal.scoreInput.fill(String(criteria.score));
    }
  }

  async createCriteria(criteria: CriteriaData): Promise<void> {
    await this.openAddModal();
    await this.fillCriteriaForm(criteria);
    await this.modal.saveButton.click();
  }

  async editCriteria(level: string, score: number, preserveHistory = false): Promise<void> {
    await this.activeRowForLevel(level).getByRole('button', { name: 'Edit' }).click();
    await this.modal.scoreInput.fill(String(score));
    await this.modal.preserveHistoryCheckbox.setChecked(preserveHistory);
    await this.modal.saveButton.click();
  }

  async deleteCriteria(level: string): Promise<void> {
    await this.activeRowForLevel(level).getByRole('button', { name: 'Delete' }).click();
    await this.page.locator('.modal-box--sm').getByRole('button', { name: 'Delete', exact: true }).click();
  }

  async deleteActiveCriteriaIfPresent(level: string): Promise<void> {
    // The header renders before the data, so an empty list can't be told from an unloaded one:
    // wait for any row of the technology (the Test data always has some) before counting
    await expect(this.rows.first()).toBeVisible();
    const activeRow = this.activeRowForLevel(level);
    if ((await activeRow.count()) > 0) {
      await this.deleteCriteria(level);
      await expect(activeRow).toHaveCount(0);
    }
  }

  // Earlier runs leave inactive rows for the level, so pin the one to restore by its score
  async reactivateCriteria(level: string, score: number): Promise<void> {
    await this.inactiveRows
      .filter({ has: this.page.locator('.pc-col-level', { hasText: level }) })
      .filter({ hasText: `${score} pts` })
      .first()
      .getByRole('button', { name: 'Reactivate' })
      .click();
  }
}
