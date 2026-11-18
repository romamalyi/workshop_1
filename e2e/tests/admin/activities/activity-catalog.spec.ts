import { test, expect } from '@playwright/test';
import { ActivityCatalogPage } from '../../../pages/admin/activities/activity-catalog.page';

test.describe('Activity Catalog page', () => {
  let catalog: ActivityCatalogPage;

  test.beforeEach(async ({ page }) => {
    catalog = new ActivityCatalogPage(page);
    await catalog.goto();
  });

  test('Page loads with the heading, add button and activities', async ({ page }) => {
    await expect(page).toHaveURL('/admin/activities');
    await expect(catalog.heading).toBeVisible();
    await expect(catalog.addButton).toBeVisible();
    await expect(catalog.rows.first()).toBeVisible();
  });

  test("Filtering by category shows only that category's activities", async () => {
    await catalog.filterByCategory('Self-courses');

    await expect(catalog.categoryChips.filter({ hasText: 'Self-courses' })).toHaveClass(/active/);
    await expect(catalog.sectionTitled('Self-courses')).toBeVisible();
    await expect(catalog.sectionTitled('Mentorship')).toHaveCount(0);
  });

  test('Clicking a section header collapses and expands the group', async () => {
    const section = catalog.sections.first();
    const header = catalog.sectionHeader(section);
    const arrow = catalog.sectionArrow(section);
    const firstRow = section.locator(catalog.rows).first();

    await expect(arrow).toHaveText('▼');
    await expect(firstRow).toBeVisible();

    await header.click();
    await expect(arrow).toHaveText('▶');
    await expect(firstRow).toBeHidden();

    await header.click();
    await expect(arrow).toHaveText('▼');
    await expect(firstRow).toBeVisible();
  });
});

test.describe('Activity management', () => {
  const activityData = { category: 'Certifications', period: '6 months', technologyIndex: 1, points: 10 } as const;
  let catalog: ActivityCatalogPage;
  let name: string;

  test.beforeEach(async ({ page }) => {
    catalog = new ActivityCatalogPage(page);
    name = `Test activity ${Math.floor(10000 + Math.random() * 90000)}`;
    await catalog.goto();
  });

  test.afterEach(async () => {
    if (await catalog.cancelButton.isVisible()) {
      await catalog.cancelButton.click();
    }
    if ((await catalog.activeRowNamed(name).count()) > 0) {
      await catalog.deleteActivity(name);
      await expect(catalog.activeRowNamed(name)).toHaveCount(0);
    }
  });

  test('Add Activity button creates a new activity', async () => {
    await catalog.createActivity({ ...activityData, name });

    await expect(catalog.rowNamed(name)).toBeVisible();
    await expect(catalog.rowNamed(name)).toContainText('6 months');
    await expect(catalog.rowNamed(name)).toContainText('10 pts');
  });

  test('Creating an activity with an existing name shows an error banner', async () => {
    await catalog.createActivity({ ...activityData, name });
    await expect(catalog.rowNamed(name)).toBeVisible();

    await catalog.createActivity({ ...activityData, name });

    await expect(catalog.duplicateNameError(name)).toBeVisible();
    await expect(catalog.rowNamed(name)).toHaveCount(1);
  });

  test('Edit button updates an activity in place', async () => {
    const editedName = `${name} edited`;

    await catalog.createActivity({ ...activityData, name });
    await expect(catalog.rowNamed(name)).toBeVisible();

    await catalog.editActivity(name, { name: editedName, points: 20 });

    await expect(catalog.rowNamed(editedName)).toBeVisible();
    await expect(catalog.rowNamed(editedName)).toContainText('20 pts');
    // the edit is saved, so the cleanup must find the renamed row
    name = editedName;
  });

  test('Edit with "Preserve history" keeps the old version as inactive and adds a new one', async () => {
    await catalog.createActivity({ ...activityData, name });
    await expect(catalog.rowNamed(name)).toBeVisible();

    await catalog.editActivity(name, { name, points: 20, preserveHistory: true });

    await expect(catalog.activeRowNamed(name)).toContainText('20 pts');
    await catalog.showInactiveCheckbox.check();
    await expect(catalog.inactiveRows.filter({ hasText: name })).toContainText('10 pts');
  });

  test('Delete button deactivates an activity', async () => {
    await catalog.createActivity({ ...activityData, name });
    await expect(catalog.rowNamed(name)).toBeVisible();

    await catalog.deleteActivity(name);
    await expect(catalog.rowNamed(name)).toBeHidden();

    await catalog.showInactiveCheckbox.check();
    await expect(catalog.inactiveBadge(name)).toBeVisible();
  });

  test('Reactivate button restores an inactive activity', async () => {
    await catalog.createActivity({ ...activityData, name });
    await catalog.deleteActivity(name);
    await catalog.showInactiveCheckbox.check();
    await expect(catalog.inactiveBadge(name)).toBeVisible();

    await catalog.reactivateActivity(name);

    await expect(catalog.inactiveBadge(name)).toBeHidden();
  });
});
