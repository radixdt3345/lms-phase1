import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { fetchJobStatuses, triggerJob, JobStatusDto } from '../api/jobApi';

interface JobState {
  jobs: JobStatusDto[];
  loading: boolean;
  error: string | null;
  triggeringJob: string | null;
}

const initialState: JobState = {
  jobs: [],
  loading: false,
  error: null,
  triggeringJob: null,
};

export const loadJobStatuses = createAsyncThunk(
  'jobs/loadStatuses',
  async () => {
    return fetchJobStatuses();
  },
);

export const triggerJobAsync = createAsyncThunk(
  'jobs/trigger',
  async ({ jobName, reason }: { jobName: string; reason?: string }) => {
    await triggerJob(jobName, reason);
    return jobName;
  },
);

const jobSlice = createSlice({
  name: 'jobs',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(loadJobStatuses.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadJobStatuses.fulfilled, (state, action) => {
        state.loading = false;
        state.jobs = action.payload;
      })
      .addCase(loadJobStatuses.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to load job statuses';
      })
      .addCase(triggerJobAsync.pending, (state, action) => {
        state.triggeringJob = action.meta.arg.jobName;
      })
      .addCase(triggerJobAsync.fulfilled, (state) => {
        state.triggeringJob = null;
      })
      .addCase(triggerJobAsync.rejected, (state) => {
        state.triggeringJob = null;
      });
  },
});

export default jobSlice.reducer;
