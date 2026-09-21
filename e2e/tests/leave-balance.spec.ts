import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-05 — Leave Balance Management
 * Issue: #47
 *
 * Covers: E2E-F05-001, E2E-F05-002, E2E-F05-003, E2E-F05-004
 *
 * UI tests use page.getByTestId() exclusively.
 * API-level tests use Playwright request context for backend actions
 * (following the seed-admin.spec.ts pattern).
 *
 * data-testids: leave-balance-page, leave-balance-table,
 *               adjust-balance-btn, adjust-dialog, balance-row-{leaveTypeId}
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const HR_ADMIN_TOKEN = 'Bearer test-hradmin-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

// Helper: log in as HRAdmin and navigate to the leave balance page
async function loginAsHRAdminAndGoToLeaveBalance(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/leave-balance');
}

test.describe('F-05 Leave Balance Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F05-001: Leave balance page loads for authenticated HRAdmin', async ({ page }) => {
    await loginAsHRAdminAndGoToLeaveBalance(page);

    // Main page container must be visible
    await expect(page.getByTestId('leave-balance-page')).toBeVisible({ timeout: 10000 });

    // Adjust balance button must be available for HRAdmin
    await expect(page.getByTestId('adjust-balance-btn')).toBeVisible();
  });

  test('E2E-F05-002: Leave balance table displays employee balances', async ({ page }) => {
    await loginAsHRAdminAndGoToLeaveBalance(page);

    await expect(page.getByTestId('leave-balance-page')).toBeVisible({ timeout: 10000 });

    // The balance table must be rendered
    await expect(page.getByTestId('leave-balance-table')).toBeVisible();

    // The table must contain at least one row of balance data
    const table = page.getByTestId('leave-balance-table');
    const rows = table.locator('tr');
    // Expect at least a header row plus one data row
    await expect(rows).toHaveCountGreaterThan(1);
  });

  test('E2E-F05-003: Adjust balance button opens dialog', async ({ page }) => {
    await loginAsHRAdminAndGoToLeaveBalance(page);

    await expect(page.getByTestId('leave-balance-page')).toBeVisible({ timeout: 10000 });
    await expect(page.getByTestId('leave-balance-table')).toBeVisible();

    // Click the adjust balance button
    await page.getByTestId('adjust-balance-btn').click();

    // Adjust dialog must open
    await expect(page.getByTestId('adjust-dialog')).toBeVisible({ timeout: 5000 });
  });

  test('E2E-F05-004: Balance adjustment persists and updates table via API', async ({ request }) => {
    // Use API-level request context to verify the adjustment endpoint
    // and that the new balance is reflected in the list response

    // Step 1: Retrieve current balances
    const listResponse = await request.get(`${BASE_URL}/api/leave-balances`, {
      headers: {
        Authorization: HR_ADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(listResponse.status()).toBe(200);
    const listBody = await listResponse.json();
    expect(listBody).toHaveProperty('data');

    // Step 2: Pick the first leave type from the response (or use a known test ID)
    const balances: Array<{ leaveTypeId: string; balance: number }> = listBody.data;
    expect(balances.length).toBeGreaterThan(0);

    const { leaveTypeId } = balances[0];
    const newBalance = 10;

    // Step 3: Submit a balance adjustment
    const adjustResponse = await request.post(`${BASE_URL}/api/leave-balances/adjust`, {
      headers: {
        Authorization: HR_ADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
      data: {
        leaveTypeId,
        balance: newBalance,
        reason: 'E2E test adjustment',
      },
    });

    // Adjustment must succeed (200 or 201)
    expect([200, 201]).toContain(adjustResponse.status());

    // Step 4: Re-fetch balances and verify the updated value
    const updatedListResponse = await request.get(`${BASE_URL}/api/leave-balances`, {
      headers: {
        Authorization: HR_ADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(updatedListResponse.status()).toBe(200);
    const updatedBody = await updatedListResponse.json();
    const updatedBalances: Array<{ leaveTypeId: string; balance: number }> = updatedBody.data;

    const updatedEntry = updatedBalances.find((b) => b.leaveTypeId === leaveTypeId);
    expect(updatedEntry).toBeDefined();
    expect(updatedEntry!.balance).toBe(newBalance);
  });
});
