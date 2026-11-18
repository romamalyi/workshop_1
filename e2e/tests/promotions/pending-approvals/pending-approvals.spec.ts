import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../../pages/employees/employees-list.page';
import { EmployeeProfilePage } from '../../../pages/employees/employee-profile/employee-profile.page';
import { PendingApprovalsPage } from '../../../pages/promotions/pending-approvals/pending-approvals.page';

let approvals: PendingApprovalsPage;
let createdActivityName: string;

// Serial so the setup runs once, not once per parallel worker
test.describe.configure({ mode: 'serial' });

test.beforeAll(async ({ browser }) => {
  // beforeAll has no `page` fixture, so open a context with the project's own options
  const context = await browser.newContext(test.info().project.use);
  const page = await context.newPage();
  const employeesList = new EmployeesListPage(page);
  const employeeProfile = new EmployeeProfilePage(page);

  await employeesList.goto();
  await employeesList.openFirstEmployee();
  await employeeProfile.openActivitiesTab();
  createdActivityName = await employeeProfile.activities.addFirstAvailableActivity();
  await expect(employeeProfile.activities.pendingRowsNamed(createdActivityName).first()).toBeVisible();

  await context.close();
});

test.beforeEach(async ({ page }) => {
  approvals = new PendingApprovalsPage(page);
  await approvals.goto();
  // A default HR filter would narrow the list, and the badge counts everyone
  await approvals.resetHrFilterToAll();
});

test('Page opens at the approvals URL and shows its main components', async ({ page }) => {
  await expect(page).toHaveURL('/promotions/approvals');
  await expect(approvals.heading).toBeVisible();
  await expect(approvals.subtitle).toBeVisible();
  await expect(approvals.hrFilter).toBeVisible();
  await expect(approvals.columnHeaders.first()).toBeVisible();
});

test('Sidebar badge reflects the number of pending activities', async () => {
  await expect(approvals.navBadge).toBeVisible();
  const badgeCount = Number(await approvals.navBadge.innerText());

  await expect(approvals.rows).toHaveCount(badgeCount);
  await approvals.expectPendingCount(badgeCount);
});

test('Selecting an HR manager adds them to the URL and the filter badge', async () => {
  await approvals.clearHrFilter();
  const option = approvals.hrOptions.first();
  const hrName = (await option.innerText()).trim();

  await option.getByRole('checkbox').check();

  await expect.poll(() => approvals.selectedHrManagersInUrl()).toEqual([hrName]);
  await expect(approvals.hrFilterBadge).toHaveText('1');
});

test('Unchecking all HR managers empties the list and checking them again restores it', async () => {
  await approvals.selectAllHr();
  await expect(approvals.rows.first()).toBeVisible();

  await approvals.hrFilterAllCheckbox.uncheck();
  await expect(approvals.emptyRow).toBeVisible();

  await approvals.hrFilterAllCheckbox.check();
  await expect(approvals.emptyRow).toBeHidden();
  await expect(approvals.rows.first()).toBeVisible();
});

test('Clicking the employee of a promotion activity opens the promotion', async ({ page }) => {
  await approvals.loadedPendingCount();
  test.skip((await approvals.promotionRows.count()) === 0, 'No pending activity is tied to a promotion');
  const row = approvals.promotionRows.first();

  await row.getByRole('link').click();

  await expect(page).toHaveURL(/\/promotions\/\d+$/);
});

test('Cancel in the approve dialog closes it and leaves the list unchanged', async () => {
  const countBefore = await approvals.loadedPendingCount();

  await approvals.openApproveDialog(createdActivityName);
  await expect(approvals.approveDialog.title).toBeVisible();
  await approvals.approveDialog.cancelButton.click();

  await expect(approvals.approveDialog.root).toBeHidden();
  await approvals.expectPendingCount(countBefore);
  await expect(approvals.rows).toHaveCount(countBefore);
});

test('Approving with a picked date removes the activity and decrements the counters', async () => {
  const countBefore = await approvals.loadedPendingCount();
  const sameNameBefore = await approvals.rowFor(createdActivityName).count();

  // beforeAll completes the activity today, so today's day is in the month the picker opens on
  await approvals.approveActivity(createdActivityName, new Date().getDate());

  await expect(approvals.approveDialog.root).toBeHidden();
  await expect(approvals.rowFor(createdActivityName)).toHaveCount(sameNameBefore - 1);
  await expect(approvals.rows).toHaveCount(countBefore - 1);
  await approvals.expectPendingCount(countBefore - 1);
});
