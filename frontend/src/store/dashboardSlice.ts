import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import {
  fetchDashboardOverview,
  fetchTeamLeave,
  fetchApprovalSummary,
  fetchCompOffSummary,
} from '../api/dashboardApi';
import type {
  OverviewDashboardDto,
  TeamLeaveOverviewDto,
  ApprovalDashboardSummaryDto,
  CompOffSummaryDto,
} from '../api/dashboardApi';

interface DashboardState {
  overview: OverviewDashboardDto | null;
  teamLeave: TeamLeaveOverviewDto | null;
  approvalSummary: ApprovalDashboardSummaryDto | null;
  compOffSummary: CompOffSummaryDto | null;
  loadingOverview: boolean;
  loadingTeamLeave: boolean;
  loadingApprovals: boolean;
  loadingCompOff: boolean;
  error: string | null;
}

const initialState: DashboardState = {
  overview: null,
  teamLeave: null,
  approvalSummary: null,
  compOffSummary: null,
  loadingOverview: false,
  loadingTeamLeave: false,
  loadingApprovals: false,
  loadingCompOff: false,
  error: null,
};

export const loadDashboardOverview = createAsyncThunk(
  'dashboard/loadOverview',
  async () => fetchDashboardOverview(),
);

export const loadTeamLeave = createAsyncThunk(
  'dashboard/loadTeamLeave',
  async (managerId?: string) => fetchTeamLeave(managerId),
);

export const loadApprovalSummary = createAsyncThunk(
  'dashboard/loadApprovalSummary',
  async () => fetchApprovalSummary(),
);

export const loadCompOffSummary = createAsyncThunk(
  'dashboard/loadCompOffSummary',
  async () => fetchCompOffSummary(),
);

const dashboardSlice = createSlice({
  name: 'dashboard',
  initialState,
  reducers: {
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Overview
    builder
      .addCase(loadDashboardOverview.pending, (state) => {
        state.loadingOverview = true;
        state.error = null;
      })
      .addCase(loadDashboardOverview.fulfilled, (state, action: PayloadAction<OverviewDashboardDto>) => {
        state.loadingOverview = false;
        state.overview = action.payload;
      })
      .addCase(loadDashboardOverview.rejected, (state, action) => {
        state.loadingOverview = false;
        state.error = action.error.message ?? 'Failed to load dashboard overview';
      });

    // Team Leave
    builder
      .addCase(loadTeamLeave.pending, (state) => {
        state.loadingTeamLeave = true;
        state.error = null;
      })
      .addCase(loadTeamLeave.fulfilled, (state, action: PayloadAction<TeamLeaveOverviewDto>) => {
        state.loadingTeamLeave = false;
        state.teamLeave = action.payload;
      })
      .addCase(loadTeamLeave.rejected, (state, action) => {
        state.loadingTeamLeave = false;
        state.error = action.error.message ?? 'Failed to load team leave';
      });

    // Approval Summary
    builder
      .addCase(loadApprovalSummary.pending, (state) => {
        state.loadingApprovals = true;
        state.error = null;
      })
      .addCase(loadApprovalSummary.fulfilled, (state, action: PayloadAction<ApprovalDashboardSummaryDto>) => {
        state.loadingApprovals = false;
        state.approvalSummary = action.payload;
      })
      .addCase(loadApprovalSummary.rejected, (state, action) => {
        state.loadingApprovals = false;
        state.error = action.error.message ?? 'Failed to load approval summary';
      });

    // Comp Off Summary
    builder
      .addCase(loadCompOffSummary.pending, (state) => {
        state.loadingCompOff = true;
        state.error = null;
      })
      .addCase(loadCompOffSummary.fulfilled, (state, action: PayloadAction<CompOffSummaryDto>) => {
        state.loadingCompOff = false;
        state.compOffSummary = action.payload;
      })
      .addCase(loadCompOffSummary.rejected, (state, action) => {
        state.loadingCompOff = false;
        state.error = action.error.message ?? 'Failed to load comp-off summary';
      });
  },
});

export const { clearError } = dashboardSlice.actions;
export default dashboardSlice.reducer;
