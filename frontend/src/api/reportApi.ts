import api from './authApi';
import axios from 'axios';
import type { ApiResponse } from '../types';

export type ReportType = 'LeaveSummary' | 'CompOffSummary' | 'ApprovalHistory';
export type ReportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export interface ReportJobDto {
  id: string;
  reportType: ReportType;
  status: ReportStatus;
  requestedByUserId: string;
  requestedAt: string;
  completedAt?: string;
  filterJson?: string;
  filePath?: string;
}

export interface CreateReportRequest {
  reportType: ReportType;
  startDate?: string;
  endDate?: string;
  departmentId?: string;
}

/** Fetch all report jobs submitted by the current user. */
export const fetchReportJobs = async (): Promise<ReportJobDto[]> => {
  const response = await api.get<ApiResponse<ReportJobDto[]>>('/reports');
  return response.data.data;
};

/** Request a new report job. */
export const requestReport = async (
  reportType: ReportType,
  startDate?: string,
  endDate?: string,
  departmentId?: string,
): Promise<ReportJobDto> => {
  const body: CreateReportRequest = { reportType, startDate, endDate, departmentId };
  const response = await api.post<ApiResponse<ReportJobDto>>('/reports/request', body);
  return response.data.data;
};

/** Download a completed report as a CSV blob. */
export const downloadReport = async (id: string): Promise<Blob> => {
  const response = await api.get(`/reports/${id}/download`, { responseType: 'blob' });
  return response.data as Blob;
};

/** Fetch a specific report job by ID. */
export const fetchReportJob = async (id: string): Promise<ReportJobDto> => {
  const response = await api.get<ApiResponse<ReportJobDto>>(`/reports/${id}`);
  return response.data.data;
};
