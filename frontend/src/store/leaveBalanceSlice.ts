import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as leaveBalanceApi from '../api/leaveBalanceApi';
import type { LeaveBalanceDto, AdjustBalanceDto } from '../api/leaveBalanceApi';

interface LeaveBalanceState {
  balances: LeaveBalanceDto[];
  loading: boolean;
  error: string | null;
  adjusting: boolean;
}

const initialState: LeaveBalanceState = {
  balances: [],
  loading: false,
  error: null,
  adjusting: false,
};

export const fetchMyLeaveBalancesThunk = createAsyncThunk(
  'leaveBalance/fetchMy',
  () => leaveBalanceApi.fetchMyLeaveBalances()
);

export const fetchAllLeaveBalancesThunk = createAsyncThunk(
  'leaveBalance/fetchAll',
  () => leaveBalanceApi.fetchAllLeaveBalances()
);

export const fetchLeaveBalancesByEmployeeThunk = createAsyncThunk(
  'leaveBalance/fetchByEmployee',
  (employeeId: string) => leaveBalanceApi.fetchLeaveBalancesByEmployee(employeeId)
);

export const adjustLeaveBalanceThunk = createAsyncThunk(
  'leaveBalance/adjust',
  (dto: AdjustBalanceDto) => leaveBalanceApi.adjustLeaveBalance(dto)
);

const leaveBalanceSlice = createSlice({
  name: 'leaveBalance',
  initialState,
  reducers: {
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMyLeaveBalancesThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchMyLeaveBalancesThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.balances = action.payload;
      })
      .addCase(fetchMyLeaveBalancesThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch leave balances';
      })
      .addCase(fetchAllLeaveBalancesThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchAllLeaveBalancesThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.balances = action.payload;
      })
      .addCase(fetchAllLeaveBalancesThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch leave balances';
      })
      .addCase(fetchLeaveBalancesByEmployeeThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchLeaveBalancesByEmployeeThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.balances = action.payload;
      })
      .addCase(fetchLeaveBalancesByEmployeeThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch leave balances';
      })
      .addCase(adjustLeaveBalanceThunk.pending, (state) => {
        state.adjusting = true;
        state.error = null;
      })
      .addCase(adjustLeaveBalanceThunk.fulfilled, (state, action) => {
        state.adjusting = false;
        const idx = state.balances.findIndex((b) => b.id === action.payload.id);
        if (idx !== -1) {
          state.balances[idx] = action.payload;
        } else {
          state.balances.push(action.payload);
        }
      })
      .addCase(adjustLeaveBalanceThunk.rejected, (state, action) => {
        state.adjusting = false;
        state.error = action.error.message ?? 'Failed to adjust balance';
      });
  },
});

export const { clearError } = leaveBalanceSlice.actions;
export default leaveBalanceSlice.reducer;
