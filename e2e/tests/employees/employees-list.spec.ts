import { test, expect } from '@playwright/test';
import { EmployeesListPage } from '../../pages/employees/employees-list.page';

test('Employees page loads with the employee table', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  await expect(page).toHaveURL('/employees');
  await expect(employeesList.table).toBeVisible();
  await expect(employeesList.nameColumnHeader).toBeVisible();
});

test('Seniority filter hides all employees when nothing is selected', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  await employeesList.deselectAllInFilter('Seniority');

  await expect(employeesList.noMatchesMessage).toBeVisible();
});

test('Technology filter hides all employees when nothing is selected', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  await employeesList.deselectAllInFilter('Technology');

  await expect(employeesList.noMatchesMessage).toBeVisible();
});

test('Project filter hides all employees when nothing is selected', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  await employeesList.deselectAllInFilter('Project');

  await expect(employeesList.noMatchesMessage).toBeVisible();
});

test('Seniority filter shows only employees with the selected seniority', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  const selectedValue = await employeesList.selectOnlyFirstOptionIn('Seniority');

  await expect(async () => {
    const cellTexts = await employeesList.seniorityCells.allTextContents();
    expect(cellTexts.length).toBeGreaterThan(0);
    expect(cellTexts.every((text) => text.trim() === selectedValue)).toBe(true);
  }).toPass();
});

test('Technology filter shows only employees with the selected technology', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  const selectedValue = await employeesList.selectOnlyFirstOptionIn('Technology');

  await expect(async () => {
    const cellTexts = await employeesList.technologyCells.allTextContents();
    expect(cellTexts.length).toBeGreaterThan(0);
    expect(cellTexts.every((text) => text.trim() === selectedValue)).toBe(true);
  }).toPass();
});

test('Project filter narrows the table to employees with the selected project', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  const selectedValue = await employeesList.selectOnlyFirstOptionIn('Project');

  await expect(async () => {
    const rowsInfo = await employeesList.projectTagsPerRow();
    expect(rowsInfo.length).toBeGreaterThan(0);
    expect(rowsInfo.every((row) => row.hasMore || row.tags.includes(selectedValue))).toBe(true);
  }).toPass();
});

test('Search field filters the table by employee name', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();

  const targetName = (await employeesList.nameCells.first().innerText()).trim();

  await employeesList.searchByName(targetName);

  await expect(async () => {
    const visibleNames = await employeesList.nameCells.allTextContents();
    expect(visibleNames.length).toBeGreaterThan(0);
    expect(visibleNames.every((name) => name.trim().toLowerCase().includes(targetName.toLowerCase()))).toBe(true);
  }).toPass();
});

test('A new search keeps the current list on screen, marked busy, until its results arrive', async ({ page }) => {
  const employeesList = new EmployeesListPage(page);
  await employeesList.goto();
  const shownName = (await employeesList.nameCells.first().innerText()).trim();

  const release = await employeesList.holdSearchRequests();
  await employeesList.searchByName('zz-no-such-employee');

  await expect(employeesList.table).toHaveAttribute('aria-busy', 'true');
  await expect(employeesList.nameCells.first()).toHaveText(shownName);
  await expect(employeesList.initialLoading).toBeHidden();

  release();
  await expect(employeesList.noMatchesMessage).toBeVisible();
  await expect(employeesList.table).toHaveAttribute('aria-busy', 'false');
});
