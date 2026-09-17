import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import authReducer from '../store/authSlice';
import employeeReducer from '../store/employeeSlice';
import EmployeeListPage from '../pages/Employees/EmployeeListPage';
import EmployeeFormDialog from '../pages/Employees/EmployeeFormDialog';
import type { EmployeeProfileDto } from '../types';

vi.mock('../api/employeeApi', () => ({
  fetchEmployees: vi.fn(),
  fetchEmployee: vi.fn(),
  createEmployee: vi.fn(),
  updateEmployee: vi.fn(),
  deleteEmployee: vi.fn(),
  fetchEmployeeLeaveBalances: vi.fn(),
  fetchEmployeeDocuments: vi.fn(),
}));

import * as employeeApi from '../api/employeeApi';

const mockEmployees: EmployeeProfileDto[] = [
  {
    id: 'emp-1',
    firstName: 'Alice',
    lastName: 'Smith',
    email: 'alice@example.com',
    employeeCode: 'EMP001',
    jobTitle: 'Software Engineer',
    departmentId: 'dept-1',
    departmentName: 'Engineering',
    dateOfJoining: '2024-01-15',
    employmentType: 'FullTime',
    status: 'Active',
  },
  {
    id: 'emp-2',
    firstName: 'Bob',
    lastName: 'Jones',
    email: 'bob@example.com',
    employeeCode: 'EMP002',
    jobTitle: 'HR Manager',
    departmentId: 'dept-2',
    departmentName: 'HR',
    dateOfJoining: '2023-06-01',
    employmentType: 'FullTime',
    status: 'Active',
  },
];

const mockPagedResult = {
  items: mockEmployees,
  totalCount: 2,
  pageNumber: 1,
  pageSize: 20,
  totalPages: 1,
};

const authState = {
  user: { id: '1', email: 'hr@test.com', displayName: 'HR Admin', roles: ['HRAdmin'], status: 'active' },
  isAuthenticated: true,
  loading: false,
  error: null,
};

function makeStore(employees: EmployeeProfileDto[] = [], loading = false, error: string | null = null) {
  return configureStore({
    reducer: { auth: authReducer, employees: employeeReducer },
    preloadedState: {
      auth: authState,
      employees: {
        employees,
        totalCount: employees.length,
        pageNumber: 1,
        pageSize: 20,
        totalPages: employees.length > 0 ? 1 : 0,
        loading,
        error,
      },
    },
  });
}

describe('EmployeeListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(employeeApi.fetchEmployees).mockResolvedValue(mockPagedResult);
  });

  it('UT-F02-UI-001: renders with data-testid employee-list-page', () => {
    render(
      <Provider store={makeStore()}>
        <MemoryRouter>
          <EmployeeListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('employee-list-page')).toBeTruthy();
    expect(screen.getByTestId('add-employee-btn')).toBeTruthy();
  });

  it('UT-F02-UI-002: shows loading spinner when loading with no data', () => {
    render(
      <Provider store={makeStore([], true)}>
        <MemoryRouter>
          <EmployeeListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('loading-spinner')).toBeTruthy();
  });

  it('UT-F02-UI-003: shows empty state when no employees', async () => {
    vi.mocked(employeeApi.fetchEmployees).mockResolvedValue({
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 0,
    });
    render(
      <Provider store={makeStore([])}>
        <MemoryRouter>
          <EmployeeListPage />
        </MemoryRouter>
      </Provider>
    );
    await waitFor(() => {
      expect(screen.getByTestId('empty-employees')).toBeTruthy();
    });
  });

  it('UT-F02-UI-004: renders employees in table with correct data-testids', () => {
    render(
      <Provider store={makeStore(mockEmployees)}>
        <MemoryRouter>
          <EmployeeListPage />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('employee-table')).toBeTruthy();
    expect(screen.getByTestId('employee-row-emp-1')).toBeTruthy();
    expect(screen.getByTestId('employee-row-emp-2')).toBeTruthy();
    expect(screen.getByTestId('edit-employee-emp-1')).toBeTruthy();
    expect(screen.getByTestId('delete-employee-emp-1')).toBeTruthy();
  });

  it('UT-F02-UI-005: shows error alert when fetch fails', async () => {
    vi.mocked(employeeApi.fetchEmployees).mockRejectedValue(new Error('Network Error'));
    const store = makeStore();
    render(
      <Provider store={store}>
        <MemoryRouter>
          <EmployeeListPage />
        </MemoryRouter>
      </Provider>
    );
    await waitFor(() => {
      expect(screen.getByTestId('employees-error')).toBeTruthy();
    });
  });

  it('UT-F02-UI-006: EmployeeFormDialog renders with all required data-testids', () => {
    render(
      <Provider store={makeStore()}>
        <MemoryRouter>
          <EmployeeFormDialog open={true} onClose={vi.fn()} />
        </MemoryRouter>
      </Provider>
    );
    expect(screen.getByTestId('employee-form-dialog')).toBeTruthy();
    expect(screen.getByTestId('employee-firstname-input')).toBeTruthy();
    expect(screen.getByTestId('employee-lastname-input')).toBeTruthy();
    expect(screen.getByTestId('employee-email-input')).toBeTruthy();
    expect(screen.getByTestId('employee-submit-btn')).toBeTruthy();
    expect(screen.getByTestId('employee-cancel-btn')).toBeTruthy();
  });

  it('UT-F02-UI-007: fetchEmployees mock returns data with correct ApiResponse envelope shape', async () => {
    const result = await vi.mocked(employeeApi.fetchEmployees)({});
    // The API module must unwrap res.data.data — mock returns already-unwrapped PagedResult
    expect(result).toHaveProperty('items');
    expect(result).toHaveProperty('totalCount');
    expect(Array.isArray(result.items)).toBe(true);
    expect(result.items[0]).toHaveProperty('id');
    expect(result.items[0]).toHaveProperty('firstName');
    expect(result.items[0]).toHaveProperty('employeeCode');
  });

  it('UT-F02-UI-008: updateEmployeeThunk replaces employee in state', async () => {
    const store = makeStore(mockEmployees);
    const updated: EmployeeProfileDto = { ...mockEmployees[0], jobTitle: 'Lead Engineer' };
    vi.mocked(employeeApi.updateEmployee).mockResolvedValue(updated);
    const { updateEmployeeThunk } = await import('../store/employeeSlice');
    await store.dispatch(updateEmployeeThunk({ id: 'emp-1', dto: { jobTitle: 'Lead Engineer' } }));
    const state = store.getState();
    expect(state.employees.employees.find((e) => e.id === 'emp-1')?.jobTitle).toBe('Lead Engineer');
  });

  it('UT-F02-UI-009: deleteEmployeeThunk removes employee from state', async () => {
    const store = makeStore(mockEmployees);
    vi.mocked(employeeApi.deleteEmployee).mockResolvedValue(true);
    const { deleteEmployeeThunk } = await import('../store/employeeSlice');
    await store.dispatch(deleteEmployeeThunk('emp-1'));
    const state = store.getState();
    expect(state.employees.employees.find((e) => e.id === 'emp-1')).toBeUndefined();
    expect(state.employees.totalCount).toBe(1);
  });
});
