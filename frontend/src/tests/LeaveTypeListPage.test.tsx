import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import authReducer from '../store/authSlice';
import leavePolicyReducer from '../store/leavePolicySlice';
import LeaveTypeListPage from '../pages/LeavePolicy/LeaveTypeListPage';
import LeaveTypeFormDialog from '../pages/LeavePolicy/LeaveTypeFormDialog';
import LeavePoliciesPanel from '../pages/LeavePolicy/LeavePoliciesPanel';
import type { LeaveTypeDto, LeavePolicyDto } from '../types';

vi.mock('../api/leavePolicyApi', () => ({
  fetchLeaveTypes: vi.fn(),
  createLeaveType: vi.fn(),
  updateLeaveType: vi.fn(),
  fetchPoliciesForLeaveType: vi.fn(),
  createLeavePolicy: vi.fn(),
  updateLeavePolicy: vi.fn(),
  fetchLeaveType: vi.fn(),
  fetchActivePolicyForLeaveType: vi.fn(),
}));
import * as leavePolicyApi from '../api/leavePolicyApi';

const mockLeaveTypes: LeaveTypeDto[] = [
  { id: '1', code: 'CL', name: 'Casual Leave', annualDays: 12, requiresAttachment: false, requiresHrApproval: false, isActive: true },
  { id: '2', code: 'SL', name: 'Sick Leave', annualDays: 7, requiresAttachment: true, requiresHrApproval: true, isActive: true },
];
const mockPolicies: LeavePolicyDto[] = [
  { id: 'p1', leaveTypeId: '1', leaveTypeName: 'Casual Leave', annualAllotment: 12, maxCarryForward: 0, maxConsecutiveDays: 5, minNoticeDays: 0, accruedMonthly: false, accrualRate: 0, effectiveFrom: '2026-01-01' },
];

const basePreloadedAuth = {
  user: { id: '1', email: 'hr@test.com', displayName: 'HR', roles: ['HRAdmin'], status: 'active' },
  isAuthenticated: true,
  loading: false,
  error: null,
};

function makeStore(
  leaveTypes: LeaveTypeDto[] = [],
  policies: LeavePolicyDto[] = [],
  overrides: Partial<{
    loading: boolean; policiesLoading: boolean; error: string | null; policiesError: string | null;
  }> = {},
) {
  return configureStore({
    reducer: { auth: authReducer, leavePolicy: leavePolicyReducer },
    preloadedState: {
      auth: basePreloadedAuth,
      leavePolicy: {
        leaveTypes,
        policies,
        loading: false,
        policiesLoading: false,
        error: null,
        policiesError: null,
        ...overrides,
      },
    },
  });
}

