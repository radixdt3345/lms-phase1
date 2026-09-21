import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-12 — Reports & CSV Export
 * Issue: #89
 *
 * Covers report job creation, listing, role-based access, and the /reports UI route.
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const HRADMIN_TOKEN = 'Bearer test-hradmin-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

test.describe('F-12 Reports & CSV Export', () => {
  test('E2E-F12-001: POST /api/reports/request creates a report job (HRAdmin)', async ({ request }) => {
    const response = await request.post(`${BASE_URL}/api/reports/request`, {
      headers: {
        Authorization: HRADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
      data: {
        reportType: 'leave-summary',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    expect(body).toHaveProperty('data');
  });

  test('E2E-F12-002: GET /api/reports returns user report jobs with data key (HRAdmin)', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/reports`, {
      headers: {
        Authorization: HRADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    expect(body).toHaveProperty('data');
  });

  test('E2E-F12-003: Employee cannot request reports (403)', async ({ request }) => {
    const response = await request.post(`${BASE_URL}/api/reports/request`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
      data: {
        reportType: 'leave-summary',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
      },
    });

    expect(response.status()).toBe(403);
  });

  test('E2E-F12-004: Reports page loads at /reports route and shows generate button', async ({ page }) => {
    // Navigate to the reports page
    await page.goto(`${BASE_URL}/reports`);

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Verify the reports page container is present
    const reportsPage = page.getByTestId('reports-page');
    await expect(reportsPage).toBeVisible();

    // Verify report form inputs are present
    await expect(page.getByTestId('report-type-select')).toBeVisible();
    await expect(page.getByTestId('start-date-input')).toBeVisible();
    await expect(page.getByTestId('end-date-input')).toBeVisible();

    // Verify the generate report button is present
    await expect(page.getByTestId('generate-report-btn')).toBeVisible();

    // Verify the reports table is present
    await expect(page.getByTestId('reports-table')).toBeVisible();
  });
});
