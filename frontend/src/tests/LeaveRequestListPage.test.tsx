import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import leaveRequestReducer from '../../store/leaveRequestSlice';
import leavePolicyReducer from '../../store/leavePolicySlice';
import LeaveRequestListPage from '../../pages/LeaveRequests/LeaveRequestListPage';
import * as leaveRequestApi from '../../api/leaveRequestApi';

jest.mock('../../api/leaveRequestApi');
jest.mock('../../api/leavePolicyApi', () => ({
  fetchLeaveTypes: jest.fn().mockResolvedValue([]),
}));

const mockRequests = [
  {
    id: 'req-1',
    employeeId: 'emp-1',
    employeeName: 'Alice Smith',
    leaveTypeId: 'lt-1',
    leaveTypeName: 'Annual Leave',
    startDate: '2024-03-01',
    endDate: '2024-03-05',
    totalDays: 5,
    reason: 'Vacation',
    status: 'Pending',
    createdAt: '2024-02-01T00:00:00Z',
    updatedAt: '2024-02-01T00:00:00Z',
  },
  {
    id: 'req-2',
    employeeId: 'emp-2',
    employeeName: 'Bob Jones',
    leaveTypeId: 'lt-1',
    leaveTypeName: 'Annual Leave',
    startDate: '2024-03-10',
    endDate: '2024-03-12',
    totalDays: 3,
    reason: 'Personal',
    status: 'Approved',
    createdAt: '2024-02-05T00:00:00Z',
    updatedAt: '2024-02-06T00:00:00Z',
  },
];

const buildStore = (preloadedState = {}) =>
  configureStore({
    reducer: {
      leaveRequests: leaveRequestReducer,
      leavePolicy: leavePolicyReducer,
    },
    preloadedState,
  });

const renderPage = (store = buildStore()) =>
  render(
    <Provider store={store}>
      <LeaveRequestListPage />
    </Provider>
  );

describe('LeaveRequestListPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockResolvedValue([]);
    (leaveRequestApi.approveLeaveRequest as jest.Mock).mockResolvedValue({
      ...mockRequests[0],
      status: 'Approved',
    });
    (leaveRequestApi.rejectLeaveRequest as jest.Mock).mockResolvedValue({
      ...mockRequests[0],
      status: 'Rejected',
      rejectionReason: 'No reason',
    });
    (leaveRequestApi.cancelLeaveRequest as jest.Mock).mockResolvedValue({
      ...mockRequests[0],
      status: 'Cancelled',
    });
  });

  it('renders the leave request list container', () => {
    renderPage();
    expect(screen.getByTestId('leave-request-list')).toBeInTheDocument();
    expect(screen.getByText('Leave Requests')).toBeInTheDocument();
    expect(screen.getByTestId('apply-leave-btn')).toBeInTheDocument();
  });

  it('shows loading spinner on initial fetch', () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('shows empty state when no requests', async () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('empty-leave-requests')).toBeInTheDocument();
    });
  });

  it('renders leave requests after fetch', async () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockResolvedValue(mockRequests);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('leave-request-row-req-1')).toBeInTheDocument();
      expect(screen.getByTestId('leave-request-row-req-2')).toBeInTheDocument();
    });
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Jones')).toBeInTheDocument();
  });

  it('filters to Pending tab', async () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockResolvedValue(mockRequests);
    renderPage();
    await waitFor(() => screen.getByTestId('leave-request-row-req-1'));

    fireEvent.click(screen.getByText('Pending'));
    expect(screen.getByTestId('leave-request-row-req-1')).toBeInTheDocument();
    expect(screen.queryByTestId('leave-request-row-req-2')).not.toBeInTheDocument();
  });

  it('shows approve and reject buttons for pending requests', async () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockResolvedValue(mockRequests);
    renderPage();
    await waitFor(() => screen.getByTestId('approve-btn-req-1'));

    expect(screen.getByTestId('approve-btn-req-1')).toBeInTheDocument();
    expect(screen.getByTestId('reject-btn-req-1')).toBeInTheDocument();
    expect(screen.getByTestId('cancel-btn-req-1')).toBeInTheDocument();
    expect(screen.queryByTestId('approve-btn-req-2')).not.toBeInTheDocument();
  });

  it('shows error alert when fetch fails', async () => {
    (leaveRequestApi.fetchLeaveRequests as jest.Mock).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('opens the apply leave dialog on button click', async () => {
    renderPage();
    fireEvent.click(screen.getByTestId('apply-leave-btn'));
    await waitFor(() => {
      expect(screen.getByTestId('leave-request-form')).toBeInTheDocument();
    });
  });
});
