import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-08 — Approval Workflow
 * Issue: #65
 *
 * Tests cover the approval workflow API endpoints and UI dashboard.
 * API tests use Playwright's `request` context.
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const MANAGER_TOKEN = 'Bearer test-manager-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

test.describe('F-08 Approval Workflow', () => {
  test('E2E-F08-001: GET /api/approvals/pending returns pending list for Manager', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/approvals/pending`, {
      headers: {
        Authorization: MANAGER_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    // Response must contain a data array of pending approvals
    expect(body).toHaveProperty('data');
    expect(Array.isArray(body.data)).toBe(true);
  });

  test('E2E-F08-002: GET /api/approvals/stats returns stats for Manager', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/approvals/stats`, {
      headers: {
        Authorization: MANAGER_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    // Response must contain a data object with stats fields
    expect(body).toHaveProperty('data');
    expect(typeof body.data).toBe('object');
  });

  test('E2E-F08-003: Employee gets 403 on /api/approvals/pending', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/approvals/pending`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(403);
  });

  test('E2E-F08-004: Approval dashboard page loads at /approvals route', async ({ page }) => {
    // Navigate to approvals dashboard
    await page.goto(`${BASE_URL}/approvals`);

    // Wait for the dashboard to render
    const dashboard = page.getByTestId('approval-dashboard');
    await expect(dashboard).toBeVisible({ timeout: 10000 });

    // Stats section should also be visible
    const stats = page.getByTestId('approval-stats');
    await expect(stats).toBeVisible({ timeout: 10000 });
  });

  test('E2E-F08-005: POST /api/approvals/{id}/escalate returns 404 for non-existent ID', async ({ request }) => {
    const nonExistentId = 'non-existent-approval-id-00000000';

    const response = await request.post(`${BASE_URL}/api/approvals/${nonExistentId}/escalate`, {
      headers: {
        Authorization: MANAGER_TOKEN,
        'Content-Type': 'application/json',
      },
      data: {
        reason: 'Escalating for test',
      },
    });

    expect(response.status()).toBe(404);
  });
});
