import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../../../pages/employees/employees-list.page';
import { EmployeeProfilePage } from '../../../../pages/employees/employee-profile/employee-profile.page';

test('Activities tab opens and shows its Pending and Approved panels', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  await employeesList.goto();
  await employeesList.openFirstEmployee();
  await employeeProfile.openActivitiesTab();

  await expect(employeeProfile.activities.pendingPanelTitle).toBeVisible();
  await expect(employeeProfile.activities.approvedPanelTitle).toBeVisible();
  await expect(employeeProfile.activities.addActivityButton).toBeVisible();
  await expect(employeeProfile.activities.reportActivityButton).toBeHidden();
});

test('Adding an activity adds it to the Pending panel', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  await employeesList.goto();
  await employeesList.openFirstEmployee();
  await employeeProfile.openActivitiesTab();

  const activityName = await employeeProfile.activities.addFirstAvailableActivity();
  const matchingRows = employeeProfile.activities.pendingRowsNamed(activityName);

  await expect(matchingRows.first()).toBeVisible();
  const countAfterCreate = await matchingRows.count();

  await employeeProfile.activities.deleteNewestPendingActivity(activityName);

  await expect(async () => {
    expect(await matchingRows.count()).toBe(countAfterCreate - 1);
  }).toPass();
});
