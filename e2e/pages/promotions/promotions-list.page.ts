import { Locator, Page } from '@playwright/test';

export class PromotionsListPage {
  readonly page: Page;
  readonly searchInput: Locator;
  readonly list: Locator;
  readonly shownNames: Locator;
  readonly initialLoading: Locator;
  readonly noPromotionsMessage: Locator;
  readonly activeFilterTab: Locator;

  constructor(page: Page) {
    this.page = page;
    this.searchInput = page.getByPlaceholder('Search employees…');
    this.list = page.locator('.promo-list');
    this.shownNames = this.list.locator('.promo-name');
    this.initialLoading = page.getByRole('status').filter({ hasText: 'Loading promotions…' });
    this.noPromotionsMessage = page.getByText('No promotions found', { exact: true });
    this.activeFilterTab = page.locator('.line-tab--active');
  }

  async goto(): Promise<void> {
    await this.page.goto('/promotions?tab=All&hrManager=all');
  }

  cardsFor(employeeName: string): Locator {
    return this.list
      .locator('.data-table__row')
      .filter({ has: this.page.locator('.promo-name').getByText(employeeName, { exact: true }) });
  }

  sentCardsFor(employeeName: string): Locator {
    return this.cardsFor(employeeName).filter({ has: this.page.locator('.badge--neutral') });
  }

  openCardsFor(employeeName: string): Locator {
    return this.cardsFor(employeeName).filter({ hasNot: this.page.locator('.badge--neutral') });
  }

  // A leaver's open promotion is soft-deleted, so an open one beside a Sent one means an active employee.
  async findEmployeeWithSentAndOpenPromotions(): Promise<string> {
    await this.page.goto('/promotions?tab=Sent&hrManager=all');
    await this.shownNames.first().waitFor();
    const names = [...new Set((await this.shownNames.allInnerTexts()).map((name) => name.trim()))];

    await this.page.goto('/promotions?tab=all&hrManager=all');
    for (const name of names) {
      await this.search(name);
      await this.sentCardsFor(name).first().waitFor();
      if ((await this.openCardsFor(name).count()) > 0) return name;
    }
    throw new Error('No employee has both a Sent and an open promotion.');
  }

  async search(term: string): Promise<void> {
    await this.searchInput.fill(term);
  }

  // Holds every list request until the returned release function is called.
  async holdListRequests(): Promise<() => void> {
    let release!: () => void;
    const released = new Promise<void>((resolve) => (release = resolve));
    await this.page.route(
      (url) => url.pathname === '/api/promotions',
      async (route) => {
        await released;
        await route.continue();
      },
    );
    return release;
  }
}
