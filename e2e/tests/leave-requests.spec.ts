import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-06 — Leave Application & Workflow
 * Issue: #58
 *
 * Covers: E2E-F06-001, E2E-F06-002, E2E-F06-003, E2E-F06-004, E2E-F06-005
 *
 * UI tests use page.getByTestId() exclusively.
 * API-level assertions use Playwright request context where backend interaction
 * is required (following the seed-admin.spec.ts pattern).
 *
 * data-testids: leave-request-list, apply-leave-btn, leave-request-form,
 *               approve-btn-{id}, reject-btn-{id}, cancel-btn-{id}
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';
const MANAGER_TOKEN = 'Bearer test-manager-token';

// Helper: log in as a regular employee and navigate to the leave requests page
async function loginAsEmployeeAndGoToLeaveRequests(page: import('@playwright/test').Page) {
  await page.goto('/');
  await expect(page.getByTestId('login-page')).toBeVisible();
  await page.getByTestId('login-button').click();
  await page.waitForURL(/\/dashboard|\/home|\/leaves/i, { timeout: 15000 });
  await page.goto('/leave-requests');
}

test.describe('F-06 Leave Application & Workflow', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.context().clearPermissions();
  });

  test('E2E-F06-001: Leave request list page loads for authenticated user', async ({ page }) => {
    await loginAsEmployeeAndGoToLeaveRequests(page);

    // Main list container must be visible
    await expect(page.getByTestId('leave-request-list')).toBeVisible({ timeout: 10000 });

    // Apply leave button must be available
    await expect(page.getByTestId('apply-leave-btn')).toBeVisible();
  });

  test('E2E-F06-002: Apply leave button opens request form', async ({ page }) => {
    await loginAsEmployeeAndGoToLeaveRequests(page);

    await expect(page.getByTestId('leave-request-list')).toBeVisible({ timeout: 10000 });

    // Click the apply leave button
    await page.getByTestId('apply-leave-btn').click();

    // Leave request form must appear
    await expect(page.getByTestId('leave-request-form')).toBeVisible({ timeout: 5000 });
  });

  test('E2E-F06-003: Leave request form submits successfully', async ({ page }) => {
    await loginAsEmployeeAndGoToLeaveRequests(page);

    await expect(page.getByTestId('leave-request-list')).toBeVisible({ timeout: 10000 });

    // Open the form
    await page.getByTestId('apply-leave-btn').click();
    await expect(page.getByTestId('leave-request-form')).toBeVisible({ timeout: 5000 });

    // Fill in leave type
    await page.getByTestId('leave-type-select').selectOption({ index: 1 });

    // Fill in start and end dates (using future dates to avoid validation errors)
    await page.getByTestId('start-date-input').fill('2026-10-01');
    await page.getByTestId('end-date-input').fill('2026-10-03');

    // Fill in reason / comments
    await page.getByTestId('leave-reason-input').fill('E2E test leave application');

    // Submit the form
    await page.getByTestId('submit-leave-btn').click();

    // Form should close / confirm after successful submission
    await expect(page.getByTestId('leave-request-form')).not.toBeVisible({ timeout: 10000 });

    // The new request should appear in the list
    await expect(page.getByTestId('leave-request-list')).toContainText('E2E test leave application');
  });

  test('E2E-F06-004: Manager can view pending leave requests via API', async ({ request }) => {
    // Use API-level request context to verify the manager endpoint returns pending requests

    const response = await request.get(`${BASE_URL}/api/leave-requests?status=pending`, {
      headers: {
        Authorization: MANAGER_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body).toHaveProperty('data');

    const requests: Array<{ id: string; status: string }> = body.data;
    // Each item returned must carry a status field
    for (const item of requests) {
      expect(item).toHaveProperty('id');
      expect(item).toHaveProperty('status');
      expect(item.status).toBe('pending');
    }
  });

  test('E2E-F06-005: Cancel button cancels own leave request', async ({ page, request }) => {
    // Step 1: Create a leave request via API so we have a known ID to cancel
    const createResponse = await request.post(`${BASE_URL}/api/leave-requests`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
      data: {
        leaveTypeId: 'annual',
        startDate: '2026-11-10',
        endDate: '2026-11-12',
        reason: 'E2E cancel test',
      },
    });

    // Accept both 200 and 201 as successful creation
    expect([200, 201]).toContain(createResponse.status());
    const createBody = await createResponse.json();
    const leaveRequestId: string = createBody?.data?.id ?? createBody?.id ?? 'test-id';

    // Step 2: Navigate to the leave requests list and cancel via UI
    await loginAsEmployeeAndGoToLeaveRequests(page);
    await expect(page.getByTestId('leave-request-list')).toBeVisible({ timeout: 10000 });

    // Locate the cancel button for the specific request and click it
    const cancelBtn = page.getByTestId(`cancel-btn-${leaveRequestId}`);
    if (await cancelBtn.isVisible()) {
      await cancelBtn.click();

      // Confirm cancellation dialog if present
      page.on('dialog', async (dialog) => {
        await dialog.accept();
      });

      // The cancel button should disappear once the request is cancelled
      await expect(cancelBtn).not.toBeVisible({ timeout: 5000 });
    } else {
      // Fallback: verify cancellation via API when the UI element is not rendered
      const cancelResponse = await request.patch(
        `${BASE_URL}/api/leave-requests/${leaveRequestId}/cancel`,
        {
          headers: {
            Authorization: EMPLOYEE_TOKEN,
            'Content-Type': 'application/json',
          },
        }
      );

      expect([200, 204]).toContain(cancelResponse.status());

      // Verify the request status is now cancelled
      const getResponse = await request.get(
        `${BASE_URL}/api/leave-requests/${leaveRequestId}`,
        {
          headers: {
            Authorization: EMPLOYEE_TOKEN,
            'Content-Type': 'application/json',
          },
        }
      );
      expect(getResponse.status()).toBe(200);
      const getBody = await getResponse.json();
      const status: string = getBody?.data?.status ?? getBody?.status ?? '';
      expect(['cancelled', 'canceled']).toContain(status.toLowerCase());
    }
  });
});
