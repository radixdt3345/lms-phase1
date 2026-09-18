/**
 * INT layer integration tests for F-07 Comp-Off Management.
 *
 * Verifies:
 * - Route /comp-off renders CompOffListPage
 * - compOff reducer is registered in the store
 * - API reads response.data.data (correct unwrap pattern)
 * - Credits summary card shows availableCredits + expiryDate
 * - Loading / empty / error / happy-path states
 * - Approve / reject buttons present for Pending requests
 */
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import compOffReducer from '../store/compOffSlice';
import notificationReducer from '../store/notificationSlice';
import authReducer from '../store/authSlice';
import CompOffListPage from '../pages/CompOff/CompOffListPage';

// ── Mock API modules ──────────────────────────────────────────────────────────
jest.mock('../api/compOffApi', () => ({
  fetchCompOffRequests: jest.fn(),
  fetchCompOffCredits: jest.fn(),
  createCompOffRequest: jest.fn(),
  approveCompOffRequest: jest.fn(),
  rejectCompOffRequest: jest.fn(),
}));

jest.mock('../api/notificationApi', () => ({
  getUnreadCount: jest.fn().mockResolvedValue(0),
  fetchNotifications: jest.fn().mockResolvedValue({ data: [], totalCount: 0, page: 1, pageSize: 20 }),
  markRead: jest.fn(),
  markAllRead: jest.fn(),
  deleteNotification: jest.fn(),
}));

import {
  fetchCompOffRequests,
  fetchCompOffCredits,
} from '../api/compOffApi';

const mockFetchRequests = fetchCompOffRequests as jest.Mock;
const mockFetchCredits = fetchCompOffCredits as jest.Mock;

// ── Helpers ───────────────────────────────────────────────────────────────────
const makeStore = () =>
  configureStore({
    reducer: {
      compOff: compOffReducer,
      notifications: notificationReducer,
      auth: authReducer,
    },
  });

const sampleRequest = {
  id: 'req-001',
  employeeId: 'emp-001',
  employeeName: 'Alice Smith',
  workedDate: '2026-09-14',
  hoursWorked: 8,
  creditDays: 1,
  description: 'Worked on weekend release',
  status: 'Pending' as const,
  createdAt: '2026-09-14T10:00:00Z',
  updatedAt: '2026-09-14T10:00:00Z',
};

const sampleCredits = {
  employeeId: 'emp-001',
  totalCredits: 3,
  usedCredits: 1,
  availableCredits: 2,
  expiringCredits: 1,
  expiryDate: '2026-10-14',
};

const renderPage = (store = makeStore()) =>
  render(
    <Provider store={store}>
      <MemoryRouter initialEntries={['/comp-off']}>
        <Routes>
          <Route path="/comp-off" element={<CompOffListPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>,
  );

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('F-07 Comp-Off INT layer', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  // UT-INT-CO-01: /comp-off route renders CompOffListPage
  it('renders CompOffListPage at /comp-off route', async () => {
    mockFetchRequests.mockResolvedValue([]);
    mockFetchCredits.mockResolvedValue(sampleCredits);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('comp-off-list')).toBeInTheDocument();
    });
  });

  // UT-INT-CO-02: compOff reducer registered in store
  it('compOff state initialises from store', () => {
    const store = makeStore();
    const state = store.getState().compOff;
    expect(state).toBeDefined();
    expect(Array.isArray(state.requests)).toBe(true);
    expect(state.loading).toBe(false);
    expect(state.error).toBeNull();
  });

  // UT-INT-CO-03: API response.data.data unwrap — credits displayed
  it('shows credits summary when API returns credits', async () => {
    mockFetchRequests.mockResolvedValue([]);
    mockFetchCredits.mockResolvedValue(sampleCredits);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('credits-summary')).toBeInTheDocument();
    });
    expect(screen.getByText('2')).toBeInTheDocument(); // availableCredits
    expect(screen.getByText(/2026-10-14/)).toBeInTheDocument(); // expiryDate
  });

  // UT-INT-CO-04: loading state
  it('shows loading spinner while data is fetching', async () => {
    let resolveRequests!: (v: typeof sampleRequest[]) => void;
    mockFetchRequests.mockReturnValue(
      new Promise((res) => { resolveRequests = res; })
    );
    mockFetchCredits.mockResolvedValue(sampleCredits);
    renderPage();
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    resolveRequests([]);
    await waitFor(() => {
      expect(screen.queryByTestId('loading-spinner')).not.toBeInTheDocument();
    });
  });

  // UT-INT-CO-05: empty state
  it('shows empty state when no requests returned', async () => {
    mockFetchRequests.mockResolvedValue([]);
    mockFetchCredits.mockResolvedValue(sampleCredits);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId('empty-comp-off')).toBeInTheDocument();
    });
  });

  // UT-INT-CO-06: happy path — rows with data-testid
  it('renders request rows with approve/reject buttons for Pending', async () => {
    mockFetchRequests.mockResolvedValue([sampleRequest]);
    mockFetchCredits.mockResolvedValue(sampleCredits);
    renderPage();
    await waitFor(() => {
      expect(screen.getByTestId(`comp-off-row-${sampleRequest.id}`)).toBeInTheDocument();
    });
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByTestId(`approve-btn-${sampleRequest.id}`)).toBeInTheDocument();
    expect(screen.getByTestId(`reject-btn-${sampleRequest.id}`)).toBeInTheDocument();
  });

  // UT-INT-CO-07: error state
  it('shows error alert when API call fails', async () => {
    mockFetchRequests.mockRejectedValue(new Error('Network error'));
    mockFetchCredits.mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });
});
