import { Locator } from '@playwright/test';

export class ActivityPickerModalComponent {
  readonly availableItems: Locator;
  readonly planButton: Locator;

  constructor(root: Locator) {
    // An activity already used in the promotion stays listed, but can't be picked
    this.availableItems = root.locator('.picker-item:not(.disabled)');
    // The label carries the basket size: "Plan 2 activities"
    this.planButton = root.locator('.picker-actions').getByRole('button', { name: /^Plan\b/ });
  }

  async planFirstAvailable(): Promise<string> {
    const item = this.availableItems.first();
    const text = await item.locator('.picker-item__name').innerText();
    const activityName = text.split('·')[0].trim();
    await item.click();
    await this.planButton.click();
    return activityName;
  }
}
