import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-09 — Notifications & Email
 * Issue: #71
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

// Helper: perform mock login and navigate to notifications page
async function loginAndGoToNotifications(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/notifications');
}

test.describe('F-09 Notifications & Email', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F09-001: Notification bell renders with unread badge (API-level test via request context)', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/notifications/unread-count`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    // Must return 200 with a count field
    expect(response.status()).toBe(200);
    const body = await response.json();

    // Response must have a data object or direct count property
    expect(body).toBeDefined();
    const count = body?.data?.count ?? body?.count ?? body?.unreadCount;
    expect(typeof count).toBe('number');
  });

  test('E2E-F09-002: GET /api/notifications returns user notifications', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/notifications`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);
    const body = await response.json();

    // Response must contain a data property (array or paginated)
    expect(body).toHaveProperty('data');
    const notifications = body.data?.items ?? body.data;
    expect(Array.isArray(notifications)).toBe(true);
  });

  test('E2E-F09-003: PUT /api/notifications/read-all marks all as read', async ({ request }) => {
    const response = await request.put(`${BASE_URL}/api/notifications/read-all`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    // Accepts 200 (updated) or 204 (no content)
    expect([200, 204]).toContain(response.status());

    // After marking all as read, unread count should be 0
    const countResponse = await request.get(`${BASE_URL}/api/notifications/unread-count`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });
    expect(countResponse.status()).toBe(200);
    const countBody = await countResponse.json();
    const count = countBody?.data?.count ?? countBody?.count ?? countBody?.unreadCount;
    expect(count).toBe(0);
  });

  test('E2E-F09-004: Notification list page loads at /notifications route', async ({ page }) => {
    await loginAndGoToNotifications(page);

    // Notification list page container must be visible
    await expect(page.getByTestId('notification-list-page')).toBeVisible({ timeout: 10000 });

    // Mark all read button must be present
    await expect(page.getByTestId('mark-all-read-btn')).toBeVisible();

    // Notification bell (in nav) should be rendered
    await expect(page.getByTestId('notification-bell')).toBeVisible();
  });
});
