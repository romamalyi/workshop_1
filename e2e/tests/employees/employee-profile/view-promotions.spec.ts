import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../../pages/employees/employees-list.page';
import { EmployeeProfilePage } from '../../../pages/employees/employee-profile/employee-profile.page';
import { PromotionsListPage } from '../../../pages/promotions/promotions-list.page';

test('View promotions from an employee page lists their promotions in every status', async ({ page }) => {
  const promotionsList = new PromotionsListPage(page);
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  // A Sent promotion is the one the In Progress default used to hide.
  const employeeName = await promotionsList.findEmployeeWithSentAndOpenPromotions();

  await page.goto('/employees?hrManager=all');
  await employeesList.searchByName(employeeName);
  await employeesList.nameCells.getByText(employeeName, { exact: true }).first().click();
  await employeeProfile.viewPromotionsButton.click();

  await expect(page).toHaveURL(/[?&]tab=all(&|$)/);
  await expect(page).toHaveURL(/[?&]hrManager=all(&|$)/);
  await expect(promotionsList.activeFilterTab).toHaveText('All');
  await expect(promotionsList.sentCardsFor(employeeName).first()).toBeVisible();
  await expect(promotionsList.openCardsFor(employeeName).first()).toBeVisible();
});
