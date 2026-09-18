import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-10 — Public Holiday Management
 * Issue: #29
 */

// Helper: perform mock login and navigate to public holidays page
async function loginAndGoToPublicHolidays(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/public-holidays');
}

test.describe('F-10 Public Holiday Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F10-001: public holiday page loads and displays the holidays table', async ({ page }) => {
    await loginAndGoToPublicHolidays(page);

    // Main page container must be visible
    await expect(page.getByTestId('public-holiday-page')).toBeVisible({ timeout: 10000 });

    // Year selector and navigation controls must be present
    await expect(page.getByTestId('year-selector')).toBeVisible();
    await expect(page.getByTestId('prev-year-btn')).toBeVisible();
    await expect(page.getByTestId('next-year-btn')).toBeVisible();

    // Holidays table must be rendered (even if empty)
    await expect(page.getByTestId('holidays-table')).toBeVisible();

    // Add holiday button must be available for HR Admin
    await expect(page.getByTestId('add-holiday-btn')).toBeVisible();
  });

  test('E2E-F10-002: add public holiday — dialog opens, form submits, new holiday appears in table', async ({ page }) => {
    await loginAndGoToPublicHolidays(page);
    await expect(page.getByTestId('public-holiday-page')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('holidays-table')).toBeVisible();

    // Open the add-holiday dialog
    await page.getByTestId('add-holiday-btn').click();
    await expect(page.getByTestId('holiday-form-dialog')).toBeVisible();

    // Fill in the holiday name
    const uniqueName = `Test Holiday ${Date.now()}`;
    await page.getByTestId('holiday-name-input').fill(uniqueName);

    // Set a date in the current year
    await page.getByTestId('holiday-date-input').fill('2026-03-15');

    // Set country code
    await page.getByTestId('holiday-country-input').fill('IN');

    // Submit the form
    await page.getByTestId('holiday-submit-btn').click();

    // Dialog should close after successful submission
    await expect(page.getByTestId('holiday-form-dialog')).not.toBeVisible({ timeout: 5000 });

    // New holiday should appear in the table
    await expect(page.getByTestId('holidays-table')).toContainText(uniqueName);
  });

  test('E2E-F10-003: edit public holiday — existing holiday details can be updated', async ({ page }) => {
    await loginAndGoToPublicHolidays(page);
    await expect(page.getByTestId('public-holiday-page')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('holidays-table')).toBeVisible();

    // Create a holiday to edit
    await page.getByTestId('add-holiday-btn').click();
    await expect(page.getByTestId('holiday-form-dialog')).toBeVisible();
    const originalName = `Edit Holiday ${Date.now()}`;
    await page.getByTestId('holiday-name-input').fill(originalName);
    await page.getByTestId('holiday-date-input').fill('2026-06-10');
    await page.getByTestId('holiday-submit-btn').click();
    await expect(page.getByTestId('holiday-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('holidays-table')).toContainText(originalName);

    // Locate the edit button for the newly created holiday row
    const row = page.getByTestId('holidays-table').locator(`text=${originalName}`).locator('..');
    const editBtn = row.getByRole('button').first();
    await editBtn.click();

    // Edit dialog should open with existing data pre-filled
    await expect(page.getByTestId('holiday-form-dialog')).toBeVisible();

    // Update the holiday name
    const updatedName = `Updated Holiday ${Date.now()}`;
    await page.getByTestId('holiday-name-input').fill(updatedName);
    await page.getByTestId('holiday-submit-btn').click();

    // Dialog should close and updated name appears in the table
    await expect(page.getByTestId('holiday-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('holidays-table')).toContainText(updatedName);
  });

  test('E2E-F10-004: delete public holiday — holiday is removed from the table', async ({ page }) => {
    await loginAndGoToPublicHolidays(page);
    await expect(page.getByTestId('public-holiday-page')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('holidays-table')).toBeVisible();

    // Create a holiday to delete
    await page.getByTestId('add-holiday-btn').click();
    await expect(page.getByTestId('holiday-form-dialog')).toBeVisible();
    const holidayName = `Delete Holiday ${Date.now()}`;
    await page.getByTestId('holiday-name-input').fill(holidayName);
    await page.getByTestId('holiday-date-input').fill('2026-09-01');
    await page.getByTestId('holiday-submit-btn').click();
    await expect(page.getByTestId('holiday-form-dialog')).not.toBeVisible({ timeout: 5000 });
    await expect(page.getByTestId('holidays-table')).toContainText(holidayName);

    // Locate the delete button for the created holiday row
    const row = page.getByTestId('holidays-table').locator(`text=${holidayName}`).locator('..');
    const deleteBtn = row.getByRole('button').last();

    // Intercept the browser confirm dialog and accept it
    page.on('dialog', async (dialog) => {
      await dialog.accept();
    });
    await deleteBtn.click();

    // Holiday should no longer appear in the table
    await expect(page.getByTestId('holidays-table')).not.toContainText(holidayName, { timeout: 5000 });
  });
});
