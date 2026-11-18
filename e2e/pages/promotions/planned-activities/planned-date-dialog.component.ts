import { Locator, expect } from '@playwright/test';
import { DatePickerComponent } from '../pending-approvals/date-picker.component';

export class PlannedDateDialogComponent {
  readonly root: Locator;
  readonly title: Locator;
  readonly activityName: Locator;
  readonly datePicker: DatePickerComponent;
  readonly saveButton: Locator;

  constructor(root: Locator) {
    this.root = root;
    this.title = root.getByRole('heading', { name: 'Edit planned date' });
    this.activityName = root.locator('.modal-body');
    this.datePicker = new DatePickerComponent(root.locator('app-date-picker'));
    this.saveButton = root.getByRole('button', { name: 'Save', exact: true });
  }

  async saveInPreviousMonth(day: number): Promise<void> {
    const monthLabel = this.datePicker.panel.locator('.dp-month-label');
    await this.datePicker.trigger.click();
    const openedOn = await monthLabel.innerText();
    await this.datePicker.panel.locator('.dp-nav-btn').first().click();
    await expect(monthLabel).not.toHaveText(openedOn);
    await this.datePicker.dayCell(day).click();
    await this.saveButton.click();
  }
}
