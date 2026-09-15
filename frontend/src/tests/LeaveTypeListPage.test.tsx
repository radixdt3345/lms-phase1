import { render, screen } from '@testing-library/react';
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
  fetchPoliciesForLeaveType: vi.fn(),
  createLeavePolicy: vi.fn(),
}));
import * as leavePolicyApi from '../api/leavePolicyApi';

const mockLeaveTypes: LeaveTypeDto[] = [
  { id: '1', code: 'CL', name: 'Casual Leave', annualDays: 12, requiresAttachment: false, requiresHrApproval: false, isActive: true },
  { id: '2', code: 'SL', name: 'Sick Leave', annualDays: 7, requiresAttachment: true, requiresHrApproval: true, isActive: true },
];
const mockPolicies: LeavePolicyDto[] = [
  { id: 'p1', leaveTypeId: '1', leaveTypeName: 'Casual Leave', annualAllotment: 12, maxCarryForward: 0, maxConsecutiveDays: 5, minNoticeDays: 0, accruedMonthly: false, accrualRate: 0, effectiveFrom: '2026-01-01' },
];

function makeStore(leaveTypes: LeaveTypeDto[] = [], policies: LeavePolicyDto[] = []) {
  return configureStore({
    reducer: { auth: authReducer, leavePolicy: leavePolicyReducer },
    preloadedState: {
      auth: { user: { id: '1', email: 'hr@test.com', displayName: 'HR', roles: ['HRAdmin'], status: 'active' }, isAuthenticated: true, loading: false, error: null },
      leavePolicy: { leaveTypes, policies, loading: false, error: null },
    },
  });
}

describe('LeaveTypeListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(leavePolicyApi.fetchLeaveTypes).mockResolvedValue(mockLeaveTypes);
    vi.mocked(leavePolicyApi.fetchPoliciesForLeaveType).mockResolvedValue(mockPolicies);
  });

  it('UT-F04-UI-001: renders with data-testid leave-type-list-page', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-type-list-page')).toBeTruthy();
  });
  it('UT-F04-UI-002: shows loading spinner', () => {
    const store = configureStore({
      reducer: { auth: authReducer, leavePolicy: leavePolicyReducer },
      preloadedState: {
        auth: { user: null, isAuthenticated: false, loading: false, error: null },
        leavePolicy: { leaveTypes: [], policies: [], loading: true, error: null },
      },
    });
    render(<Provider store={store}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('loading-spinner')).toBeTruthy();
  });
  it('UT-F04-UI-003: renders leave types table', () => {
    render(<Provider store={makeStore(mockLeaveTypes)}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-types-table')).toBeTruthy();
    expect(screen.getByTestId('leave-type-row-1')).toBeTruthy();
  });
  it('UT-F04-UI-004: Add Leave Type button is visible', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('add-leave-type-btn')).toBeTruthy();
  });
  it('UT-F04-UI-005: LeaveTypeFormDialog renders with data-testids', () => {
    render(<Provider store={makeStore()}><MemoryRouter><LeaveTypeFormDialog open={true} onClose={vi.fn()} /></MemoryRouter></Provider>);
    expect(screen.getByTestId('leave-type-dialog')).toBeTruthy();
    expect(screen.getByTestId('lt-name-input')).toBeTruthy();
    expect(screen.getByTestId('lt-code-input')).toBeTruthy();
    expect(screen.getByTestId('lt-submit-btn')).toBeTruthy();
  });
  it('UT-F04-UI-006: LeavePoliciesPanel renders policies', () => {
    render(<Provider store={makeStore([], mockPolicies)}><MemoryRouter><LeavePoliciesPanel leaveTypeId="1" /></MemoryRouter></Provider>);
    expect(screen.getByTestId('policies-panel')).toBeTruthy();
  });
});
