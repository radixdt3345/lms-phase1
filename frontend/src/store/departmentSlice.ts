import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as departmentApi from '../api/departmentApi';
import type { DepartmentDto, CreateDepartmentDto, UpdateDepartmentDto } from '../types';

interface DepartmentState {
  departments: DepartmentDto[];
  loading: boolean;
  error: string | null;
}
const initialState: DepartmentState = { departments: [], loading: false, error: null };

export const fetchDepartmentsThunk = createAsyncThunk('departments/fetchAll', departmentApi.fetchDepartments);
export const createDepartmentThunk = createAsyncThunk('departments/create', (dto: CreateDepartmentDto) => departmentApi.createDepartment(dto));
export const updateDepartmentThunk = createAsyncThunk('departments/update', ({ id, dto }: { id: string; dto: UpdateDepartmentDto }) => departmentApi.updateDepartment(id, dto));
export const deleteDepartmentThunk = createAsyncThunk('departments/delete', (id: string) => departmentApi.deleteDepartment(id));

const departmentSlice = createSlice({
  name: 'departments',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchDepartmentsThunk.pending, (state) => { state.loading = true; state.error = null; })
      .addCase(fetchDepartmentsThunk.fulfilled, (state, action) => { state.loading = false; state.departments = action.payload; })
      .addCase(fetchDepartmentsThunk.rejected, (state, action) => { state.loading = false; state.error = action.error.message ?? 'Failed'; })
      .addCase(createDepartmentThunk.fulfilled, (state, action) => { state.departments.push(action.payload); })
      .addCase(updateDepartmentThunk.fulfilled, (state, action) => {
        const idx = state.departments.findIndex(d => d.id === action.payload.id);
        if (idx !== -1) state.departments[idx] = action.payload;
      })
      .addCase(deleteDepartmentThunk.fulfilled, (state, action) => {
        const id = action.meta.arg;
        state.departments = state.departments.filter(d => d.id !== id);
      });
  },
});
export default departmentSlice.reducer;
