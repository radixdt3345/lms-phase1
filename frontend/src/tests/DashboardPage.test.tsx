import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import DashboardPage from '../pages/Dashboard/DashboardPage';
import dashboardReducer from '../store/dashboardSlice';
import * as dashboardApi from '../api/dashboardApi';

jest.mock('../api/dashboardApi', () => ({
  fetchDashboardOverview: jest.fn(),
  fetchTeamLeave: jest.fn(),
  fetchApprovalSummary: jest.fn(),
  fetchCompOffSummary: jest.fn(),
}));

const mockApi = dashboardApi as jest.Mocked<typeof dashboardApi>;

const mockOverview: dashboardApi.OverviewDashboardDto = {
  totalEmployees: 150,
  onLeaveToday: 12,
  pendingApprovals: 5,
  availableCompOffCredits: 30,
};

const mockTeamLeave: dashboardApi.TeamLeaveOverviewDto = {
  totalOnLeave: 2,
  totalPending: 1,
  employeesOnLeave: [
    {
      employeeId: 'emp-001',
      employeeName: 'Alice Smith',
      leaveType: 'Annual Leave',
      startDate: '2025-07-01',
      endDate: '2025-07-05',
      status: 'Approved',
    },
  ],
  pendingRequests: [
    {
      employeeId: 'emp-002',
      employeeName: 'Bob Jones',
      leaveType: 'Sick Leave',
      startDate: '2025-07-02',
      endDate: '2025-07-02',
      status: 'Pending',
    },
  ],
};

const mockApprovalSummary: dashboardApi.ApprovalDashboardSummaryDto = {
  pendingCount: 5,
  approvedToday: 3,
  rejectedToday: 1,
  averageTurnaroundHours: 4.5,
  pendingApprovals: [
    {
      id: 'apr-001',
      employeeName: 'Carol White',
      leaveType: 'Annual Leave',
      startDate: '2025-07-10',
      endDate: '2025-07-12',
      submittedAt: '2025-07-01T10:00:00Z',
    },
  ],
};

const mockCompOffSummary: dashboardApi.CompOffSummaryDto = {
  totalCreditsAvailable: 30,
  expiringThisMonth: 5,
  creditsUsedThisMonth: 8,
  creditsByEmployee: [
    {
      employeeId: 'emp-003',
      employeeName: 'Dave Brown',
      creditsAvailable: 10,
      expiringCredits: 2,
    },
  ],
};

const makeStore = (preloadedState?: any) =>
  configureStore({
    reducer: {
      dashboard: dashboardReducer,
      auth: () => ({ user: { roles: ['HRAdmin'] } }),
    },
    preloadedState,
  });

const renderPage = (preloadedState?: any) => {
  const store = makeStore(preloadedState);
  return render(
    <Provider store={store}>
      <DashboardPage />
    </Provider>
  );
};

describe('DashboardPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  // Scenario 1: Renders the page container
  it('renders the dashboard-page container', async () => {
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    expect(screen.getByTestId('dashboard-page')).toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  // Scenario 2: Shows loading spinner while all data is loading
  it('shows loading spinner when all data is loading', () => {
    mockApi.fetchDashboardOverview.mockReturnValue(new Promise(() => {}));
    mockApi.fetchTeamLeave.mockReturnValue(new Promise(() => {}));
    mockApi.fetchApprovalSummary.mockReturnValue(new Promise(() => {}));
    mockApi.fetchCompOffSummary.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  // Scenario 3: Overview stats cards display correct data
  it('displays overview stat cards with correct numbers', async () => {
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('employee-count-card')).toBeInTheDocument();
      expect(screen.getByTestId('on-leave-count-card')).toBeInTheDocument();
      expect(screen.getByTestId('pending-approvals-card')).toBeInTheDocument();
      expect(screen.getByTestId('comp-off-credits-card')).toBeInTheDocument();
    });
    expect(screen.getByText('150')).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
  });

  // Scenario 4: Tab switching — team leave tab shows table
  it('switches to team leave tab and shows team leave data', async () => {
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('team-leave-tab')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('team-leave-tab'));
    await waitFor(() => {
      expect(screen.getByTestId('team-leave-table')).toBeInTheDocument();
    });
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
  });

  // Scenario 5: Empty state — no team leave entries
  it('shows empty state when team leave has no entries', async () => {
    const emptyTeamLeave: dashboardApi.TeamLeaveOverviewDto = {
      totalOnLeave: 0,
      totalPending: 0,
      employeesOnLeave: [],
      pendingRequests: [],
    };
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(emptyTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('team-leave-tab')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('team-leave-tab'));
    await waitFor(() => {
      expect(screen.getByTestId('team-leave-empty')).toBeInTheDocument();
    });
  });

  // Scenario 6: Error state — shows error alert
  it('shows error alert when fetch fails', async () => {
    mockApi.fetchDashboardOverview.mockRejectedValue(new Error('Network error'));
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Network error/i)).toBeInTheDocument();
    });
  });

  // Scenario 7: Approvals tab shows approval data
  it('switches to approvals tab and shows approval data', async () => {
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('approvals-tab')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('approvals-tab'));
    await waitFor(() => {
      expect(screen.getByTestId('approvals-table')).toBeInTheDocument();
    });
    expect(screen.getByText('Carol White')).toBeInTheDocument();
  });

  // Scenario 8: Comp off tab shows comp-off data
  it('switches to comp-off tab and shows comp-off data', async () => {
    mockApi.fetchDashboardOverview.mockResolvedValue(mockOverview);
    mockApi.fetchTeamLeave.mockResolvedValue(mockTeamLeave);
    mockApi.fetchApprovalSummary.mockResolvedValue(mockApprovalSummary);
    mockApi.fetchCompOffSummary.mockResolvedValue(mockCompOffSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('comp-off-tab')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('comp-off-tab'));
    await waitFor(() => {
      expect(screen.getByTestId('comp-off-table')).toBeInTheDocument();
    });
    expect(screen.getByText('Dave Brown')).toBeInTheDocument();
  });
});
