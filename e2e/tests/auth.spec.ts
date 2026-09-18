import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-01 — Authentication & Identity
 * Issue: #26
 */

test.describe('F-01 Authentication & Identity', () => {
  test.beforeEach(async ({ page }) => {
    // Clear any existing session state before each test
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F01-001: login page loads with expected elements', async ({ page }) => {
    await page.goto('/');

    // Verify the login page is present
    await expect(page.getByTestId('login-page')).toBeVisible();

    // Verify the login button is rendered
    await expect(page.getByTestId('login-button')).toBeVisible();

    // Verify the page title includes the app name
    await expect(page).toHaveTitle(/LMS|Leave Management/i);
  });

  test('E2E-F01-002: unauthenticated user is redirected to login', async ({ page }) => {
    // Attempt to access a protected route directly without authentication
    await page.goto('/dashboard');

    // The app must redirect to login or show the login page
    await expect(page.getByTestId('login-page')).toBeVisible({ timeout: 5000 });

    // Verify URL has changed to login or root
    expect(page.url()).toMatch(/\/login|\/$/);
  });

  test('E2E-F01-003: mock-login navigates to dashboard', async ({ page }) => {
    await page.goto('/');

    // Wait for login page to be ready
    await expect(page.getByTestId('login-page')).toBeVisible();

    // Trigger login flow via the login button
    await page.getByTestId('login-button').click();

    // After login (or mock-login in dev/staging), user should reach dashboard
    // Allow time for auth redirect to complete
    await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });

    // Verify we left the login page
    await expect(page.getByTestId('login-page')).not.toBeVisible({ timeout: 5000 });
  });

  test('E2E-F01-004: logout clears session and returns to login', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('login-page')).toBeVisible();

    // Perform login
    await page.getByTestId('login-button').click();
    await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });

    // Perform logout
    await expect(page.getByTestId('logout-button')).toBeVisible();
    await page.getByTestId('logout-button').click();

    // After logout, user should be returned to the login page
    await expect(page.getByTestId('login-page')).toBeVisible({ timeout: 5000 });

    // Verify session is cleared — navigating to protected route should redirect
    await page.goto('/dashboard');
    await expect(page.getByTestId('login-page')).toBeVisible({ timeout: 5000 });
  });
});
