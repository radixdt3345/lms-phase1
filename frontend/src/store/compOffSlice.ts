import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as compOffApi from '../api/compOffApi';
import type { CompOffRequestDto, CreateCompOffRequestDto, CompOffCreditDto } from '../api/compOffApi';

interface CompOffState {
  requests: CompOffRequestDto[];
  credits: CompOffCreditDto | null;
  loading: boolean;
  creditsLoading: boolean;
  error: string | null;
  submitting: boolean;
}

const initialState: CompOffState = {
  requests: [],
  credits: null,
  loading: false,
  creditsLoading: false,
  error: null,
  submitting: false,
};

export const fetchCompOffRequestsThunk = createAsyncThunk(
  'compOff/fetchAll',
  () => compOffApi.fetchCompOffRequests()
);

export const createCompOffRequestThunk = createAsyncThunk(
  'compOff/create',
  (dto: CreateCompOffRequestDto) => compOffApi.createCompOffRequest(dto)
);

export const approveCompOffRequestThunk = createAsyncThunk(
  'compOff/approve',
  (id: string) => compOffApi.approveCompOffRequest(id)
);

export const rejectCompOffRequestThunk = createAsyncThunk(
  'compOff/reject',
  ({ id, reason }: { id: string; reason: string }) => compOffApi.rejectCompOffRequest(id, reason)
);

export const fetchCompOffCreditsThunk = createAsyncThunk(
  'compOff/fetchCredits',
  () => compOffApi.fetchCompOffCredits()
);

const compOffSlice = createSlice({
  name: 'compOff',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchCompOffRequestsThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchCompOffRequestsThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.requests = action.payload;
      })
      .addCase(fetchCompOffRequestsThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch comp-off requests';
      })
      .addCase(createCompOffRequestThunk.pending, (state) => {
        state.submitting = true;
        state.error = null;
      })
      .addCase(createCompOffRequestThunk.fulfilled, (state, action) => {
        state.submitting = false;
        state.requests.unshift(action.payload);
      })
      .addCase(createCompOffRequestThunk.rejected, (state, action) => {
        state.submitting = false;
        state.error = action.error.message ?? 'Failed to submit comp-off request';
      })
      .addCase(approveCompOffRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      })
      .addCase(rejectCompOffRequestThunk.fulfilled, (state, action) => {
        const idx = state.requests.findIndex((r) => r.id === action.payload.id);
        if (idx !== -1) state.requests[idx] = action.payload;
      })
      .addCase(fetchCompOffCreditsThunk.pending, (state) => {
        state.creditsLoading = true;
      })
      .addCase(fetchCompOffCreditsThunk.fulfilled, (state, action) => {
        state.creditsLoading = false;
        state.credits = action.payload;
      })
      .addCase(fetchCompOffCreditsThunk.rejected, (state) => {
        state.creditsLoading = false;
      });
  },
});

export default compOffSlice.reducer;
