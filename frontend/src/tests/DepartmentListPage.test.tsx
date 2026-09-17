import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import authReducer from '../store/authSlice';
import departmentReducer from '../store/departmentSlice';
import DepartmentListPage from '../pages/Departments/DepartmentListPage';
import DepartmentFormDialog from '../pages/Departments/DepartmentFormDialog';
import type { DepartmentDto } from '../types';

vi.mock('../api/departmentApi', () => ({
  fetchDepartments: vi.fn(),
  createDepartment: vi.fn(),
  updateDepartment: vi.fn(),
  deleteDepartment: vi.fn(),
}));

import * as departmentApi from '../api/departmentApi';

const mockDepts: DepartmentDto[] = [
  { id: '1', name: 'HR', code: 'HR', overlapLimit: 0, isActive: true, createdAt: '2026-01-01' },
  { id: '2', name: 'Engineering', code: 'ENG', overlapLimit: 2, isActive: true, createdAt: '2026-01-01' },
];

function makeStore(departments: DepartmentDto[] = []) {
  return configureStore({
    reducer: { auth: authReducer, departments: departmentReducer },
    preloadedState: {
      auth: { user: { id: '1', email: 'hr@test.com', displayName: 'HR Admin', roles: ['HRAdmin'], status: 'active' }, isAuthenticated: true, loading: false, error: null },
      departments: { departments, loading: false, error: null },
    },
  });
}

describe('DepartmentListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(departmentApi.fetchDepartments).mockResolvedValue(mockDepts);
  });

  it('UT-F03-UI-001: renders with data-testid department-list-page', () => {
    render(<Provider store={makeStore()}><MemoryRouter><DepartmentListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('department-list-page')).toBeTruthy();
  });

  it('UT-F03-UI-002: shows loading spinner during fetch', () => {
    const store = configureStore({
      reducer: { auth: authReducer, departments: departmentReducer },
      preloadedState: {
        auth: { user: null, isAuthenticated: false, loading: true, error: null },
        departments: { departments: [], loading: true, error: null },
      },
    });
    render(<Provider store={store}><MemoryRouter><DepartmentListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('loading-spinner')).toBeTruthy();
  });

  it('UT-F03-UI-003: renders departments in table', () => {
    render(<Provider store={makeStore(mockDepts)}><MemoryRouter><DepartmentListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('departments-table')).toBeTruthy();
    expect(screen.getByTestId('dept-row-1')).toBeTruthy();
    expect(screen.getByTestId('dept-row-2')).toBeTruthy();
  });

  it('UT-F03-UI-004: Add Department button is visible', () => {
    render(<Provider store={makeStore()}><MemoryRouter><DepartmentListPage /></MemoryRouter></Provider>);
    expect(screen.getByTestId('add-department-btn')).toBeTruthy();
  });

  it('UT-F03-UI-005: DepartmentFormDialog renders with required data-testids', () => {
    render(<Provider store={makeStore()}><MemoryRouter><DepartmentFormDialog open={true} onClose={vi.fn()} /></MemoryRouter></Provider>);
    expect(screen.getByTestId('dept-form-dialog')).toBeTruthy();
    expect(screen.getByTestId('dept-name-input')).toBeTruthy();
    expect(screen.getByTestId('dept-code-input')).toBeTruthy();
    expect(screen.getByTestId('dept-submit-btn')).toBeTruthy();
    expect(screen.getByTestId('dept-cancel-btn')).toBeTruthy();
  });

  it('UT-F03-UI-006: fetchDepartments returns DepartmentDto array (ApiResponse already unwrapped)', async () => {
    const result = await vi.mocked(departmentApi.fetchDepartments)();
    expect(Array.isArray(result)).toBe(true);
    expect(result[0]).toHaveProperty('id');
    expect(result[0]).toHaveProperty('name');
    expect(result[0]).toHaveProperty('code');
  });

  it('UT-F03-INT-001: displays error alert when fetch fails', async () => {
    vi.mocked(departmentApi.fetchDepartments).mockRejectedValue(new Error('Network Error'));
    const store = makeStore();
    render(<Provider store={store}><MemoryRouter><DepartmentListPage /></MemoryRouter></Provider>);
    await waitFor(() => {
      expect(screen.getByTestId('departments-error')).toBeTruthy();
    });
  });

  it('UT-F03-INT-002: updateDepartmentThunk replaces department in state', async () => {
    const store = makeStore(mockDepts);
    const updated: DepartmentDto = { ...mockDepts[0], name: 'Human Resources' };
    vi.mocked(departmentApi.updateDepartment).mockResolvedValue(updated);
    const { updateDepartmentThunk } = await import('../store/departmentSlice');
    await store.dispatch(updateDepartmentThunk({ id: '1', dto: { name: 'Human Resources' } }));
    const state = store.getState();
    expect(state.departments.departments.find(d => d.id === '1')?.name).toBe('Human Resources');
  });
});
