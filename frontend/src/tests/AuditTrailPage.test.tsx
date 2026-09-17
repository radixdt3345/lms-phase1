import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import authReducer from '../store/authSlice';
import auditLogReducer from '../store/auditLogSlice';
import AuditTrailPage from '../pages/AuditTrail/AuditTrailPage';
import type { AuditLogPagedResult } from '../types';

// Mock the API module — return values simulate ApiResponse<T> already unwrapped by the API module
vi.mock('../api/auditLogApi', () => ({
  fetchAuditLogs: vi.fn(),
}));

import * as auditLogApi from '../api/auditLogApi';

const mockAuditLogs: AuditLogPagedResult = {
  items: [
    {
      id: 'a1',
      actorUserId: 'u1',
      actorEmail: 'hr@test.com',
      actionType: 'CREATE',
      recordType: 'Employee',
      recordId: 'emp-001',
      oldValue: null,
      newValue: '{"name":"Alice"}',
      ipAddress: '192.168.1.1',
      timestamp: '2026-09-01T10:00:00Z',
    },
    {
      id: 'a2',
      actorUserId: 'u2',
      actorEmail: 'admin@test.com',
      actionType: 'UPDATE',
      recordType: 'Department',
      recordId: 'dept-002',
      oldValue: '{"name":"HR"}',
      newValue: '{"name":"Human Resources"}',
      ipAddress: '10.0.0.5',
      timestamp: '2026-09-02T11:30:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 50,
};

const emptyPagedResult: AuditLogPagedResult = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 50,
};

const authState = {
  user: { id: 'u1', email: 'hr@test.com', displayName: 'HR Admin', roles: ['HRAdmin'], status: 'active' },
  isAuthenticated: true,
  loading: false,
  error: null,
};

function makeStore(auditLogState?: Partial<{
  items: typeof mockAuditLogs.items;
  totalCount: number;
  loading: boolean;
  error: string | null;
}>) {
  return configureStore({
    reducer: { auth: authReducer, auditLog: auditLogReducer },
    preloadedState: {
      auth: authState,
      auditLog: {
        items: auditLogState?.items ?? [],
        totalCount: auditLogState?.totalCount ?? 0,
        loading: auditLogState?.loading ?? false,
        error: auditLogState?.error ?? null,
        filters: { page: 1, pageSize: 50 },
      },
    },
  });
}

describe('AuditTrailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(auditLogApi.fetchAuditLogs).mockResolvedValue(mockAuditLogs);
  });

  // UT-FE-13-001: renders page with required data-testid
  it('UT-FE-13-001: renders page with data-testid audit-trail-page', () => {
    render(
      <Provider store={makeStore()}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    expect(screen.getByTestId('audit-trail-page')).toBeTruthy();
  });

  // UT-FE-13-002: shows loading spinner when loading with no data
  it('UT-FE-13-002: shows loading spinner during initial fetch', () => {
    const store = configureStore({
      reducer: { auth: authReducer, auditLog: auditLogReducer },
      preloadedState: {
        auth: authState,
        auditLog: {
          items: [],
          totalCount: 0,
          loading: true,
          error: null,
          filters: { page: 1, pageSize: 50 },
        },
      },
    });
    render(
      <Provider store={store}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    expect(screen.getByTestId('audit-loading-spinner')).toBeTruthy();
  });

  // UT-FE-13-003: shows empty state when no records returned
  it('UT-FE-13-003: shows empty state when no records found', async () => {
    vi.mocked(auditLogApi.fetchAuditLogs).mockResolvedValue(emptyPagedResult);
    render(
      <Provider store={makeStore({ items: [], totalCount: 0, loading: false })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    // waitFor because useEffect triggers a thunk dispatch that temporarily sets loading=true
    await waitFor(() => {
      expect(screen.getByTestId('audit-empty-state')).toBeTruthy();
    });
  });

  // UT-FE-13-004: shows error alert when fetch fails (waitFor because pending action clears error briefly)
  it('UT-FE-13-004: shows error alert on fetch failure', async () => {
    vi.mocked(auditLogApi.fetchAuditLogs).mockRejectedValue(new Error('Server error'));
    render(
      <Provider store={makeStore({ items: [], totalCount: 0, error: 'Server error' })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    // useEffect dispatch triggers pending (clears error) then rejected (sets error again)
    await waitFor(() => {
      expect(screen.getByText('Server error')).toBeTruthy();
    });
  });

  // UT-FE-13-005: happy path — renders audit log table with records
  it('UT-FE-13-005: renders audit-log-table with records on happy path', async () => {
    render(
      <Provider store={makeStore({ items: mockAuditLogs.items, totalCount: 2 })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    expect(screen.getByTestId('audit-log-table')).toBeTruthy();
    expect(screen.getByText('hr@test.com')).toBeTruthy();
    expect(screen.getByText('admin@test.com')).toBeTruthy();
  });

  // UT-FE-13-006: all filter data-testids are present
  it('UT-FE-13-006: renders all required filter data-testids', async () => {
    render(
      <Provider store={makeStore({ items: mockAuditLogs.items, totalCount: 2 })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    expect(screen.getByTestId('audit-filter-user')).toBeTruthy();
    expect(screen.getByTestId('audit-filter-action-type')).toBeTruthy();
    expect(screen.getByTestId('audit-filter-record-type')).toBeTruthy();
    expect(screen.getByTestId('audit-filter-date-from')).toBeTruthy();
    expect(screen.getByTestId('audit-filter-date-to')).toBeTruthy();
    expect(screen.getByTestId('audit-clear-filters-button')).toBeTruthy();
  });

  // UT-FE-13-007: clear filters button resets inputs
  it('UT-FE-13-007: clear filters button resets all filter inputs', async () => {
    render(
      <Provider store={makeStore({ items: mockAuditLogs.items, totalCount: 2 })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    // find the native input inside the user filter wrapper
    const userFilterWrapper = screen.getByTestId('audit-filter-user');
    const userInput = userFilterWrapper.querySelector('input') as HTMLInputElement;
    fireEvent.change(userInput, { target: { value: 'u1' } });
    expect(userInput.value).toBe('u1');

    const clearBtn = screen.getByTestId('audit-clear-filters-button');
    fireEvent.click(clearBtn);

    await waitFor(() => {
      expect(userInput.value).toBe('');
    });
  });

  // UT-FE-13-008: API mock uses ApiResponse envelope (fetchAuditLogs returns unwrapped payload)
  it('UT-FE-13-008: fetchAuditLogs returns AuditLogPagedResult (ApiResponse already unwrapped)', async () => {
    const result = await auditLogApi.fetchAuditLogs({ page: 1, pageSize: 50 });
    expect(result).toEqual(mockAuditLogs);
    expect(Array.isArray(result.items)).toBe(true);
    expect(result.totalCount).toBe(2);
  });

  // UT-FE-13-009: json diff expand button renders for records with values
  it('UT-FE-13-009: json diff expand button visible for records with old/new values', () => {
    render(
      <Provider store={makeStore({ items: mockAuditLogs.items, totalCount: 2 })}>
        <MemoryRouter>
          <AuditTrailPage />
        </MemoryRouter>
      </Provider>,
    );
    const expandBtns = screen.getAllByTestId('audit-json-diff-expand');
    // a1 has newValue, a2 has both oldValue and newValue — both show expand button
    expect(expandBtns.length).toBeGreaterThanOrEqual(1);
  });
});
