import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import {
  fetchReportJobs,
  requestReport,
} from '../api/reportApi';
import type { ReportJobDto, ReportType } from '../api/reportApi';

interface ReportState {
  reportJobs: ReportJobDto[];
  generating: boolean;
  loading: boolean;
  error: string | null;
}

const initialState: ReportState = {
  reportJobs: [],
  generating: false,
  loading: false,
  error: null,
};

export const loadReportJobs = createAsyncThunk('reports/loadJobs', async () =>
  fetchReportJobs(),
);

export const generateReport = createAsyncThunk(
  'reports/generate',
  async ({
    reportType,
    startDate,
    endDate,
    departmentId,
  }: {
    reportType: ReportType;
    startDate?: string;
    endDate?: string;
    departmentId?: string;
  }) => requestReport(reportType, startDate, endDate, departmentId),
);

const reportSlice = createSlice({
  name: 'reports',
  initialState,
  reducers: {
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Load jobs
    builder
      .addCase(loadReportJobs.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadReportJobs.fulfilled, (state, action: PayloadAction<ReportJobDto[]>) => {
        state.loading = false;
        state.reportJobs = action.payload;
      })
      .addCase(loadReportJobs.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to load reports';
      });

    // Generate report
    builder
      .addCase(generateReport.pending, (state) => {
        state.generating = true;
        state.error = null;
      })
      .addCase(generateReport.fulfilled, (state, action: PayloadAction<ReportJobDto>) => {
        state.generating = false;
        state.reportJobs = [action.payload, ...state.reportJobs];
      })
      .addCase(generateReport.rejected, (state, action) => {
        state.generating = false;
        state.error = action.error.message ?? 'Failed to generate report';
      });
  },
});

export const { clearError } = reportSlice.actions;
export default reportSlice.reducer;
