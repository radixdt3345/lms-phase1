import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as api from '../api/leavePolicyApi';
import type { LeaveTypeDto, LeavePolicyDto, CreateLeaveTypeDto, CreateLeavePolicyDto } from '../types';

interface LeavePolicyState {
  leaveTypes: LeaveTypeDto[];
  policies: LeavePolicyDto[];
  loading: boolean;
  policiesLoading: boolean;
  error: string | null;
  policiesError: string | null;
}

export const fetchLeaveTypesThunk = createAsyncThunk('leavePolicy/fetchTypes', api.fetchLeaveTypes);
export const createLeaveTypeThunk = createAsyncThunk('leavePolicy/createType', (dto: CreateLeaveTypeDto) => api.createLeaveType(dto));
export const updateLeaveTypeThunk = createAsyncThunk(
  'leavePolicy/updateType',
  ({ id, dto }: { id: string; dto: Partial<CreateLeaveTypeDto> }) => api.updateLeaveType(id, dto),
);
export const fetchPoliciesThunk = createAsyncThunk('leavePolicy/fetchPolicies', (leaveTypeId: string) => api.fetchPoliciesForLeaveType(leaveTypeId));
export const createLeavePolicyThunk = createAsyncThunk('leavePolicy/createPolicy', (dto: CreateLeavePolicyDto) => api.createLeavePolicy(dto));
export const updateLeavePolicyThunk = createAsyncThunk(
  'leavePolicy/updatePolicy',
  ({ id, dto }: { id: string; dto: Partial<CreateLeavePolicyDto> }) => api.updateLeavePolicy(id, dto),
);

const leavePolicySlice = createSlice({
  name: 'leavePolicy',
  initialState: {
    leaveTypes: [],
    policies: [],
    loading: false,
    policiesLoading: false,
    error: null,
    policiesError: null,
  } as LeavePolicyState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // ── Leave Types ──────────────────────────────────────────────────────────
      .addCase(fetchLeaveTypesThunk.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchLeaveTypesThunk.fulfilled, (s, a) => { s.loading = false; s.leaveTypes = a.payload; })
      .addCase(fetchLeaveTypesThunk.rejected, (s, a) => { s.loading = false; s.error = a.error.message ?? 'Failed to load leave types'; })
      .addCase(createLeaveTypeThunk.pending, (s) => { s.error = null; })
      .addCase(createLeaveTypeThunk.fulfilled, (s, a) => { s.leaveTypes.push(a.payload); })
      .addCase(createLeaveTypeThunk.rejected, (s, a) => { s.error = a.error.message ?? 'Failed to create leave type'; })
      .addCase(updateLeaveTypeThunk.fulfilled, (s, a) => {
        const idx = s.leaveTypes.findIndex((lt) => lt.id === a.payload.id);
        if (idx !== -1) s.leaveTypes[idx] = a.payload;
      })
      .addCase(updateLeaveTypeThunk.rejected, (s, a) => { s.error = a.error.message ?? 'Failed to update leave type'; })
      // ── Leave Policies ───────────────────────────────────────────────────────
      .addCase(fetchPoliciesThunk.pending, (s) => { s.policiesLoading = true; s.policiesError = null; })
      .addCase(fetchPoliciesThunk.fulfilled, (s, a) => { s.policiesLoading = false; s.policies = a.payload; })
      .addCase(fetchPoliciesThunk.rejected, (s, a) => { s.policiesLoading = false; s.policiesError = a.error.message ?? 'Failed to load policies'; })
      .addCase(createLeavePolicyThunk.pending, (s) => { s.policiesError = null; })
      .addCase(createLeavePolicyThunk.fulfilled, (s, a) => { s.policies.push(a.payload); })
      .addCase(createLeavePolicyThunk.rejected, (s, a) => { s.policiesError = a.error.message ?? 'Failed to create policy'; })
      .addCase(updateLeavePolicyThunk.fulfilled, (s, a) => {
        const idx = s.policies.findIndex((p) => p.id === a.payload.id);
        if (idx !== -1) s.policies[idx] = a.payload;
      })
      .addCase(updateLeavePolicyThunk.rejected, (s, a) => { s.policiesError = a.error.message ?? 'Failed to update policy'; });
  },
});

export default leavePolicySlice.reducer;
