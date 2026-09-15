import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as api from '../api/leavePolicyApi';
import type { LeaveTypeDto, LeavePolicyDto, CreateLeaveTypeDto } from '../types';

interface LeavePolicyState {
  leaveTypes: LeaveTypeDto[];
  policies: LeavePolicyDto[];
  loading: boolean;
  error: string | null;
}

export const fetchLeaveTypesThunk = createAsyncThunk('leavePolicy/fetchTypes', api.fetchLeaveTypes);
export const createLeaveTypeThunk = createAsyncThunk('leavePolicy/createType', (dto: CreateLeaveTypeDto) => api.createLeaveType(dto));
export const fetchPoliciesThunk = createAsyncThunk('leavePolicy/fetchPolicies', (leaveTypeId: string) => api.fetchPoliciesForLeaveType(leaveTypeId));

const leavePolicySlice = createSlice({
  name: 'leavePolicy',
  initialState: { leaveTypes: [], policies: [], loading: false, error: null } as LeavePolicyState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchLeaveTypesThunk.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchLeaveTypesThunk.fulfilled, (s, a) => { s.loading = false; s.leaveTypes = a.payload; })
      .addCase(fetchLeaveTypesThunk.rejected, (s, a) => { s.loading = false; s.error = a.error.message ?? 'Failed'; })
      .addCase(createLeaveTypeThunk.fulfilled, (s, a) => { s.leaveTypes.push(a.payload); })
      .addCase(fetchPoliciesThunk.fulfilled, (s, a) => { s.policies = a.payload; });
  },
});

export default leavePolicySlice.reducer;
