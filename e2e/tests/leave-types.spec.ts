import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-04 — Leave Type & Policy Management
 * Issue: #28
 */

// Helper: perform mock login and navigate to leave-types page
async function loginAndGoToLeaveTypes(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/leave-types');
}

test.describe('F-04 Leave Type & Policy Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F04-001: leave type list page loads and displays table', async ({ page }) => {
    await loginAndGoToLeaveTypes(page);

    // Leave type list container must be visible
    await expect(page.getByTestId('leave-type-list')).toBeVisible({ timeout: 10000 });

    // Add button must be available for HR Admin
    await expect(page.getByTestId('add-leave-type-btn')).toBeVisible();
  });

  test('E2E-F04-002: create leave type — dialog opens, form submits, new row appears', async ({ page }) => {
    await loginAndGoToLeaveTypes(page);
    await expect(page.getByTestId('leave-type-list')).toBeVisible({ timeout: 10000 });

    // Open the add leave type dialog
    await page.getByTestId('add-leave-type-btn').click();
    await expect(page.getByTestId('leave-type-form-dialog')).toBeVisible();

    // Fill in the leave type name
    const uniqueName = `Test Leave ${Date.now()}`;
    await page.getByTestId('leave-type-name-input').fill(uniqueName);

    // Submit the form
    await page.getByTestId('submit-btn').click();

    // Dialog should close after successful submission
    await expect(page.getByTestId('leave-type-form-dialog')).not.toBeVisible({ timeout: 5000 });

    // New leave type should appear in the list
    await expect(page.getByTestId('leave-type-list')).toContainText(uniqueName);
  });

  test('E2E-F04-003: toggle leave type active status — status is reflected in list', async ({ page }) => {
    await loginAndGoToLeaveTypes(page);
    await expect(page.getByTestId('leave-type-list')).toBeVisible({ timeout: 10000 });

    // Create a leave type to toggle
    await page.getByTestId('add-leave-type-btn').click();
    await expect(page.getByTestId('leave-type-form-dialog')).toBeVisible();
    const leaveName = `Toggle Me ${Date.now()}`;
    await page.getByTestId('leave-type-name-input').fill(leaveName);
    await page.getByTestId('submit-btn').click();
    await expect(page.getByTestId('leave-type-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('leave-type-list')).toContainText(leaveName);

    // Find and click the toggle/status button for this leave type
    const row = page.getByTestId('leave-type-list').locator(`text=${leaveName}`).locator('..');
    const toggleBtn = row.getByRole('button', { name: /deactivate|toggle|active/i });
    const initialStatus = await toggleBtn.textContent();
    await toggleBtn.click();

    // Verify that the toggle happened (status changed)
    await expect(toggleBtn).not.toHaveText(initialStatus ?? '', { timeout: 5000 });
  });

  test('E2E-F04-004: create leave type validation — empty name shows validation error', async ({ page }) => {
    await loginAndGoToLeaveTypes(page);
    await expect(page.getByTestId('leave-type-list')).toBeVisible({ timeout: 10000 });

    // Open the add leave type dialog
    await page.getByTestId('add-leave-type-btn').click();
    await expect(page.getByTestId('leave-type-form-dialog')).toBeVisible();

    // Submit without filling in any required fields
    await page.getByTestId('submit-btn').click();

    // Dialog must remain open (validation prevented submission)
    await expect(page.getByTestId('leave-type-form-dialog')).toBeVisible();

    // A validation error message must be displayed
    const errorMsg = page.locator('[role="alert"], .error, [data-testid*="error"]').first();
    await expect(errorMsg).toBeVisible({ timeout: 3000 });
  });
});
