import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as employeeApi from '../api/employeeApi';
import type { EmployeeProfileDto, CreateEmployeeDto, UpdateEmployeeDto, PagedResult } from '../types';

interface EmployeeState {
  employees: EmployeeProfileDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  loading: boolean;
  error: string | null;
}

const initialState: EmployeeState = {
  employees: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 20,
  totalPages: 0,
  loading: false,
  error: null,
};

export const fetchEmployeesThunk = createAsyncThunk(
  'employees/fetchAll',
  (params: employeeApi.EmployeeQueryParams) => employeeApi.fetchEmployees(params)
);

export const createEmployeeThunk = createAsyncThunk(
  'employees/create',
  (dto: CreateEmployeeDto) => employeeApi.createEmployee(dto)
);

export const updateEmployeeThunk = createAsyncThunk(
  'employees/update',
  ({ id, dto }: { id: string; dto: UpdateEmployeeDto }) => employeeApi.updateEmployee(id, dto)
);

export const deleteEmployeeThunk = createAsyncThunk(
  'employees/delete',
  (id: string) => employeeApi.deleteEmployee(id)
);

const employeeSlice = createSlice({
  name: 'employees',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchEmployeesThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchEmployeesThunk.fulfilled, (state, action) => {
        state.loading = false;
        const result: PagedResult<EmployeeProfileDto> = action.payload;
        state.employees = result.items;
        state.totalCount = result.totalCount;
        state.pageNumber = result.pageNumber;
        state.pageSize = result.pageSize;
        state.totalPages = result.totalPages;
      })
      .addCase(fetchEmployeesThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch employees';
      })
      .addCase(createEmployeeThunk.fulfilled, (state, action) => {
        state.employees.unshift(action.payload);
        state.totalCount += 1;
      })
      .addCase(updateEmployeeThunk.fulfilled, (state, action) => {
        const idx = state.employees.findIndex((e) => e.id === action.payload.id);
        if (idx !== -1) state.employees[idx] = action.payload;
      })
      .addCase(deleteEmployeeThunk.fulfilled, (state, action) => {
        const id = action.meta.arg;
        state.employees = state.employees.filter((e) => e.id !== id);
        state.totalCount = Math.max(0, state.totalCount - 1);
      });
  },
});

export default employeeSlice.reducer;
