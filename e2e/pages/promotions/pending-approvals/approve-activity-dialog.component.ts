import { Page, Locator } from '@playwright/test';
import { DatePickerComponent } from './date-picker.component';

export class ApproveActivityDialogComponent {
  readonly root: Locator;
  readonly title: Locator;
  readonly cancelButton: Locator;
  readonly approveButton: Locator;
  readonly datePicker: DatePickerComponent;

  constructor(page: Page) {
    this.root = page.locator('app-approve-activity-dialog');
    this.title = this.root.getByRole('heading', { name: 'Approve activity' });
    this.cancelButton = this.root.getByRole('button', { name: 'Cancel', exact: true });
    this.approveButton = this.root.getByRole('button', { name: 'Approve', exact: true });
    this.datePicker = new DatePickerComponent(this.root.locator('app-date-picker'));
  }

  // Without a day, confirms with the date the dialog prefilled
  async approve(day?: number): Promise<void> {
    if (day !== undefined) {
      await this.datePicker.pickDay(day);
    }
    await this.approveButton.click();
  }
}
