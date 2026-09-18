import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import LeaveBalancePage from '../pages/LeaveBalance/LeaveBalancePage';
import leaveBalanceReducer from '../store/leaveBalanceSlice';
import * as leaveBalanceApi from '../api/leaveBalanceApi';

// Mock the API module with ApiResponse<T> envelope
jest.mock('../api/leaveBalanceApi', () => ({
  fetchMyLeaveBalances: jest.fn(),
  fetchAllLeaveBalances: jest.fn(),
  fetchLeaveBalancesByEmployee: jest.fn(),
  adjustLeaveBalance: jest.fn(),
}));

const mockApi = leaveBalanceApi as jest.Mocked<typeof leaveBalanceApi>;

const mockBalances: leaveBalanceApi.LeaveBalanceDto[] = [
  {
    id: 'bal-001',
    employeeId: 'emp-001',
    employeeName: 'John Doe',
    leaveTypeId: 'lt-001',
    leaveTypeName: 'Annual Leave',
    leaveTypeCode: 'AL',
    year: 2025,
    totalDays: 21,
    usedDays: 5,
    pendingDays: 2,
    adjustedDays: 0,
    availableDays: 14,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 'bal-002',
    employeeId: 'emp-001',
    employeeName: 'John Doe',
    leaveTypeId: 'lt-002',
    leaveTypeName: 'Sick Leave',
    leaveTypeCode: 'SL',
    year: 2025,
    totalDays: 10,
    usedDays: 1,
    pendingDays: 0,
    adjustedDays: 0,
    availableDays: 9,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
];

const makeStore = (roles: string[] = []) =>
  configureStore({
    reducer: {
      leaveBalance: leaveBalanceReducer,
      auth: () => ({ user: { roles } }),
    },
  });

const renderPage = (roles: string[] = []) => {
  const store = makeStore(roles);
  return render(
    <Provider store={store}>
      <LeaveBalancePage />
    </Provider>
  );
};

describe('LeaveBalancePage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  // Scenario 1: Renders the page container
  it('renders the leave-balance-page container', async () => {
    mockApi.fetchMyLeaveBalances.mockResolvedValue([]);
    renderPage();
    expect(screen.getByTestId('leave-balance-page')).toBeInTheDocument();
    expect(screen.getByText('Leave Balances')).toBeInTheDocument();
  });

  // Scenario 2: Shows loading spinner while fetching
  it('shows loading spinner during fetch', () => {
    // Return a promise that never resolves during this test
    mockApi.fetchMyLeaveBalances.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  // Scenario 3: Shows empty state when no balances
  it('shows empty state when no balances returned', async () => {
    mockApi.fetchMyLeaveBalances.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('empty-balances')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('leave-balance-table')).not.toBeInTheDocument();
  });

  // Scenario 4: Shows error state
  it('shows error message when fetch fails', async () => {
    mockApi.fetchMyLeaveBalances.mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Network error/i)).toBeInTheDocument();
    });
  });

  // Scenario 5: Happy path — shows balance table with rows
  it('renders balance table with balance-row-{leaveTypeId} test ids', async () => {
    mockApi.fetchMyLeaveBalances.mockResolvedValue(mockBalances);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('leave-balance-table')).toBeInTheDocument();
    });
    expect(screen.getByTestId('balance-row-lt-001')).toBeInTheDocument();
    expect(screen.getByTestId('balance-row-lt-002')).toBeInTheDocument();
    expect(screen.getByText('Annual Leave')).toBeInTheDocument();
    expect(screen.getByText('Sick Leave')).toBeInTheDocument();
  });

  // Scenario 6: HRAdmin sees adjust button and can open dialog
  it('shows adjust-balance-btn for HRAdmin and opens adjust-dialog', async () => {
    mockApi.fetchMyLeaveBalances.mockResolvedValue(mockBalances);
    renderPage(['HRAdmin']);
    await waitFor(() => {
      expect(screen.getAllByTestId('adjust-balance-btn').length).toBeGreaterThan(0);
    });
    // Click the first Adjust button
    fireEvent.click(screen.getAllByTestId('adjust-balance-btn')[0]);
    expect(screen.getByTestId('adjust-dialog')).toBeInTheDocument();
  });

  // Scenario 7 (failure path): Adjust failure shows error in dialog
  it('shows error in dialog when adjustment fails', async () => {
    mockApi.fetchMyLeaveBalances.mockResolvedValue(mockBalances);
    mockApi.adjustLeaveBalance.mockRejectedValue(new Error('Adjustment failed'));
    renderPage(['HRAdmin']);
    await waitFor(() => {
      expect(screen.getAllByTestId('adjust-balance-btn').length).toBeGreaterThan(0);
    });
    fireEvent.click(screen.getAllByTestId('adjust-balance-btn')[0]);
    // Fill in adjustment fields
    fireEvent.change(screen.getByTestId('adjustment-input'), { target: { value: '2' } });
    fireEvent.change(screen.getByTestId('reason-input'), { target: { value: 'Correction' } });
    fireEvent.click(screen.getByTestId('submit-adjust-btn'));
    await waitFor(() => {
      expect(screen.getByText(/Adjustment failed/i)).toBeInTheDocument();
    });
  });
});
