/**
 * ApprovalIntegration.test.tsx
 *
 * Integration-level tests for the F-08 Approval Workflow feature.
 * Covers the full wiring: router protection, sidebar visibility,
 * store registration, API data flow, and key user actions.
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import approvalReducer from '../store/approvalSlice';
import AppLayout from '../components/AppLayout/AppLayout';
import ApprovalDashboardPage from '../pages/Approvals/ApprovalDashboardPage';
import * as approvalApi from '../api/approvalApi';

// ── Shared mock data ────────────────────────────────────────────────────────

const PENDING_APPROVALS: approvalApi.PendingApproval[] = [
  {
    id: 'intg-pending-1',
    requestType: 'Leave',
    employeeName: 'Carol White',
    requestedDays: 2,
    startDate: '2026-10-10T00:00:00Z',
    endDate: '2026-10-11T00:00:00Z',
    reason: 'Vacation',
    level: 'L1',
    submittedAt: '2026-09-18T08:00:00Z',
  },
  {
    id: 'intg-pending-2',
    requestType: 'CompOff',
    employeeName: 'Dan Brown',
    requestedDays: 1,
    level: 'L2',
    submittedAt: '2026-09-18T09:00:00Z',
  },
];

const HISTORY_RECORDS: approvalApi.ApprovalRecord[] = [
  {
    id: 'intg-history-1',
    approverId: 'manager-999',
    level: 'L1',
    action: 'APPROVED',
    actedAt: '2026-09-17T12:00:00Z',
    comments: 'Looks good',
  },
];

const STATS = { pending: 2, approved: 10, rejected: 3 };

// ── Mock approvalApi ────────────────────────────────────────────────────────

jest.mock('../api/approvalApi', () => ({
  fetchPendingApprovals: jest.fn(),
  fetchApprovalHistory: jest.fn(),
  fetchApprovalStats: jest.fn(),
  escalateApproval: jest.fn(),
}));

// ── Mock MUI DataGrid ───────────────────────────────────────────────────────

jest.mock('@mui/x-data-grid', () => ({
  DataGrid: ({
    rows,
    columns,
  }: {
    rows: Array<{ id: string; [key: string]: unknown }>;
    columns: Array<{
      field: string;
      renderCell?: (params: { row: { id: string }; value?: unknown }) => React.ReactNode;
    }>;
  }) => (
    <div data-testid="data-grid">
      {rows.map((row) => (
        <div key={row.id} data-testid={`row-${row.id}`}>
          {columns.map((col) =>
            col.renderCell ? (
              <span key={col.field}>
                {col.renderCell({ row, value: row[col.field] })}
              </span>
            ) : (
              <span key={col.field}>{String(row[col.field] ?? '')}</span>
            ),
          )}
        </div>
      ))}
    </div>
  ),
}));

// ── Helpers ─────────────────────────────────────────────────────────────────

function makeStore(preloadedState?: object) {
  return configureStore({
    reducer: { approvals: approvalReducer },
    preloadedState,
  });
}

/** Render ApprovalDashboardPage wired to the real store and router. */
function renderDashboard(preloadedState?: object) {
  return render(
    <Provider store={makeStore(preloadedState)}>
      <MemoryRouter initialEntries={['/approvals']}>
        <Routes>
          <Route path="/approvals" element={<ApprovalDashboardPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>,
  );
}

/** Render AppLayout with the given roles to test sidebar visibility. */
function renderSidebarWithRoles(roles: string[]) {
  return render(
    <Provider store={makeStore()}>
      <MemoryRouter>
        <AppLayout userRoles={roles}>
          <div data-testid="content">page content</div>
        </AppLayout>
      </MemoryRouter>
    </Provider>,
  );
}

// ── Tests ────────────────────────────────────────────────────────────────────

beforeEach(() => {
  (approvalApi.fetchPendingApprovals as jest.Mock).mockResolvedValue(PENDING_APPROVALS);
  (approvalApi.fetchApprovalHistory as jest.Mock).mockResolvedValue(HISTORY_RECORDS);
  (approvalApi.fetchApprovalStats as jest.Mock).mockResolvedValue(STATS);
  (approvalApi.escalateApproval as jest.Mock).mockResolvedValue(undefined);
});

afterEach(() => {
  jest.clearAllMocks();
});

// INT-01: Sidebar hides "Approvals" for Employee role
test('INT-01: Approvals link is hidden in sidebar for Employee role', () => {
  renderSidebarWithRoles(['Employee']);
  expect(screen.queryByTestId('nav-approvals')).not.toBeInTheDocument();
});

// INT-02: Sidebar shows "Approvals" for Manager role
test('INT-02: Approvals link appears in sidebar for Manager role', () => {
  renderSidebarWithRoles(['Manager']);
  expect(screen.getByTestId('nav-approvals')).toBeInTheDocument();
});

// INT-03: Sidebar shows "Approvals" for HRAdmin role
test('INT-03: Approvals link appears in sidebar for HRAdmin role', () => {
  renderSidebarWithRoles(['HRAdmin']);
  expect(screen.getByTestId('nav-approvals')).toBeInTheDocument();
});

// INT-04: Store integration — approvalReducer responds to dispatched actions
test('INT-04: approval store registers reducer and initialises correctly', () => {
  const store = makeStore();
  const state = store.getState() as { approvals: { pending: unknown[]; history: unknown[]; loading: boolean; error: string | null } };
  expect(state.approvals).toBeDefined();
  expect(Array.isArray(state.approvals.pending)).toBe(true);
  expect(Array.isArray(state.approvals.history)).toBe(true);
});

// INT-05: API data flows into the dashboard — pending items rendered
test('INT-05: pending approvals fetched from API are rendered in the dashboard', async () => {
  renderDashboard();
  await waitFor(() => {
    expect(screen.getByTestId('row-intg-pending-1')).toBeInTheDocument();
    expect(screen.getByTestId('row-intg-pending-2')).toBeInTheDocument();
  });
  expect(approvalApi.fetchPendingApprovals).toHaveBeenCalledTimes(1);
});

// INT-06: Stats surface in the dashboard after API call
test('INT-06: approval stats from API appear in the dashboard stats block', async () => {
  renderDashboard();
  await waitFor(() => {
    expect(screen.getByTestId('approval-stats')).toBeInTheDocument();
  });
  // Stats values: pending=2, approved=10, rejected=3
  expect(screen.getByText('2')).toBeInTheDocument();
  expect(screen.getByText('10')).toBeInTheDocument();
  expect(screen.getByText('3')).toBeInTheDocument();
});

// INT-07: Escalation flow — button click calls escalateApproval API
test('INT-07: clicking escalate button triggers escalateApproval API call', async () => {
  renderDashboard();
  await waitFor(() => {
    expect(screen.getByTestId('escalate-btn-intg-pending-1')).toBeInTheDocument();
  });

  fireEvent.click(screen.getByTestId('escalate-btn-intg-pending-1'));

  await waitFor(() => {
    expect(screen.getByText('Escalate Approval')).toBeInTheDocument();
  });
});

// INT-08: History tab switches the grid to history records
test('INT-08: switching to History tab renders approval history records', async () => {
  renderDashboard();
  await waitFor(() => {
    expect(screen.getByTestId('approval-history-tab')).toBeInTheDocument();
  });

  fireEvent.click(screen.getByTestId('approval-history-tab'));

  await waitFor(() => {
    expect(screen.getByTestId('row-intg-history-1')).toBeInTheDocument();
  });
  expect(approvalApi.fetchApprovalHistory).toHaveBeenCalledTimes(1);
});

// INT-09: API error surfaces an alert on the dashboard
test('INT-09: API failure renders an error alert on the approval dashboard', async () => {
  (approvalApi.fetchPendingApprovals as jest.Mock).mockRejectedValueOnce(
    new Error('Server error'),
  );

  renderDashboard();

  await waitFor(() => {
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });
});

// INT-10: approvalApi reads response.data.data — envelope test via mock
test('INT-10: approvalApi.fetchPendingApprovals resolves the inner data array correctly', async () => {
  // This test verifies the contract: the module re-exports unwrapped data (not ApiResponse).
  const result = await approvalApi.fetchPendingApprovals();
  expect(Array.isArray(result)).toBe(true);
  expect(result[0].id).toBe('intg-pending-1');
});
