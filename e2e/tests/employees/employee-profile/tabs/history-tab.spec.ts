import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../../../pages/employees/employees-list.page';
import { EmployeeProfilePage } from '../../../../pages/employees/employee-profile/employee-profile.page';

test('History tab opens and shows its content', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  await employeesList.goto();
  await employeesList.openFirstEmployee();
  await employeeProfile.openHistoryTab();

  await expect(employeeProfile.history.content).toBeVisible();
});
