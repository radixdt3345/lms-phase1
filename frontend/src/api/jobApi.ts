import api from './authApi';
import type { ApiResponse } from '../types';

export interface JobStatusDto {
  jobName: string;
  cronExpression: string;
  lastRunAt: string | null;
  lastRunStatus: string | null;
  lastRunDurationMs: number | null;
  nextRunAt: string | null;
  isEnabled: boolean;
}

export const fetchJobStatuses = async (): Promise<JobStatusDto[]> => {
  const res = await api.get<ApiResponse<JobStatusDto[]>>('/jobs/status');
  return res.data.data;
};

export const triggerJob = async (jobName: string, reason?: string): Promise<boolean> => {
  const res = await api.post<ApiResponse<boolean>>(`/jobs/${jobName}/trigger`, { reason });
  return res.data.data;
};
