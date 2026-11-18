import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../../../pages/employees/employees-list.page';
import { EmployeeProfilePage } from '../../../../pages/employees/employee-profile/employee-profile.page';

test('Details tab opens and shows employee info sections', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  await employeesList.goto();
  await employeesList.openFirstEmployee();
  await employeeProfile.openDetailsTab();

  await expect(employeeProfile.details.personalSectionLabel).toBeVisible();
  await expect(employeeProfile.details.employmentSectionLabel).toBeVisible();
});

test.describe('PDP Link Management', () => {
  let employeeProfile: EmployeeProfilePage;
  const pdpUrl = 'https://docs.google.com/spreadsheets/d/test';

  test.beforeEach(async ({ page }) => {
    const employeesList = new EmployeesListPage(page);
    employeeProfile = new EmployeeProfilePage(page);

    await employeesList.goto();
    await employeesList.openFirstEmployee();
    await employeeProfile.openDetailsTab();

    // Ensure clean state before test
    await employeeProfile.details.ensureNoPdpLink();
  });

  test('Adding a PDP link shows it on the Details tab', async () => {
    await employeeProfile.details.addPdpLink(pdpUrl);

    await expect(employeeProfile.details.pdpSavedToast).toBeVisible();
    await expect(employeeProfile.details.pdpLink).toHaveAttribute('href', pdpUrl);

    await employeeProfile.details.removePdpLink();
  });

  test('Removing a PDP link brings back the Add button', async () => {
    await employeeProfile.details.addPdpLink(pdpUrl);
    await expect(employeeProfile.details.editPdpButton).toBeVisible();

    await employeeProfile.details.removePdpLink();

    await expect(employeeProfile.details.addPdpButton).toBeVisible();
    await expect(employeeProfile.details.pdpLink).toBeHidden();
  });
});
