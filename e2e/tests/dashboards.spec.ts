import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-11 — Dashboards
 * Issue: #88
 *
 * Covers dashboard API endpoints (HRAdmin, Manager, Employee roles) and
 * the /dashboard UI route.
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const HRADMIN_TOKEN = 'Bearer test-hradmin-token';
const MANAGER_TOKEN = 'Bearer test-manager-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

test.describe('F-11 Dashboards', () => {
  test('E2E-F11-001: GET /api/dashboards/overview returns data for HRAdmin', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/dashboards/overview`, {
      headers: {
        Authorization: HRADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    expect(body).toHaveProperty('data');
  });

  test('E2E-F11-002: GET /api/dashboards/team-leave returns data for Manager', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/dashboards/team-leave`, {
      headers: {
        Authorization: MANAGER_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    expect(body).toHaveProperty('data');
  });

  test('E2E-F11-003: Employee gets 403 on /api/dashboards/overview', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/dashboards/overview`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(403);
  });

  test('E2E-F11-004: Dashboard page loads at /dashboard route and shows stat cards', async ({ page }) => {
    // Navigate to the dashboard page
    await page.goto(`${BASE_URL}/dashboard`);

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Verify the dashboard page container is present
    const dashboardPage = page.getByTestId('dashboard-page');
    await expect(dashboardPage).toBeVisible();

    // Verify overview stats section is present
    const overviewStats = page.getByTestId('overview-stats');
    await expect(overviewStats).toBeVisible();

    // Verify individual stat cards are present
    await expect(page.getByTestId('employee-count-card')).toBeVisible();
    await expect(page.getByTestId('on-leave-count-card')).toBeVisible();
    await expect(page.getByTestId('pending-approvals-card')).toBeVisible();
    await expect(page.getByTestId('comp-off-credits-card')).toBeVisible();
  });
});
