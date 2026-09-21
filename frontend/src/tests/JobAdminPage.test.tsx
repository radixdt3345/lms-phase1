import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import JobAdminPage from '../pages/Jobs/JobAdminPage';
import jobReducer from '../store/jobSlice';

// Mock API module
vi.mock('../api/jobApi', () => ({
  fetchJobStatuses: vi.fn().mockResolvedValue([
    {
      jobName: 'LeaveBalanceSyncJob',
      cronExpression: '0 0 * * *',
      lastRunAt: '2024-01-15T00:00:00Z',
      lastRunStatus: 'Succeeded',
      lastRunDurationMs: 1234,
      nextRunAt: '2024-01-16T00:00:00Z',
      isEnabled: true,
    },
    {
      jobName: 'CompOffExpiryJob',
      cronExpression: '0 1 * * *',
      lastRunAt: '2024-01-15T01:00:00Z',
      lastRunStatus: 'Failed',
      lastRunDurationMs: 500,
      nextRunAt: '2024-01-16T01:00:00Z',
      isEnabled: true,
    },
    {
      jobName: 'EmailDispatchJob',
      cronExpression: '*/5 * * * *',
      lastRunAt: null,
      lastRunStatus: null,
      lastRunDurationMs: null,
      nextRunAt: null,
      isEnabled: false,
    },
  ]),
  triggerJob: vi.fn().mockResolvedValue(true),
}));

function makeStore() {
  return configureStore({
    reducer: { jobs: jobReducer },
  });
}

function renderPage() {
  return render(
    <Provider store={makeStore()}>
      <MemoryRouter>
        <JobAdminPage />
      </MemoryRouter>
    </Provider>,
  );
}

// UT-FE-J-01: Page renders with correct testid
test('UT-FE-J-01: renders job-admin-page container', () => {
  renderPage();
  expect(screen.getByTestId('job-admin-page')).toBeInTheDocument();
});

// UT-FE-J-02: Job table renders
test('UT-FE-J-02: renders job-table after data loads', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('job-table')).toBeInTheDocument();
  });
});

// UT-FE-J-03: Job status testids present
test('UT-FE-J-03: renders job-status-{jobName} testids for each job', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('job-status-LeaveBalanceSyncJob')).toBeInTheDocument();
    expect(screen.getByTestId('job-status-CompOffExpiryJob')).toBeInTheDocument();
    expect(screen.getByTestId('job-status-EmailDispatchJob')).toBeInTheDocument();
  });
});

// UT-FE-J-04: Trigger buttons render
test('UT-FE-J-04: renders trigger-job-btn for each job', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('trigger-job-btn-LeaveBalanceSyncJob')).toBeInTheDocument();
    expect(screen.getByTestId('trigger-job-btn-CompOffExpiryJob')).toBeInTheDocument();
  });
});

// UT-FE-J-05: Trigger button calls triggerJob
test('UT-FE-J-05: clicking trigger-job-btn calls triggerJob API', async () => {
  const jobApiMod = await import('../api/jobApi');
  const { triggerJob } = jobApiMod;
  renderPage();
  await waitFor(() => {
    expect(screen.getByTestId('trigger-job-btn-LeaveBalanceSyncJob')).toBeInTheDocument();
  });
  fireEvent.click(screen.getByTestId('trigger-job-btn-LeaveBalanceSyncJob'));
  await waitFor(() => {
    expect(triggerJob).toHaveBeenCalledWith('LeaveBalanceSyncJob', expect.any(String));
  });
});

// UT-FE-J-06: Failed status shows error chip
test('UT-FE-J-06: shows error-colored chip for Failed status', async () => {
  renderPage();
  await waitFor(() => {
    expect(screen.getByText('Failed')).toBeInTheDocument();
  });
});

// UT-FE-J-07: Disabled job button is disabled
test('UT-FE-J-07: trigger button is disabled for disabled job (EmailDispatchJob)', async () => {
  renderPage();
  await waitFor(() => {
    const btn = screen.getByTestId('trigger-job-btn-EmailDispatchJob');
    expect(btn).toBeDisabled();
  });
});

// UT-FE-J-08: Error state renders alert
test('UT-FE-J-08: renders error alert when API fails', async () => {
  const jobApiMod = await import('../api/jobApi');
  vi.mocked(jobApiMod.fetchJobStatuses).mockRejectedValueOnce(new Error('Network failure'));

  render(
    <Provider store={makeStore()}>
      <MemoryRouter>
        <JobAdminPage />
      </MemoryRouter>
    </Provider>,
  );

  await waitFor(() => {
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });
});
