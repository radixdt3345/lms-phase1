import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as leaveRequestApi from '../api/leaveRequestApi';
import type { LeaveRequestDto, CreateLeaveRequestDto } from '../api/leaveRequestApi';

interface LeaveRequestState {
  requests: LeaveRequestDto[];
  pendingRequests: LeaveRequestDto[];
  loading: boolean;
  pendingLoading: boolean;
  error: string | null;
  submitting: boolean;
}

const initialState: LeaveRequestState = {
  requests: [],
  pendingRequests: [],
  loading: false,
  pendingLoading: false,
  error: null,
  submitting: false,
};

export const fetchLeaveRequestsThunk = createAsyncThunk(
  'leaveRequests/fetchAll',
  () => leaveRequestApi.fetchLeaveRequests()
);

/** Fetches leave requests pending approval — Manager/HRAdmin only. */
export const fetchPendingLeaveRequestsThunk = createAsyncThunk(
  'leaveRequests/fetchPending',
  () => leaveRequestApi.fetchPendingLeaveRequests()
);

export const createLeaveRequestThunk = createAsyncThunk(
  'leaveRequests/create',
  (dto: CreateLeaveRequestDto) => leaveRequestApi.createLeaveRequest(dto)
);

export const approveLeaveRequestThunk = createAsyncThunk(
  'leaveRequests/approve',
  (id: string) => leaveRequestApi.approveLeaveRequest(id)
);

export const rejectLeaveRequestThunk = createAsyncThunk(
  'leaveRequests/reject',
  ({ id, reason }: { id: string; reason: string }) => leaveRequestApi.rejectLeaveRequest(id, reason)
);

export const cancelLeaveRequestThunk = createAsyncThunk(
  'leaveRequests/cancel',
  (id: string) => leaveRequestApi.cancelLeaveRequest(id)
);

const leaveRequestSlice = createSlice({
  name: 'leaveRequests',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // fetchAll
      .addCase(fetchLeaveRequestsThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchLeaveRequestsThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.requests = action.payload;
      })
      .addCase(fetchLeaveRequestsThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch leave requests';
      })
      // fetchPending
      .addCase(fetchPendingLeaveRequestsThunk.pending, (state) => {
        state.pendingLoading = true;
        state.error = null;
      })
      .addCase(fetchPendingLeaveRequestsThunk.fulfilled, (state, action) => {
        state.pendingLoading = false;
        state.pendingRequests = action.payload;
      })
      .addCase(fetchPendingLeaveRequestsThunk.rejected, (state, action) => {
        state.pendingLoading = false;
        state.error = action.error.message ?? 'Failed to fetch pending leave requests';
      })
      // create
      .addCase(createLeaveRequestThunk.pending, (state) => {
        state.submitting = true;
        state.error = null;
      })
      .addCase(createLeaveRequestThunk.fulfilled, (state, action) => {
        state.submitting = false;
        state.requests.unshift(action.payload);
      })
      .addCase(createLeaveRequestThunk.rejected, (state, action) => {
        state.submitting = false;
        state.error = action.error.message ?? 'Failed to submit leave request';
      })
      // approve
      .addCase(approveLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
        // Also update pendingRequests — remove from pending list on approval
        state.pendingRequests = state.pendingRequests.filter((r) => r.id !== action.payload.id);
      })
      // reject
      .addCase(rejectLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
        // Remove from pending list on rejection
        state.pendingRequests = state.pendingRequests.filter((r) => r.id !== action.payload.id);
      })
      // cancel
      .addCase(cancelLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      });
  },
});

export default leaveRequestSlice.reducer;
