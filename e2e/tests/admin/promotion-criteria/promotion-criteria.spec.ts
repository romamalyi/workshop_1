import { test, expect } from '@playwright/test';
import { PromotionCriteriaPage } from '../../../pages/admin/promotion-criteria/promotion-criteria.page';

// Deleted criteria stay as inactive rows, so a fixed score would match leftovers from earlier runs
const uniqueScore = (): number => 1000 + (Date.now() % 100000);

test.describe('Promotion Criteria page', () => {
  let promotionCriteria: PromotionCriteriaPage;

  test.beforeEach(async ({ page }) => {
    promotionCriteria = new PromotionCriteriaPage(page);
    await promotionCriteria.goto();
  });

  test('Page shows the heading, add button and criteria list', async ({ page }) => {
    await expect(page).toHaveURL('/admin/promotion-criteria');
    await expect(promotionCriteria.heading).toBeVisible();
    await expect(promotionCriteria.addButton).toBeVisible();
    await expect(promotionCriteria.headerRow).toBeVisible();
    await expect(promotionCriteria.rows.first()).toBeVisible();
  });
});

test.describe('Promotion Criteria management', () => {
  const technology = 'Flutter';
  const level = 'Trainee Associate';
  const criteriaData = {
    technology,
    seniority: 'Trainee',
    subLevel: 'Associate',
    englishLevel: 'Intermediate',
    score: 15,
  } as const;
  let promotionCriteria: PromotionCriteriaPage;

  // Every test works on the same Flutter level, so they must not run in parallel workers
  test.describe.configure({ mode: 'default' });

  test.beforeEach(async ({ page }) => {
    promotionCriteria = new PromotionCriteriaPage(page);
    await promotionCriteria.goto();
    await promotionCriteria.filterByTechnology(technology);
    await promotionCriteria.deleteActiveCriteriaIfPresent(level);
  });

  test.afterEach(async () => {
    if (await promotionCriteria.modal.cancelButton.isVisible()) {
      await promotionCriteria.modal.cancelButton.click();
    }
    await promotionCriteria.deleteActiveCriteriaIfPresent(level);
  });

  test('Add Criteria button creates a new criteria', async () => {
    await promotionCriteria.createCriteria(criteriaData);

    const row = promotionCriteria.rowForLevel(level);
    await expect(row).toBeVisible();
    await expect(row).toContainText('Intermediate');
    await expect(row).toContainText('15 pts');
  });

  test('Technology filter lists a criteria only under its own technology', async () => {
    // A score no real criteria uses, so the row can't be confused with existing data
    const score = uniqueScore();
    await promotionCriteria.createCriteria({ ...criteriaData, score });
    const row = promotionCriteria.rowForLevel(level).filter({ hasText: `${score} pts` });
    await expect(row).toBeVisible();

    const otherChip = promotionCriteria.technologyChips.filter({ hasNotText: technology }).first();
    await otherChip.click();
    await expect(promotionCriteria.activeTechnologyChip).toHaveCount(1);
    await expect(promotionCriteria.activeTechnologyChip).toHaveText(await otherChip.innerText());
    await expect(row).toHaveCount(0);

    await promotionCriteria.filterByTechnology(technology);
    await expect(promotionCriteria.activeTechnologyChip).toHaveText(technology);
    await expect(row).toBeVisible();
  });

  test('Cancel button closes the modal without creating a criteria', async () => {
    await promotionCriteria.openAddModal();
    await promotionCriteria.fillCriteriaForm({ seniority: criteriaData.seniority, subLevel: criteriaData.subLevel });
    await promotionCriteria.modal.cancelButton.click();

    await expect(promotionCriteria.modal.saveButton).toBeHidden();
    await expect(promotionCriteria.rowForLevel(level)).toHaveCount(0);
  });

  test.describe('with an existing criteria', () => {
    test.beforeEach(async () => {
      await promotionCriteria.createCriteria(criteriaData);
      await expect(promotionCriteria.modal.saveButton).toBeHidden();
      await expect(promotionCriteria.rowForLevel(level)).toBeVisible();
    });

    test('Add Criteria button shows a duplicate error for an existing level', async () => {
      await promotionCriteria.createCriteria(criteriaData);

      await expect(promotionCriteria.modal.duplicateError).toBeVisible();
      await expect(promotionCriteria.rowForLevel(level)).toHaveCount(1);
    });

    test('Edit button updates a criteria in place', async () => {
      await promotionCriteria.editCriteria(level, 20);

      await expect(promotionCriteria.rowForLevel(level)).toHaveCount(1);
      await expect(promotionCriteria.rowForLevel(level)).toContainText('20 pts');
    });

    test('Edit button with Preserve history keeps the old version as inactive and adds a new one', async () => {
      // Earlier runs leave inactive rows, so give this version a score no earlier run used
      const oldScore = uniqueScore();
      await promotionCriteria.editCriteria(level, oldScore);
      await expect(promotionCriteria.activeRowForLevel(level)).toContainText(`${oldScore} pts`);

      await promotionCriteria.editCriteria(level, 20, true);

      await expect(promotionCriteria.activeRowForLevel(level)).toContainText('20 pts');
      await promotionCriteria.showInactiveCheckbox.check();
      await expect(
        promotionCriteria.inactiveRows
          .filter({ has: promotionCriteria.page.locator('.pc-col-level', { hasText: level }) })
          .filter({ hasText: `${oldScore} pts` }),
      ).toHaveCount(1);
    });

    test('Delete button deactivates a criteria and Show inactive checkbox reveals it', async () => {
      await promotionCriteria.deleteCriteria(level);
      await expect(promotionCriteria.rowForLevel(level)).toHaveCount(0);

      await promotionCriteria.showInactiveCheckbox.check();
      await expect(promotionCriteria.inactiveBadge(level).first()).toBeVisible();

      await promotionCriteria.showInactiveCheckbox.uncheck();
      await expect(promotionCriteria.rowForLevel(level)).toHaveCount(0);
    });

    test('Reactivate button restores an inactive criteria', async () => {
      const score = uniqueScore();
      await promotionCriteria.editCriteria(level, score);
      await expect(promotionCriteria.rowForLevel(level)).toContainText(`${score} pts`);
      await promotionCriteria.deleteCriteria(level);
      await promotionCriteria.showInactiveCheckbox.check();
      await expect(promotionCriteria.inactiveBadge(level).first()).toBeVisible();

      await promotionCriteria.reactivateCriteria(level, score);

      await expect(promotionCriteria.activeRowForLevel(level)).toContainText(`${score} pts`);
    });
  });
});
