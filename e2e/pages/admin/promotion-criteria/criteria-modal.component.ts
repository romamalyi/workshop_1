import { Page, Locator } from '@playwright/test';

export class CriteriaModalComponent {
  readonly technologySelect: Locator;
  readonly senioritySelect: Locator;
  readonly subLevelSelect: Locator;
  readonly englishLevelSelect: Locator;
  readonly scoreInput: Locator;
  readonly preserveHistoryCheckbox: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;
  readonly duplicateError: Locator;

  constructor(page: Page) {
    const root = page.locator('.modal-box--wide');
    // The modal's labels have no for, so controls are found through their field wrapper
    const field = (label: string) => root.locator('.form-field').filter({ hasText: label });

    this.technologySelect = field('Technology').locator('select');
    this.senioritySelect = field('Seniority').locator('select');
    this.subLevelSelect = field('Sub-level').locator('select');
    this.englishLevelSelect = field('English Level').locator('select');
    this.scoreInput = field('Activities Score').locator('input');
    this.preserveHistoryCheckbox = root.getByRole('checkbox', { name: 'Preserve history' });
    this.saveButton = root.getByRole('button', { name: 'Save', exact: true });
    this.cancelButton = root.getByRole('button', { name: 'Cancel', exact: true });
    this.duplicateError = root.locator('.pc-duplicate-error');
  }
}
