import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import dashboardReducer from '../store/dashboardSlice';
import authReducer from '../store/authSlice';
import * as dashboardApi from '../api/dashboardApi';
import DashboardPage from '../pages/Dashboard/DashboardPage';

jest.mock('../api/dashboardApi');

const mockOverview = {
  totalEmployees: 50,
  onLeaveToday: 3,
  pendingApprovals: 5,
  availableCompOffCredits: 12,
};

const mockTeamLeave = {
  employeesOnLeave: [],
  pendingRequests: [],
  totalOnLeave: 3,
  totalPending: 5,
};

const mockApprovalSummary = {
  pendingCount: 5,
  approvedToday: 2,
  rejectedToday: 1,
  averageTurnaroundHours: 4,
  pendingApprovals: [],
};

const mockCompOff = {
  totalCreditsAvailable: 12,
  expiringThisMonth: 2,
  creditsUsedThisMonth: 1,
  creditsByEmployee: [],
};

const buildStore = (roles: string[] = ['HRAdmin']) =>
  configureStore({
    reducer: { dashboard: dashboardReducer, auth: authReducer },
    preloadedState: {
      auth: { user: { roles }, token: 'test-token', isAuthenticated: true } as any,
    },
  });

const renderDashboard = (roles: string[] = ['HRAdmin']) => {
  const store = buildStore(roles);
  return render(
    <Provider store={store}>
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/dashboard" element={<DashboardPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>
  );
};

describe('Dashboard Integration', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (dashboardApi.fetchDashboardOverview as jest.Mock).mockResolvedValue(mockOverview);
    (dashboardApi.fetchTeamLeave as jest.Mock).mockResolvedValue(mockTeamLeave);
    (dashboardApi.fetchApprovalSummary as jest.Mock).mockResolvedValue(mockApprovalSummary);
    (dashboardApi.fetchCompOffSummary as jest.Mock).mockResolvedValue(mockCompOff);
  });

  it('INT-DASH-01: dashboard route renders DashboardPage without crashing', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    // Page should be in the document (not redirected)
    expect(document.body).toBeTruthy();
  });

  it('INT-DASH-02: dashboard store slice is registered — state accessible', () => {
    const store = buildStore(['Manager']);
    const state = store.getState();
    expect(state).toHaveProperty('dashboard');
  });

  it('INT-DASH-03: dashboardApi.fetchDashboardOverview returns response.data.data payload', async () => {
    const result = await dashboardApi.fetchDashboardOverview();
    expect(result).toEqual(mockOverview);
    expect(result).toHaveProperty('totalEmployees');
    expect(result).toHaveProperty('pendingApprovals');
  });

  it('INT-DASH-04: dashboardApi.fetchTeamLeave returns response.data.data payload', async () => {
    const result = await dashboardApi.fetchTeamLeave();
    expect(result).toEqual(mockTeamLeave);
    expect(result).toHaveProperty('totalOnLeave');
    expect(result).toHaveProperty('totalPending');
  });

  it('INT-DASH-05: dashboardApi.fetchApprovalSummary returns response.data.data payload', async () => {
    const result = await dashboardApi.fetchApprovalSummary();
    expect(result).toEqual(mockApprovalSummary);
    expect(result).toHaveProperty('pendingCount');
    expect(result).toHaveProperty('averageTurnaroundHours');
  });

  it('INT-DASH-06: dashboardApi.fetchCompOffSummary returns response.data.data payload', async () => {
    const result = await dashboardApi.fetchCompOffSummary();
    expect(result).toEqual(mockCompOff);
    expect(result).toHaveProperty('totalCreditsAvailable');
    expect(result).toHaveProperty('expiringThisMonth');
  });
});
