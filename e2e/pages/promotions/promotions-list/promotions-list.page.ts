import { Page, Locator } from '@playwright/test';

export class PromotionsListPage {
  readonly page: Page;
  readonly promotionsTab: Locator;
  readonly searchInput: Locator;
  readonly cards: Locator;

  constructor(page: Page) {
    this.page = page;
    this.promotionsTab = page.getByRole('navigation', { name: 'Modules' }).getByRole('link', { name: 'Promotions' });
    this.searchInput = page.getByPlaceholder('Search employees…');
    this.cards = page.locator('.data-table__row');
  }

  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await this.page.locator('.employee-name').first().waitFor();
    await this.promotionsTab.click();
    await this.searchInput.waitFor();
  }

  async openNthPromotion(index: number): Promise<string> {
    const card = this.cards.nth(index);
    const employeeName = (await card.locator('.promo-name').innerText()).trim();
    await card.click();
    return employeeName;
  }
}
