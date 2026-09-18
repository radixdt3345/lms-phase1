import { test, expect } from '@playwright/test';

/**
 * E2E Tests for F-14 — Seed Data & Initial Setup
 * Issue: #41
 *
 * F-14 is an admin-only API with no React UI.
 * These tests use Playwright's `request` context to call the API directly.
 */

const BASE_URL = process.env.BASE_URL || 'http://localhost:5173';
const SUPERADMIN_TOKEN = 'Bearer test-superadmin-token';
const EMPLOYEE_TOKEN = 'Bearer test-employee-token';

test.describe('F-14 Seed Data & Initial Setup (Admin API)', () => {
  test('E2E-F14-001: GET /api/admin/seed-status returns 200 with seeded boolean when authenticated as SuperAdmin', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/admin/seed-status`, {
      headers: {
        Authorization: SUPERADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);

    const body = await response.json();

    // Response must contain a `data` object with a boolean `seeded` field
    expect(body).toHaveProperty('data');
    expect(body.data).toHaveProperty('seeded');
    expect(typeof body.data.seeded).toBe('boolean');
  });

  test('E2E-F14-002: POST /api/admin/reseed returns 200 when authenticated as SuperAdmin', async ({ request }) => {
    const response = await request.post(`${BASE_URL}/api/admin/reseed`, {
      headers: {
        Authorization: SUPERADMIN_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(200);
  });

  test('E2E-F14-003: GET /api/admin/seed-status returns 401 when unauthenticated', async ({ request }) => {
    const response = await request.get(`${BASE_URL}/api/admin/seed-status`, {
      headers: {
        'Content-Type': 'application/json',
        // No Authorization header
      },
    });

    expect(response.status()).toBe(401);
  });

  test('E2E-F14-004: POST /api/admin/reseed returns 403 for Employee role', async ({ request }) => {
    const response = await request.post(`${BASE_URL}/api/admin/reseed`, {
      headers: {
        Authorization: EMPLOYEE_TOKEN,
        'Content-Type': 'application/json',
      },
    });

    expect(response.status()).toBe(403);
  });
});
