import { Locator } from '@playwright/test';

export class DatePickerComponent {
  readonly trigger: Locator;
  readonly panel: Locator;
  readonly monthCells: Locator;

  constructor(root: Locator) {
    this.trigger = root.locator('.dp-trigger');
    this.panel = root.locator('.dp-panel');
    this.monthCells = this.panel.locator('.dp-cell:not(.dp-cell--empty)');
  }

  dayCell(day: number): Locator {
    return this.monthCells.and(this.panel.getByRole('button', { name: String(day), exact: true }));
  }

  // The panel starts closed; picks a day in the month it opens on
  async pickDay(day: number): Promise<void> {
    await this.trigger.click();
    await this.dayCell(day).click();
  }
}
