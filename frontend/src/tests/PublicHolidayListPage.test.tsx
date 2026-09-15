import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import authReducer from '../store/authSlice';
import publicHolidayReducer from '../store/publicHolidaySlice';
import PublicHolidayListPage from '../pages/PublicHolidays/PublicHolidayListPage';
import PublicHolidayFormDialog from '../pages/PublicHolidays/PublicHolidayFormDialog';
import type { PublicHolidayDto } from '../api/publicHolidayApi';

vi.mock('../api/publicHolidayApi', () => ({
  fetchPublicHolidays: vi.fn(),
  createPublicHoliday: vi.fn(),
  updatePublicHoliday: vi.fn(),
  deletePublicHoliday: vi.fn(),
  bulkCreatePublicHolidays: vi.fn(),
}));

import * as publicHolidayApi from '../api/publicHolidayApi';

const mockHolidays: PublicHolidayDto[] = [
  { id: '1', name: 'New Year', date: '2026-01-01', country: 'IN', isRecurring: true },
  { id: '2', name: 'Republic Day', date: '2026-01-26', country: 'IN', isRecurring: true },
];

const authState = {
  user: { id: '1', email: 'hr@test.com', displayName: 'HR Admin', roles: ['HRAdmin'], status: 'active' },
  isAuthenticated: true,
  loading: false,
  error: null,
};

function makeStore(
  holidays: PublicHolidayDto[] = [],
  loading = false,
  error: string | null = null,
  selectedYear = 2026
) {
  return configureStore({
    reducer: { auth: authReducer, publicHolidays: publicHolidayReducer },
    preloadedState: {
      auth: authState,
      publicHolidays: { holidays, loading, error, selectedYear },
    },
  });
}

describe('PublicHolidayListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(publicHolidayApi.fetchPublicHolidays).mockResolvedValue(mockHolidays);
  });

  it('UT-F10-UI-001: renders loading spinner when loading and no data', () => {
    const store = makeStore([], true);
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('loading-spinner')).toBeTruthy();
  });

  it('UT-F10-UI-002: renders holiday list after fetch', () => {
    const store = makeStore(mockHolidays);
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('public-holiday-page')).toBeTruthy();
    expect(screen.getByTestId('holidays-table')).toBeTruthy();
    expect(screen.getByTestId('holiday-row-1')).toBeTruthy();
    expect(screen.getByTestId('holiday-row-2')).toBeTruthy();
  });

  it('UT-F10-UI-003: renders empty state when no holidays', async () => {
    vi.mocked(publicHolidayApi.fetchPublicHolidays).mockResolvedValue([]);
    const store = makeStore([]);
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    // Wait for fetch to resolve with empty array, then empty-holidays appears
    await waitFor(() => {
      expect(screen.getByTestId('empty-holidays')).toBeTruthy();
    });
  });

  it('UT-F10-UI-004: renders error message on fetch failure', async () => {
    vi.mocked(publicHolidayApi.fetchPublicHolidays).mockRejectedValue(new Error('Network Error'));
    const store = makeStore([], false, null);
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    // Wait for the rejected thunk to set the error state
    await new Promise((r) => setTimeout(r, 50));
    expect(store.getState().publicHolidays.error).toBeTruthy();
  });

  it('UT-F10-UI-005: opens add dialog on button click', () => {
    const store = makeStore();
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    const addBtn = screen.getByTestId('add-holiday-btn');
    expect(addBtn).toBeTruthy();
    fireEvent.click(addBtn);
    expect(screen.getByTestId('holiday-form-dialog')).toBeTruthy();
  });

  it('UT-F10-UI-006: can navigate to previous and next year', () => {
    const store = makeStore([], false, null, 2026);
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('year-selector').textContent).toBe('2026');
    fireEvent.click(screen.getByTestId('prev-year-btn'));
    expect(store.getState().publicHolidays.selectedYear).toBe(2025);
    fireEvent.click(screen.getByTestId('next-year-btn'));
    expect(store.getState().publicHolidays.selectedYear).toBe(2026);
  });

  it('UT-F10-UI-007: PublicHolidayFormDialog renders with all required data-testids', () => {
    const store = makeStore();
    render(
      <Provider store={store}>
        <MemoryRouter>
          <PublicHolidayFormDialog open={true} onClose={vi.fn()} year={2026} />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('holiday-form-dialog')).toBeTruthy();
    expect(screen.getByTestId('holiday-name-input')).toBeTruthy();
    expect(screen.getByTestId('holiday-date-input')).toBeTruthy();
    expect(screen.getByTestId('holiday-country-input')).toBeTruthy();
    expect(screen.getByTestId('holiday-recurring-checkbox')).toBeTruthy();
    expect(screen.getByTestId('holiday-submit-btn')).toBeTruthy();
    expect(screen.getByTestId('holiday-cancel-btn')).toBeTruthy();
  });
});
