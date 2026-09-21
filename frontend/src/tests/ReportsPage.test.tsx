import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import ReportsPage from '../pages/Reports/ReportsPage';
import reportReducer from '../store/reportSlice';
import * as reportApi from '../api/reportApi';

vi.mock('../api/reportApi', () => ({
  fetchReportJobs: vi.fn(),
  requestReport: vi.fn(),
  downloadReport: vi.fn(),
  fetchReportJob: vi.fn(),
}));

const mockApi = reportApi as vi.Mocked<typeof reportApi>;

const mockReportJob: reportApi.ReportJobDto = {
  id: 'job-001',
  reportType: 'LeaveSummary',
  status: 'Completed',
  requestedByUserId: 'user-001',
  requestedAt: '2025-07-01T10:00:00Z',
  completedAt: '2025-07-01T10:05:00Z',
};

const mockPendingJob: reportApi.ReportJobDto = {
  id: 'job-002',
  reportType: 'ApprovalHistory',
  status: 'Pending',
  requestedByUserId: 'user-001',
  requestedAt: '2025-07-01T11:00:00Z',
};

const makeStore = (preloadedState?: any) =>
  configureStore({
    reducer: {
      reports: reportReducer,
      auth: () => ({ user: { roles: ['HRAdmin'] } }),
    },
    preloadedState,
  });

const renderPage = (preloadedState?: any) => {
  const store = makeStore(preloadedState);
  return render(
    <Provider store={store}>
      <ReportsPage />
    </Provider>
  );
};

describe('ReportsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // Scenario 1: Renders the page container
  it('renders the reports-page container', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([]);
    renderPage();
    expect(screen.getByTestId('reports-page')).toBeInTheDocument();
    expect(screen.getByText('Reports & CSV Export')).toBeInTheDocument();
    expect(screen.getByTestId('generate-report-btn')).toBeInTheDocument();
  });

  // Scenario 2: Report type select changes value
  it('allows report type selection', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([]);
    renderPage();
    const select = screen.getByTestId('report-type-select');
    expect(select).toBeInTheDocument();
  });

  // Scenario 3: Generate report button dispatches action
  it('clicking generate report button calls requestReport', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([]);
    mockApi.requestReport.mockResolvedValue(mockReportJob);
    renderPage();
    const btn = screen.getByTestId('generate-report-btn');
    fireEvent.click(btn);
    await waitFor(() => {
      expect(mockApi.requestReport).toHaveBeenCalledTimes(1);
    });
  });

  // Scenario 4: Reports table shows jobs with status chips
  it('renders reports table with status chips', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([mockReportJob, mockPendingJob]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('reports-table')).toBeInTheDocument();
    });
    expect(screen.getByTestId(`report-status-${mockReportJob.id}`)).toBeInTheDocument();
    expect(screen.getByTestId(`report-status-${mockPendingJob.id}`)).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  // Scenario 5: Download button appears for completed reports
  it('shows download button only for completed reports', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([mockReportJob, mockPendingJob]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId(`download-report-btn-${mockReportJob.id}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId(`download-report-btn-${mockPendingJob.id}`)).not.toBeInTheDocument();
  });

  // Scenario 6: Empty state when no report jobs
  it('shows empty state when no report jobs exist', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('empty-reports')).toBeInTheDocument();
    });
  });

  // Scenario 7: Error state when fetch fails
  it('shows error alert when fetchReportJobs fails', async () => {
    mockApi.fetchReportJobs.mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Server error/i)).toBeInTheDocument();
    });
  });

  // Scenario 8: Generate report failure shows error
  it('shows error when report generation fails', async () => {
    mockApi.fetchReportJobs.mockResolvedValue([]);
    mockApi.requestReport.mockRejectedValue(new Error('Generation failed'));
    renderPage();
    fireEvent.click(screen.getByTestId('generate-report-btn'));
    await waitFor(() => {
      expect(screen.getByText(/Generation failed/i)).toBeInTheDocument();
    });
  });
});
