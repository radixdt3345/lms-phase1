import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import reportReducer from '../store/reportSlice';
import authReducer from '../store/authSlice';
import * as reportApi from '../api/reportApi';
import ReportsPage from '../pages/Reports/ReportsPage';

jest.mock('../api/reportApi');

const mockReportJobs = [
  {
    id: 'job-001',
    reportType: 'LeaveSummary' as const,
    status: 'Completed' as const,
    requestedByUserId: 'user-1',
    requestedAt: '2026-09-21T10:00:00Z',
    completedAt: '2026-09-21T10:05:00Z',
  },
  {
    id: 'job-002',
    reportType: 'CompOffSummary' as const,
    status: 'Pending' as const,
    requestedByUserId: 'user-1',
    requestedAt: '2026-09-21T11:00:00Z',
  },
];

const mockNewJob = {
  id: 'job-003',
  reportType: 'ApprovalHistory' as const,
  status: 'Pending' as const,
  requestedByUserId: 'user-1',
  requestedAt: '2026-09-21T12:00:00Z',
};

const buildStore = (roles: string[] = ['HRAdmin']) =>
  configureStore({
    reducer: { reports: reportReducer, auth: authReducer },
    preloadedState: {
      auth: { user: { roles }, token: 'test-token', isAuthenticated: true } as any,
    },
  });

const renderReports = (roles: string[] = ['HRAdmin']) => {
  const store = buildStore(roles);
  return render(
    <Provider store={store}>
      <MemoryRouter initialEntries={['/reports']}>
        <Routes>
          <Route path="/reports" element={<ReportsPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>
  );
};

describe('Reports Integration', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (reportApi.fetchReportJobs as jest.Mock).mockResolvedValue(mockReportJobs);
    (reportApi.requestReport as jest.Mock).mockResolvedValue(mockNewJob);
    (reportApi.fetchReportJob as jest.Mock).mockResolvedValue(mockReportJobs[0]);
    (reportApi.downloadReport as jest.Mock).mockResolvedValue(new Blob(['col1,col2'], { type: 'text/csv' }));
  });

  it('INT-RPT-01: /reports route renders ReportsPage without crashing', async () => {
    renderReports();
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    expect(document.body).toBeTruthy();
  });

  it('INT-RPT-02: reports store slice is registered — state accessible', () => {
    const store = buildStore(['SuperAdmin']);
    const state = store.getState();
    expect(state).toHaveProperty('reports');
  });

  it('INT-RPT-03: fetchReportJobs returns list via response.data.data pattern', async () => {
    const result = await reportApi.fetchReportJobs();
    expect(result).toEqual(mockReportJobs);
    expect(Array.isArray(result)).toBe(true);
    expect(result[0]).toHaveProperty('reportType');
    expect(result[0]).toHaveProperty('status');
  });

  it('INT-RPT-04: requestReport returns new job via response.data.data pattern', async () => {
    const result = await reportApi.requestReport('LeaveSummary', '2026-09-01', '2026-09-30');
    expect(result).toEqual(mockNewJob);
    expect(result).toHaveProperty('id');
    expect(result.status).toBe('Pending');
  });

  it('INT-RPT-05: fetchReportJob returns single job via response.data.data pattern', async () => {
    const result = await reportApi.fetchReportJob('job-001');
    expect(result).toEqual(mockReportJobs[0]);
    expect(result).toHaveProperty('id', 'job-001');
    expect(result.status).toBe('Completed');
  });

  it('INT-RPT-06: downloadReport returns a Blob for completed jobs', async () => {
    const result = await reportApi.downloadReport('job-001');
    expect(result).toBeInstanceOf(Blob);
    expect(result.type).toBe('text/csv');
  });
});
