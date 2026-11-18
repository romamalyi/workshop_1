import { Page, Locator } from '@playwright/test';

export class HistoryTabComponent {
  readonly content: Locator;

  constructor(page: Page) {
    // Matches either the timeline summary container or the empty-state message, whichever this employee has.
    this.content = page.getByText('No history records.', { exact: true }).or(page.locator('.history-summary'));
  }
}
