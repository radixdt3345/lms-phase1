import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import ApprovalDashboardPage from '../pages/Approvals/ApprovalDashboardPage';
import approvalReducer from '../store/approvalSlice';

// Mock approvalApi
vi.mock('../api/approvalApi', () => ({
  fetchPendingApprovals: vi.fn().mockResolvedValue([
    {
      id: 'pending-1',
      requestType: 'Leave',
      employeeName: 'Alice Johnson',
      requestedDays: 3,
      startDate: '2026-10-01T00:00:00Z',
      endDate: '2026-10-03T00:00:00Z',
      reason: 'Personal reasons',
      level: 'L1',
      submittedAt: '2026-09-15T09:00:00Z',
    },
    {
      id: 'pending-2',
      requestType: 'CompOff',
      employeeName: 'Bob Smith',
      requestedDays: 1,
      level: 'L2',
      submittedAt: '2026-09-16T10:00:00Z',
    },
  ]),
  fetchApprovalHistory: vi.fn().mockResolvedValue([
    {
      id: 'history-1',
      approverId: 'manager-001',
      level: 'L1',
      action: 'APPROVED',
      actedAt: '2026-09-14T08:00:00Z',
      comments: 'Approved for personal reasons',
    },
  ]),
  fetchApprovalStats: vi.fn().mockResolvedValue({ pending: 2, approved: 5, rejected: 1 }),
  escalateApproval: vi.fn().mockResolvedValue(undefined),
}));

// Mock MUI DataGrid to keep tests fast
vi.mock('@mui/x-data-grid', () => ({
  DataGrid: ({ rows, columns }: { rows: Array<{ id: string; [key: string]: unknown }>; columns: Array<{ field: string; renderCell?: (params: { row: { id: string }; value?: unknown }) => React.ReactNode }> }) => (
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

function makeStore(preloadedState?: object) {
  return configureStore({
    reducer: { approvals: approvalReducer },
    preloadedState,
  });
}

function renderPage(preloadedState?: object) {
  return render(
    <Provider store={makeStore(preloadedState)}>
      <MemoryRouter>
        <ApprovalDashboardPage />
      </MemoryRouter>
    </Provider>,
  );
}

// UT-FE-AP-01: Page renders with correct testid
test('UT-FE-AP-01: renders approval-dashboard container', () => {
  renderPage();
  expect(screen.getByTestId('approval-dashboard')).toBeInTheDocument();
});

// UT-FE-AP-02: Stats cards render
test('UT-FE-AP-02: renders approval-stats block', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('approval-stats')).toBeInTheDocument();
  });
});

// UT-FE-AP-03: Empty state — no rows means empty grid
test('UT-FE-AP-03: renders empty grid when no pending approvals', async () => {
  const approvalApiMod = await import('../api/approvalApi');
  vi.mocked(approvalApiMod.fetchPendingApprovals).mockResolvedValueOnce([]);

  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('data-grid')).toBeInTheDocument();
  });
});

// UT-FE-AP-04: Approve button renders for each pending row
test('UT-FE-AP-04: approve and reject buttons rendered for each pending row', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('approve-btn-pending-1')).toBeInTheDocument();
    expect(screen.getByTestId('reject-btn-pending-1')).toBeInTheDocument();
    expect(screen.getByTestId('approve-btn-pending-2')).toBeInTheDocument();
    expect(screen.getByTestId('reject-btn-pending-2')).toBeInTheDocument();
  });
});

// UT-FE-AP-05: Escalate button opens dialog
test('UT-FE-AP-05: escalate button triggers escalation for a pending row', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('escalate-btn-pending-1')).toBeInTheDocument();
  });
  fireEvent.click(screen.getByTestId('escalate-btn-pending-1'));
  await waitFor(() => {
    expect(screen.getByText('Escalate Approval')).toBeInTheDocument();
  });
});

// UT-FE-AP-06: History tab is accessible
test('UT-FE-AP-06: history tab renders and is clickable', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('approval-history-tab')).toBeInTheDocument();
  });
  fireEvent.click(screen.getByTestId('approval-history-tab'));
  await waitFor(() => {
    expect(screen.getByTestId('data-grid')).toBeInTheDocument();
  });
});

// UT-FE-AP-07: Error state shows alert
test('UT-FE-AP-07: renders error alert when API fails', async () => {
  const approvalApiMod = await import('../api/approvalApi');
  vi.mocked(approvalApiMod.fetchPendingApprovals).mockRejectedValueOnce(new Error('Network failure'));

  renderPage();
  await waitFor(() => {
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });
});

// UT-FE-AP-08: Stats values populate from API
test('UT-FE-AP-08: stats show values returned by fetchApprovalStats', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByText('2')).toBeInTheDocument(); // pending count
    expect(screen.getByText('5')).toBeInTheDocument(); // approved count
    expect(screen.getByText('1')).toBeInTheDocument(); // rejected count
  });
});
