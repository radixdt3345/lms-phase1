import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as auditLogApi from '../api/auditLogApi';
import type { AuditLogDto, AuditLogFilters } from '../types';

interface AuditLogState {
  items: AuditLogDto[];
  totalCount: number;
  loading: boolean;
  error: string | null;
  filters: AuditLogFilters;
}

const initialState: AuditLogState = {
  items: [],
  totalCount: 0,
  loading: false,
  error: null,
  filters: { page: 1, pageSize: 50 },
};

export const fetchAuditLogsThunk = createAsyncThunk(
  'auditLog/fetchAll',
  (filters: AuditLogFilters) => auditLogApi.fetchAuditLogs(filters),
);

const auditLogSlice = createSlice({
  name: 'auditLog',
  initialState,
  reducers: {
    setFilters(state, action: { payload: Partial<AuditLogFilters> }) {
      state.filters = { ...state.filters, ...action.payload, page: 1 };
    },
    setPage(state, action: { payload: number }) {
      state.filters = { ...state.filters, page: action.payload };
    },
    clearFilters(state) {
      state.filters = { page: 1, pageSize: 50 };
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAuditLogsThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchAuditLogsThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.items = action.payload.items;
        state.totalCount = action.payload.totalCount;
      })
      .addCase(fetchAuditLogsThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch audit logs';
      });
  },
});

export const { setFilters, setPage, clearFilters } = auditLogSlice.actions;
export default auditLogSlice.reducer;
