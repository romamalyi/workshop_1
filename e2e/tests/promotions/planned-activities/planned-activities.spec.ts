import { test, expect, Locator } from '@playwright/test';
import { PlannedActivitiesPage } from '../../../pages/promotions/planned-activities/planned-activities.page';
import { PromotionDetailPage } from '../../../pages/promotions/promotions-list/promotion-detail/promotion-detail.page';
import { PromotionsListPage } from '../../../pages/promotions/promotions-list/promotions-list.page';

test.describe.configure({ mode: 'default' });

let plannedEmployee: string;
let plannedActivity: string;
let plannedActivities: PlannedActivitiesPage;
let plannedRow: Locator;

test.beforeAll(async ({ browser }, testInfo) => {
  // beforeAll has no `page` fixture, so open a context with the project's own options
  const context = await browser.newContext(testInfo.project.use);
  try {
    const page = await context.newPage();
    const promotionsList = new PromotionsListPage(page);
    const promotion = new PromotionDetailPage(page);
    await promotionsList.goto();
    plannedEmployee = await promotionsList.openNthPromotion(testInfo.parallelIndex);
    plannedActivity = await promotion.planFirstAvailableActivity();
    await promotion.plannedRow(plannedActivity).waitFor();
  } finally {
    await context.close();
  }
});

test.afterAll(async ({ browser }, testInfo) => {
  if (!plannedActivity) return;
  const context = await browser.newContext(testInfo.project.use);
  try {
    const page = await context.newPage();
    const planned = new PlannedActivitiesPage(page);
    const promotion = new PromotionDetailPage(page);
    // A direct link to the promotion is lost in the login redirect, so open it from its row
    await planned.goto();
    await planned.hrFilter.resetToAll();
    await planned.rowEmployee(planned.rowFor(plannedEmployee, plannedActivity)).click();
    await promotion.removePlannedActivity(plannedActivity);
    await promotion.plannedRow(plannedActivity).waitFor({ state: 'detached' });
  } finally {
    await context.close();
  }
});

test.beforeEach(async ({ page }) => {
  plannedActivities = new PlannedActivitiesPage(page);
  await plannedActivities.goto();
  await plannedActivities.hrFilter.resetToAll();
  plannedRow = plannedActivities.rowFor(plannedEmployee, plannedActivity);
  await expect(plannedRow).toBeVisible();
});

test('Page opens at the planned URL and shows its main components', async ({ page }) => {
  await expect(page).toHaveURL(/\/promotions\/planned(\?|$)/);
  await expect(plannedActivities.sideRailLink).toHaveText(/^Planned activities\s*[1-9]\d*$/);
  await expect(plannedActivities.heading).toBeVisible();
  await expect(plannedActivities.subtitle).toBeVisible();
  await expect(plannedActivities.columnHeaders).toHaveText(['Employee', 'Activity', 'Category', 'Planned on', 'HR']);
});

test('Searching by employee name shows only their activities and adds the term to the URL', async () => {
  await plannedActivities.searchInput.fill(plannedEmployee);

  await expect.poll(() => plannedActivities.queryParamValues('searchTerm')).toEqual([plannedEmployee]);
  await expect(plannedRow).toBeVisible();
  await expect(plannedActivities.rowsOutsideEmployee(plannedEmployee)).toHaveCount(0);
});

test('Searching by activity name shows only matching activities', async () => {
  await plannedActivities.searchInput.fill(plannedActivity);

  await expect(plannedRow).toBeVisible();
  await expect(plannedActivities.rowsNotMatching(plannedActivity)).toHaveCount(0);
});

test('Selecting an HR manager shows only their employees and adds them to the URL and the filter badge', async () => {
  const rowWithHr = plannedActivities.firstRowWithHr;
  await expect(rowWithHr).toBeVisible();
  const hrName = (await plannedActivities.rowHr(rowWithHr).innerText()).trim();

  await plannedActivities.hrFilter.selectOnly(hrName);

  await expect.poll(() => plannedActivities.queryParamValues('hrManager')).toEqual([hrName]);
  await expect(plannedActivities.hrFilter.badge).toHaveText('1');
  await expect(plannedActivities.rows.first()).toBeVisible();
  await expect(plannedActivities.rowsOutsideHr(hrName)).toHaveCount(0);
});

test('Selecting a category shows only its activities and adds it to the URL and the filter badge', async () => {
  const category = (await plannedActivities.rowCategory(plannedRow).innerText()).trim();

  await plannedActivities.categoryFilter.selectOnly(category);

  await expect.poll(() => plannedActivities.queryParamValues('category')).toHaveLength(1);
  await expect(plannedActivities.categoryFilter.badge).toHaveText('1');
  await expect(plannedRow).toBeVisible();
  await expect(plannedActivities.rowsOutsideCategory(category)).toHaveCount(0);
});

test('Clicking the employee of a promotion activity opens their promotion', async ({ page }) => {
  await plannedActivities.rowEmployee(plannedRow).click();

  await expect(page).toHaveURL(/\/promotions\/\d+$/);
  await expect(new PromotionDetailPage(page).employeeName).toHaveText(plannedEmployee);
});

test('Saving a new planned date updates the row and shows a success toast', async () => {
  const dateBefore = (await plannedActivities.rowPlannedDate(plannedRow).innerText()).trim();

  await plannedActivities.rowEditButton(plannedRow).click();
  await expect(plannedActivities.plannedDateDialog.title).toBeVisible();
  await expect(plannedActivities.plannedDateDialog.activityName).toHaveText(plannedActivity);
  await plannedActivities.plannedDateDialog.saveInPreviousMonth(1);

  await expect(plannedActivities.plannedDateDialog.root).toBeHidden();
  await expect(plannedActivities.successToast).toContainText(`Updated the planned date of “${plannedActivity}”`);
  await expect(plannedActivities.rowPlannedDate(plannedRow)).not.toHaveText(dateBefore);
  await expect(plannedActivities.rowPlannedDate(plannedRow)).toHaveText(/^01 /);
});
