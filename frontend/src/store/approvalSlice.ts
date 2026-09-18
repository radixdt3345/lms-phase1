import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import {
  fetchPendingApprovals,
  fetchApprovalHistory,
  fetchApprovalStats,
  escalateApproval,
  PendingApproval,
  ApprovalRecord,
} from '../api/approvalApi';

interface ApprovalStats {
  pending: number;
  approved: number;
  rejected: number;
}

interface ApprovalState {
  pending: PendingApproval[];
  history: ApprovalRecord[];
  stats: ApprovalStats | null;
  loadingPending: boolean;
  loadingHistory: boolean;
  loadingStats: boolean;
  escalating: string | null;
  error: string | null;
}

const initialState: ApprovalState = {
  pending: [],
  history: [],
  stats: null,
  loadingPending: false,
  loadingHistory: false,
  loadingStats: false,
  escalating: null,
  error: null,
};

export const loadPendingApprovals = createAsyncThunk(
  'approvals/loadPending',
  async () => {
    return fetchPendingApprovals();
  },
);

export const loadApprovalHistory = createAsyncThunk(
  'approvals/loadHistory',
  async () => {
    return fetchApprovalHistory();
  },
);

export const loadApprovalStats = createAsyncThunk(
  'approvals/loadStats',
  async () => {
    return fetchApprovalStats();
  },
);

export const escalateApprovalAsync = createAsyncThunk(
  'approvals/escalate',
  async ({ id, reason }: { id: string; reason: string }) => {
    await escalateApproval(id, reason);
    return id;
  },
);

const approvalSlice = createSlice({
  name: 'approvals',
  initialState,
  reducers: {
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Pending approvals
    builder
      .addCase(loadPendingApprovals.pending, (state) => {
        state.loadingPending = true;
        state.error = null;
      })
      .addCase(loadPendingApprovals.fulfilled, (state, action: PayloadAction<PendingApproval[]>) => {
        state.loadingPending = false;
        state.pending = action.payload;
      })
      .addCase(loadPendingApprovals.rejected, (state, action) => {
        state.loadingPending = false;
        state.error = action.error.message ?? 'Failed to load pending approvals';
      });

    // History
    builder
      .addCase(loadApprovalHistory.pending, (state) => {
        state.loadingHistory = true;
        state.error = null;
      })
      .addCase(loadApprovalHistory.fulfilled, (state, action: PayloadAction<ApprovalRecord[]>) => {
        state.loadingHistory = false;
        state.history = action.payload;
      })
      .addCase(loadApprovalHistory.rejected, (state, action) => {
        state.loadingHistory = false;
        state.error = action.error.message ?? 'Failed to load approval history';
      });

    // Stats
    builder
      .addCase(loadApprovalStats.pending, (state) => {
        state.loadingStats = true;
      })
      .addCase(loadApprovalStats.fulfilled, (state, action: PayloadAction<ApprovalStats>) => {
        state.loadingStats = false;
        state.stats = action.payload;
      })
      .addCase(loadApprovalStats.rejected, (state, action) => {
        state.loadingStats = false;
        state.error = action.error.message ?? 'Failed to load stats';
      });

    // Escalate
    builder
      .addCase(escalateApprovalAsync.pending, (state, action) => {
        state.escalating = action.meta.arg.id;
      })
      .addCase(escalateApprovalAsync.fulfilled, (state, action: PayloadAction<string>) => {
        state.escalating = null;
        state.pending = state.pending.filter((p) => p.id !== action.payload);
      })
      .addCase(escalateApprovalAsync.rejected, (state, action) => {
        state.escalating = null;
        state.error = action.error.message ?? 'Failed to escalate';
      });
  },
});

export const { clearError } = approvalSlice.actions;
export default approvalSlice.reducer;
