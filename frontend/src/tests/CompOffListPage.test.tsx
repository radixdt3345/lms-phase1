import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import compOffReducer from '../store/compOffSlice';
import CompOffListPage from '../pages/CompOff/CompOffListPage';
import * as compOffApi from '../api/compOffApi';

vi.mock('../api/compOffApi');

const mockRequests = [
  {
    id: 'co-1',
    employeeId: 'emp-1',
    employeeName: 'Alice Smith',
    workedDate: '2024-02-10',
    hoursWorked: 8,
    creditDays: 1,
    description: 'Weekend deployment',
    status: 'Pending',
    createdAt: '2024-02-11T00:00:00Z',
    updatedAt: '2024-02-11T00:00:00Z',
  },
  {
    id: 'co-2',
    employeeId: 'emp-2',
    employeeName: 'Bob Jones',
    workedDate: '2024-01-20',
    hoursWorked: 5,
    creditDays: 0.5,
    description: 'Saturday support',
    status: 'Approved',
    createdAt: '2024-01-21T00:00:00Z',
    updatedAt: '2024-01-22T00:00:00Z',
  },
];

const mockCredits = {
  employeeId: 'emp-1',
  totalCredits: 3,
  usedCredits: 1,
  availableCredits: 2,
  expiringCredits: 0.5,
  expiryDate: '2024-04-10',
};

const buildStore = (preloadedState = {}) =>
  configureStore({
    reducer: { compOff: compOffReducer },
    preloadedState,
  });

const renderPage = (store = buildStore()) =>
  render(
    <Provider store={store}>
      <CompOffListPage />
    </Provider>
  );

describe('CompOffListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (compOffApi.fetchCompOffRequests as vi.Mock).mockResolvedValue([]);
    (compOffApi.fetchCompOffCredits as vi.Mock).mockResolvedValue(mockCredits);
    (compOffApi.approveCompOffRequest as vi.Mock).mockResolvedValue({
      ...mockRequests[0],
      status: 'Approved',
    });
    (compOffApi.rejectCompOffRequest as vi.Mock).mockResolvedValue({
      ...mockRequests[0],
      status: 'Rejected',
      rejectionReason: 'Not valid',
    });
  });

  it('renders the comp-off list container and heading', () => {
    renderPage();
    expect(screen.getByTestId('comp-off-list')).toBeInTheDocument();
    expect(screen.getByText('Comp-Off Requests')).toBeInTheDocument();
    expect(screen.getByTestId('apply-comp-off-btn')).toBeInTheDocument();
  });

  it('shows loading spinner on initial fetch', () => {
    (compOffApi.fetchCompOffRequests as vi.Mock).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
  });

  it('shows empty state when no requests exist', async () => {
    (compOffApi.fetchCompOffRequests as vi.Mock).mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('empty-comp-off')).toBeInTheDocument();
    });
  });

  it('renders comp-off request rows after fetch', async () => {
    (compOffApi.fetchCompOffRequests as vi.Mock).mockResolvedValue(mockRequests);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('comp-off-row-co-1')).toBeInTheDocument();
      expect(screen.getByTestId('comp-off-row-co-2')).toBeInTheDocument();
    });
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Weekend deployment')).toBeInTheDocument();
  });

  it('displays credits summary card when credits are available', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('credits-summary')).toBeInTheDocument();
    });
    expect(screen.getByText('Comp-Off Credits Balance')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();  // totalCredits
    expect(screen.getByText('2')).toBeInTheDocument();  // availableCredits
  });

  it('shows approve and reject buttons only for pending requests', async () => {
    (compOffApi.fetchCompOffRequests as vi.Mock).mockResolvedValue(mockRequests);
    renderPage();
    await waitFor(() => screen.getByTestId('approve-btn-co-1'));
    expect(screen.getByTestId('approve-btn-co-1')).toBeInTheDocument();
    expect(screen.getByTestId('reject-btn-co-1')).toBeInTheDocument();
    expect(screen.queryByTestId('approve-btn-co-2')).not.toBeInTheDocument();
  });

  it('shows error alert when fetch fails', async () => {
    (compOffApi.fetchCompOffRequests as vi.Mock).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('opens the comp-off form dialog on button click', async () => {
    renderPage();
    fireEvent.click(screen.getByTestId('apply-comp-off-btn'));
    await waitFor(() => {
      expect(screen.getByTestId('comp-off-form')).toBeInTheDocument();
    });
    expect(screen.getByTestId('worked-date-input')).toBeInTheDocument();
    expect(screen.getByTestId('hours-input')).toBeInTheDocument();
    expect(screen.getByTestId('description-input')).toBeInTheDocument();
    expect(screen.getByTestId('submit-btn')).toBeInTheDocument();
  });
});
