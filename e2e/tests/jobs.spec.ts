import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-15 — Background Jobs
 * Issue: #77
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const HRADMIN_TOKEN = 'Bearer test-hradmin-token';
const SUPERADMIN_TOKEN = 'Bearer test-superadmin-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

// Helper: perform mock login and navigate to jobs admin page
async function loginAndGoToJobs(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/jobs');
}

test.describe('F-15 Background Jobs', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F15-001: GET /api/jobs/status returns job list (API request context, HRAdmin)', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/jobs/status`, {
      headers: {
        Authorization: HRADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);
    const body = await response.json();

    // Response must contain a data property with an array of jobs
    expect(body).toHaveProperty('data');
    const jobs = body.data?.items ?? body.data;
    expect(Array.isArray(jobs)).toBe(true);
  });

  test('E2E-F15-002: POST /api/jobs/LeaveBalanceSyncJob/trigger triggers job (SuperAdmin)', async ({ request }) => {
    const response = await request.post(`${BASE_URL}/api/jobs/LeaveBalanceSyncJob/trigger`, {
      headers: {
        Authorization: SUPERADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    // Accepts 200 (triggered) or 202 (accepted async)
    expect([200, 202]).toContain(response.status());

    const body = await response.json();
    expect(body).toBeDefined();

    // Response should indicate the job was triggered
    const message = body?.message ?? body?.data?.message ?? '';
    const status = body?.status ?? body?.data?.status ?? '';
    // Either a message or status field should indicate success
    expect(message || status).toBeTruthy();
  });

  test('E2E-F15-003: Employee cannot access /api/jobs/status (403 via request context)', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/jobs/status`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    // Employee must be forbidden from accessing job status
    expect(response.status()).toBe(403);
  });

  test('E2E-F15-004: Job admin page loads at /jobs route', async ({ page }) => {
    await loginAndGoToJobs(page);

    // Job admin page container must be visible
    await expect(page.getByTestId('job-admin-page')).toBeVisible({ timeout: 10000 });

    // Job table must be rendered
    await expect(page.getByTestId('job-table')).toBeVisible();
  });
});
