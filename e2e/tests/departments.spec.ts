import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-03 — Department Management
 * Issue: #27
 */

// Helper: perform mock login and navigate to departments page
async function loginAndGoToDepartments(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/departments');
}

test.describe('F-03 Department Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F03-001: department list page loads and displays table', async ({ page }) => {
    await loginAndGoToDepartments(page);

    // Department list container must be visible
    await expect(page.getByTestId('department-list')).toBeVisible({ timeout: 10000 });

    // Add-department button must be available for HR Admin
    await expect(page.getByTestId('add-department-btn')).toBeVisible();
  });

  test('E2E-F03-002: create department flow — dialog opens, form submits, new row appears', async ({ page }) => {
    await loginAndGoToDepartments(page);
    await expect(page.getByTestId('department-list')).toBeVisible({ timeout: 10000 });

    // Open the add-department dialog
    await page.getByTestId('add-department-btn').click();
    await expect(page.getByTestId('department-form-dialog')).toBeVisible();

    // Fill in the department name
    const uniqueName = `Test Department ${Date.now()}`;
    await page.getByTestId('department-name-input').fill(uniqueName);

    // Submit the form
    await page.getByTestId('submit-btn').click();

    // Dialog should close after successful submission
    await expect(page.getByTestId('department-form-dialog')).not.toBeVisible({ timeout: 5000 });

    // New department should appear in the list
    await expect(page.getByTestId('department-list')).toContainText(uniqueName);
  });

  test('E2E-F03-003: edit department — existing department name can be updated', async ({ page }) => {
    await loginAndGoToDepartments(page);
    await expect(page.getByTestId('department-list')).toBeVisible({ timeout: 10000 });

    // Create a department to edit
    await page.getByTestId('add-department-btn').click();
    await expect(page.getByTestId('department-form-dialog')).toBeVisible();
    const originalName = `Edit Me ${Date.now()}`;
    await page.getByTestId('department-name-input').fill(originalName);
    await page.getByTestId('submit-btn').click();
    await expect(page.getByTestId('department-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('department-list')).toContainText(originalName);

    // Find the edit button for the newly created department row
    const row = page.getByTestId('department-list').locator(`text=${originalName}`).locator('..');
    const editBtn = row.getByRole('button', { name: /edit/i });
    await editBtn.click();

    // Edit dialog should open with existing data
    await expect(page.getByTestId('department-form-dialog')).toBeVisible();
    const updatedName = `Updated ${Date.now()}`;
    await page.getByTestId('department-name-input').fill(updatedName);
    await page.getByTestId('submit-btn').click();

    // Dialog should close and updated name appears
    await expect(page.getByTestId('department-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('department-list')).toContainText(updatedName);
  });

  test('E2E-F03-004: delete department — department is removed from the list', async ({ page }) => {
    await loginAndGoToDepartments(page);
    await expect(page.getByTestId('department-list')).toBeVisible({ timeout: 10000 });

    // Create a department to delete
    await page.getByTestId('add-department-btn').click();
    await expect(page.getByTestId('department-form-dialog')).toBeVisible();
    const deptName = `Delete Me ${Date.now()}`;
    await page.getByTestId('department-name-input').fill(deptName);
    await page.getByTestId('submit-btn').click();
    await expect(page.getByTestId('department-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('department-list')).toContainText(deptName);

    // Find delete button for the created department
    const row = page.getByTestId('department-list').locator(`text=${deptName}`).locator('..');
    const deleteBtn = row.getByTestId('delete-btn');
    await deleteBtn.click();

    // Handle confirmation dialog if present
    const confirmBtn = page.getByRole('button', { name: /confirm|yes|delete/i });
    if (await confirmBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
      await confirmBtn.click();
    }

    // Department should no longer appear in the list
    await expect(page.getByTestId('department-list')).not.toContainText(deptName, { timeout: 5000 });
  });
});
