import { test, expect } from '@playwright/test';
import { PromotionsListPage } from '../../pages/promotions/promotions-list.page';

test('Promotions shows a loading state until the first results arrive', async ({ page }) => {
  const promotionsList = new PromotionsListPage(page);
  const release = await promotionsList.holdListRequests();
  await promotionsList.goto();

  await expect(promotionsList.initialLoading).toBeVisible();

  release();
  await expect(promotionsList.shownNames.first()).toBeVisible();
  await expect(promotionsList.initialLoading).toBeHidden();
});

test('A new search keeps the current list on screen, marked busy, until its results arrive', async ({ page }) => {
  const promotionsList = new PromotionsListPage(page);
  await promotionsList.goto();
  const shownName = (await promotionsList.shownNames.first().innerText()).trim();

  const release = await promotionsList.holdListRequests();
  await promotionsList.search('zz-no-such-employee');

  await expect(promotionsList.list).toHaveAttribute('aria-busy', 'true');
  await expect(promotionsList.shownNames.first()).toHaveText(shownName);
  await expect(promotionsList.initialLoading).toBeHidden();

  release();
  await expect(promotionsList.noPromotionsMessage).toBeVisible();
  await expect(promotionsList.list).toHaveAttribute('aria-busy', 'false');
});
