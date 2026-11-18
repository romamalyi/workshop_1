import { Page, Locator } from '@playwright/test';

export class DetailsTabComponent {
  readonly personalSectionLabel: Locator;
  readonly employmentSectionLabel: Locator;
  readonly addPdpButton: Locator;
  readonly editPdpButton: Locator;
  readonly pdpInput: Locator;
  readonly savePdpButton: Locator;
  readonly pdpLink: Locator;
  readonly pdpSavedToast: Locator;

  constructor(page: Page) {
    this.personalSectionLabel = page.getByText('Personal', { exact: true });
    this.employmentSectionLabel = page.getByText('Employment', { exact: true });
    this.addPdpButton = page.getByRole('button', { name: 'Add PDP link' });
    this.editPdpButton = page.getByRole('button', { name: 'Edit PDP link' });
    this.pdpInput = page.getByRole('textbox', { name: 'PDP link' });
    this.savePdpButton = page.getByRole('button', { name: 'Save PDP link' });
    this.pdpLink = page.getByRole('link', { name: 'Open employee PDP Sheet in a new tab' });
    this.pdpSavedToast = page
      .getByRole('alert')
      .filter({ hasText: 'PDP link updated successfully.' });
  }

  async addPdpLink(url: string): Promise<void> {
    await this.addPdpButton.click();
    await this.pdpInput.fill(url);
    await this.savePdpButton.click();
  }

  async removePdpLink(): Promise<void> {
    await this.editPdpButton.click();
    await this.pdpInput.fill('');
    await this.savePdpButton.click();
  }

  async ensureNoPdpLink(): Promise<void> {
    await this.addPdpButton.or(this.editPdpButton).waitFor();
    if (await this.editPdpButton.isVisible()) {
      await this.removePdpLink();
      await this.addPdpButton.waitFor();
    }
  }
}
