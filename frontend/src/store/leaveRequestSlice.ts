import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as leaveRequestApi from '../api/leaveRequestApi';
import type { LeaveRequestDto, CreateLeaveRequestDto } from '../api/leaveRequestApi';

interface LeaveRequestState {
  requests: LeaveRequestDto[];
  loading: boolean;
  error: string | null;
  submitting: boolean;
}

const initialState: LeaveRequestState = {
  requests: [],
  loading: false,
  error: null,
  submitting: false,
};

export const fetchLeaveRequestsThunk = createAsyncThunk(
  'leaveRequests/fetchAll',
  () => leaveRequestApi.fetchLeaveRequests()
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
      .addCase(approveLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      })
      .addCase(rejectLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      })
      .addCase(cancelLeaveRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      });
  },
});

export default leaveRequestSlice.reducer;
