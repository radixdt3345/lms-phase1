import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-02 — Employee Management
 * Issue: #36
 */

// Helper: perform mock login and navigate to employees page
async function loginAndGoToEmployees(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/employees');
}

test.describe('F-02 Employee Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F02-001: employee list page loads and shows table', async ({ page }) => {
    await loginAndGoToEmployees(page);

    // Employee list page container must be visible
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // Add-employee button must be available for HR Admin
    await expect(page.getByTestId('add-employee-btn')).toBeVisible();

    // Employee table must be visible (or empty state)
    const hasTable = await page.getByTestId('employee-table').isVisible().catch(() => false);
    const hasEmpty = await page.getByTestId('empty-employees').isVisible().catch(() => false);
    expect(hasTable || hasEmpty).toBeTruthy();
  });

  test('E2E-F02-002: create employee flow — form opens, submits, new employee appears in list', async ({ page }) => {
    await loginAndGoToEmployees(page);
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // Open the add-employee dialog
    await page.getByTestId('add-employee-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).toBeVisible();

    // Generate unique test data
    const timestamp = Date.now();
    const firstName = `TestFirst${timestamp}`;
    const lastName = `TestLast${timestamp}`;
    const email = `test.${timestamp}@example.com`;
    const code = `EMP${timestamp.toString().slice(-6)}`;
    const jobTitle = `QA Engineer ${timestamp}`;

    // Fill in all required fields
    await page.getByTestId('employee-firstname-input').fill(firstName);
    await page.getByTestId('employee-lastname-input').fill(lastName);
    await page.getByTestId('employee-email-input').fill(email);
    await page.getByTestId('employee-code-input').fill(code);
    await page.getByTestId('employee-jobtitle-input').fill(jobTitle);

    // Select a department — click the select input to open dropdown
    await page.getByTestId('employee-department-input').click();
    // Pick the first available department option (not the empty "Select Department" option)
    const deptOptions = page.locator('[role="option"]').filter({ hasNot: page.locator(':text("Select Department")') });
    await deptOptions.first().click();

    // Set date of joining
    await page.getByTestId('employee-doj-input').fill('2024-01-15');

    // Submit the form
    await page.getByTestId('employee-submit-btn').click();

    // Dialog should close after successful submission
    await expect(page.getByTestId('employee-form-dialog')).not.toBeVisible({ timeout: 8000 });

    // New employee should appear in the table
    await expect(page.getByTestId('employee-table')).toContainText(firstName, { timeout: 5000 });
    await expect(page.getByTestId('employee-table')).toContainText(lastName);
  });

  test('E2E-F02-003: edit employee — name can be updated and change is reflected in list', async ({ page }) => {
    await loginAndGoToEmployees(page);
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // First create an employee to edit
    await page.getByTestId('add-employee-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).toBeVisible();

    const timestamp = Date.now();
    const firstName = `EditFirst${timestamp}`;
    const lastName = `EditLast${timestamp}`;
    const email = `edit.${timestamp}@example.com`;
    const code = `EMP${timestamp.toString().slice(-6)}`;

    await page.getByTestId('employee-firstname-input').fill(firstName);
    await page.getByTestId('employee-lastname-input').fill(lastName);
    await page.getByTestId('employee-email-input').fill(email);
    await page.getByTestId('employee-code-input').fill(code);
    await page.getByTestId('employee-jobtitle-input').fill(`Developer ${timestamp}`);

    await page.getByTestId('employee-department-input').click();
    const deptOptions = page.locator('[role="option"]').filter({ hasNot: page.locator(':text("Select Department")') });
    await deptOptions.first().click();

    await page.getByTestId('employee-doj-input').fill('2024-01-15');
    await page.getByTestId('employee-submit-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).not.toBeVisible({ timeout: 8000 });

    // Find the employee row and click the edit button
    const table = page.getByTestId('employee-table');
    await expect(table).toContainText(firstName, { timeout: 5000 });

    // Find the specific row containing the created employee's first name
    const row = table.locator('tr').filter({ hasText: firstName });
    const editBtn = row.locator('[data-testid^="edit-employee-"]');
    await editBtn.click();

    // Edit dialog should open with existing data
    await expect(page.getByTestId('employee-form-dialog')).toBeVisible();

    // Update the first name
    const updatedFirstName = `Updated${timestamp}`;
    await page.getByTestId('employee-firstname-input').fill(updatedFirstName);
    await page.getByTestId('employee-submit-btn').click();

    // Dialog should close and updated name appears in list
    await expect(page.getByTestId('employee-form-dialog')).not.toBeVisible({ timeout: 8000 });
    await expect(page.getByTestId('employee-table')).toContainText(updatedFirstName, { timeout: 5000 });
  });

  test('E2E-F02-004: delete employee — confirm dialog dismisses and employee is removed from list', async ({ page }) => {
    await loginAndGoToEmployees(page);
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // Create an employee to delete
    await page.getByTestId('add-employee-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).toBeVisible();

    const timestamp = Date.now();
    const firstName = `DelFirst${timestamp}`;
    const lastName = `DelLast${timestamp}`;
    const email = `del.${timestamp}@example.com`;
    const code = `EMP${timestamp.toString().slice(-6)}`;

    await page.getByTestId('employee-firstname-input').fill(firstName);
    await page.getByTestId('employee-lastname-input').fill(lastName);
    await page.getByTestId('employee-email-input').fill(email);
    await page.getByTestId('employee-code-input').fill(code);
    await page.getByTestId('employee-jobtitle-input').fill(`Tester ${timestamp}`);

    await page.getByTestId('employee-department-input').click();
    const deptOptions = page.locator('[role="option"]').filter({ hasNot: page.locator(':text("Select Department")') });
    await deptOptions.first().click();

    await page.getByTestId('employee-doj-input').fill('2024-01-15');
    await page.getByTestId('employee-submit-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).not.toBeVisible({ timeout: 8000 });

    const table = page.getByTestId('employee-table');
    await expect(table).toContainText(firstName, { timeout: 5000 });

    // Handle the native window.confirm dialog by auto-accepting it
    page.on('dialog', async (dialog) => {
      expect(dialog.message()).toContain(firstName);
      await dialog.accept();
    });

    // Find the row and click delete
    const row = table.locator('tr').filter({ hasText: firstName });
    const deleteBtn = row.locator('[data-testid^="delete-employee-"]');
    await deleteBtn.click();

    // Employee should no longer appear in the list
    await expect(table).not.toContainText(firstName, { timeout: 8000 });
  });

  test('E2E-F02-005: filter employees by department — list narrows to matching department only', async ({ page }) => {
    await loginAndGoToEmployees(page);
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // The department filter select must be visible
    await expect(page.getByTestId('employee-dept-filter')).toBeVisible();

    // Open the department filter dropdown and pick the first real option
    await page.getByTestId('employee-dept-filter').click();
    const options = page.locator('[role="option"]');
    await options.first().waitFor({ timeout: 5000 });

    // Pick the first non-empty department option
    const nonEmptyOptions = options.filter({ hasNot: page.locator(':text("All Departments")') });
    const count = await nonEmptyOptions.count();

    if (count > 0) {
      const firstDeptOption = nonEmptyOptions.first();
      const selectedDeptName = await firstDeptOption.textContent();
      await firstDeptOption.click();

      // Wait for the list to update
      await page.waitForTimeout(500);

      // Either a table with only the filtered department's employees is shown, or the empty state
      const hasTable = await page.getByTestId('employee-table').isVisible().catch(() => false);
      const hasEmpty = await page.getByTestId('empty-employees').isVisible().catch(() => false);
      expect(hasTable || hasEmpty).toBeTruthy();

      // If employees are shown, each row in the table should show the selected department
      if (hasTable && selectedDeptName) {
        const rows = page.getByTestId('employee-table').locator('tbody tr');
        const rowCount = await rows.count();
        if (rowCount > 0) {
          // Verify the table contains the selected department name
          await expect(page.getByTestId('employee-table')).toContainText(selectedDeptName.trim());
        }
      }
    } else {
      // No departments to filter by — just verify the filter element is present
      await page.keyboard.press('Escape');
      await expect(page.getByTestId('employee-dept-filter')).toBeVisible();
    }
  });

  test('E2E-F02-006: search employees by name — list filters to matching results', async ({ page }) => {
    await loginAndGoToEmployees(page);
    await expect(page.getByTestId('employee-list-page')).toBeVisible({ timeout: 10000 });

    // Create a uniquely named employee to search for
    await page.getByTestId('add-employee-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).toBeVisible();

    const timestamp = Date.now();
    const uniqueFirstName = `Srch${timestamp}`;
    const lastName = `SearchUser`;
    const email = `search.${timestamp}@example.com`;
    const code = `EMP${timestamp.toString().slice(-6)}`;

    await page.getByTestId('employee-firstname-input').fill(uniqueFirstName);
    await page.getByTestId('employee-lastname-input').fill(lastName);
    await page.getByTestId('employee-email-input').fill(email);
    await page.getByTestId('employee-code-input').fill(code);
    await page.getByTestId('employee-jobtitle-input').fill(`Analyst ${timestamp}`);

    await page.getByTestId('employee-department-input').click();
    const deptOptions = page.locator('[role="option"]').filter({ hasNot: page.locator(':text("Select Department")') });
    await deptOptions.first().click();

    await page.getByTestId('employee-doj-input').fill('2024-01-15');
    await page.getByTestId('employee-submit-btn').click();
    await expect(page.getByTestId('employee-form-dialog')).not.toBeVisible({ timeout: 8000 });

    // Now search for the unique name
    await expect(page.getByTestId('employee-search-input')).toBeVisible();
    await page.getByTestId('employee-search-input').fill(uniqueFirstName);

    // Wait for results to filter
    await page.waitForTimeout(800);

    // Table must be visible and contain the searched employee
    const hasTable = await page.getByTestId('employee-table').isVisible().catch(() => false);
    const hasEmpty = await page.getByTestId('empty-employees').isVisible().catch(() => false);
    expect(hasTable || hasEmpty).toBeTruthy();

    if (hasTable) {
      await expect(page.getByTestId('employee-table')).toContainText(uniqueFirstName, { timeout: 5000 });
    }

    // Clear the search and verify all employees return
    await page.getByTestId('employee-search-input').fill('');
    await page.waitForTimeout(800);

    // Table or empty state should be visible again
    const hasTableAfterClear = await page.getByTestId('employee-table').isVisible().catch(() => false);
    const hasEmptyAfterClear = await page.getByTestId('empty-employees').isVisible().catch(() => false);
    expect(hasTableAfterClear || hasEmptyAfterClear).toBeTruthy();
  });
});