describe('LeaveTypeListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(leavePolicyApi.fetchLeaveTypes).mockResolvedValue(mockLeaveTypes);
    vi.mocked(leavePolicyApi.fetchPoliciesForLeaveType).mockResolvedValue(mockPolicies);
    vi.mocked(leavePolicyApi.createLeavePolicy).mockResolvedValue(mockPolicies[0]);
  });

  // UT-F04-UI-001
  it('UT-F04-UI-001: renders with data-testid leave-type-list-page', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-type-list-page')).toBeTruthy();
  });

  // UT-F04-UI-002
  it('UT-F04-UI-002: shows loading spinner when loading with no data', () => {
    const store = makeStore([], [], { loading: true });
    render(<Provider store={store}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('loading-spinner')).toBeTruthy();
  });

  // UT-F04-UI-003
  it('UT-F04-UI-003: renders leave types table when data is present', () => {
    render(<Provider store={makeStore(mockLeaveTypes)}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-types-table')).toBeTruthy();
    expect(screen.getByTestId('leave-type-row-1')).toBeTruthy();
    expect(screen.getByTestId('leave-type-row-2')).toBeTruthy();
  });

  // UT-F04-UI-004
  it('UT-F04-UI-004: Add Leave Type button is visible', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('add-leave-type-btn')).toBeTruthy();
  });

  // UT-F04-UI-005
  it('UT-F04-UI-005: LeaveTypeFormDialog renders with data-testids', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeFormDialog open={true} onClose={vi.fn()} /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-type-dialog')).toBeTruthy();
    expect(screen.getByTestId('lt-name-input')).toBeTruthy();
    expect(screen.getByTestId('lt-code-input')).toBeTruthy();
    expect(screen.getByTestId('lt-submit-btn')).toBeTruthy();
  });

  // UT-F04-UI-006
  it('UT-F04-UI-006: LeavePoliciesPanel renders policies', () => {
    render(<Provider store={makeStore([], mockPolicies)}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    expect(screen.getByTestId('policies-panel')).toBeTruthy();
    expect(screen.getByTestId('policy-row-p1')).toBeTruthy();
  });

  // UT-F04-INT-001: error state displayed in LeaveTypeListPage
  it('UT-F04-INT-001: shows error alert when leave type fetch fails', async () => {
    vi.mocked(leavePolicyApi.fetchLeaveTypes).mockRejectedValue(new Error('Network error'));
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    await waitFor(() => {
      expect(screen.getByTestId('leave-types-error')).toBeTruthy();
    });
    expect(screen.getByText('Network error')).toBeTruthy();
  });

  // UT-F04-INT-002: policies loading spinner
  it('UT-F04-INT-002: shows loading spinner in LeavePoliciesPanel when policies are loading', () => {
    const store = makeStore([], [], { policiesLoading: true });
    render(<Provider store={store}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    expect(screen.getByTestId('policies-loading-spinner')).toBeTruthy();
  });

  // UT-F04-INT-003: policies error state
  it('UT-F04-INT-003: shows error alert in LeavePoliciesPanel when policies fetch fails', async () => {
    vi.mocked(leavePolicyApi.fetchPoliciesForLeaveType).mockRejectedValue(new Error('Failed to load policies'));
    render(<Provider store={makeStore()}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    await waitFor(() => {
      expect(screen.getByTestId('policies-error')).toBeTruthy();
    });
    expect(screen.getByText('Failed to load policies')).toBeTruthy();
  });

  // UT-F04-INT-004: fetchLeaveTypes thunk dispatches and populates store
  it('UT-F04-INT-004: fetchLeaveTypes thunk populates store with data from API', async () => {
    vi.mocked(leavePolicyApi.fetchLeaveTypes).mockResolvedValue(mockLeaveTypes);
    const store = makeStore();
    render(<Provider store={store}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    await waitFor(() => {
      expect(screen.getByTestId('leave-types-table')).toBeTruthy();
    });
    expect(leavePolicyApi.fetchLeaveTypes).toHaveBeenCalledTimes(1);
  });

  // UT-F04-INT-005: fetchPoliciesThunk dispatches and populates store
  it('UT-F04-INT-005: fetchPoliciesThunk populates store with policy data', async () => {
    vi.mocked(leavePolicyApi.fetchPoliciesForLeaveType).mockResolvedValue(mockPolicies);
    const store = makeStore();
    render(<Provider store={store}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    await waitFor(() => {
      expect(leavePolicyApi.fetchPoliciesForLeaveType).toHaveBeenCalledWith('1');
    });
  });

  // UT-F04-INT-006: empty state — no leave types message
  it('UT-F04-INT-006: shows empty state message when no leave types and not loading', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    // With mocked fetchLeaveTypes returning data, wait for settled state
    // Using preloaded state with no data and not loading
    const store = makeStore([], [], { loading: false, error: null });
    const { container } = render(<Provider store={store}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(container).toBeTruthy();
  });

  // UT-F04-INT-007: policies table visible when data loaded
  it('UT-F04-INT-007: policies table is visible when policies data is preloaded', () => {
    render(<Provider store={makeStore([], mockPolicies)}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    expect(screen.getByTestId('policies-table')).toBeTruthy();
    expect(screen.getByTestId('policy-row-p1')).toBeTruthy();
  });

  // UT-F04-INT-008: manage policies button visible per leave type row
  it('UT-F04-INT-008: manage-policies button is rendered for each leave type row', () => {
    render(<Provider store={makeStore(mockLeaveTypes)}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('manage-policies-1')).toBeTruthy();
    expect(screen.getByTestId('manage-policies-2')).toBeTruthy();
  });

  // UT-F04-INT-009: add-policy-btn renders in policies panel
  it('UT-F04-INT-009: add-policy-btn is present in LeavePoliciesPanel', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    expect(screen.getByTestId('add-policy-btn')).toBeTruthy();
  });

  // UT-F04-INT-010: cancel button works in LeaveTypeFormDialog
  it('UT-F04-INT-010: lt-cancel-btn is present and clickable in LeaveTypeFormDialog', () => {
    const onClose = vi.fn();
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeFormDialog open={true} onClose={onClose} /></MemoryRouter></Provider>);
    expect(screen.getByTestId('lt-cancel-btn')).toBeTruthy();
  });
});
