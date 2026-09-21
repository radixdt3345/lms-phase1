import api from './authApi';
import type { ApiResponse } from '../types';

export interface OverviewDashboardDto {
  totalEmployees: number;
  onLeaveToday: number;
  pendingApprovals: number;
  availableCompOffCredits: number;
}

export interface TeamLeaveOverviewDto {
  employeesOnLeave: TeamLeaveEntry[];
  pendingRequests: TeamLeaveEntry[];
  totalOnLeave: number;
  totalPending: number;
}

export interface TeamLeaveEntry {
  employeeId: string;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface ApprovalDashboardSummaryDto {
  pendingCount: number;
  approvedToday: number;
  rejectedToday: number;
  averageTurnaroundHours: number;
  pendingApprovals: ApprovalEntry[];
}

export interface ApprovalEntry {
  id: string;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  submittedAt: string;
}

export interface CompOffSummaryDto {
  totalCreditsAvailable: number;
  expiringThisMonth: number;
  creditsUsedThisMonth: number;
  creditsByEmployee: CompOffEntry[];
}

export interface CompOffEntry {
  employeeId: string;
  employeeName: string;
  creditsAvailable: number;
  expiringCredits: number;
}

/** Fetches overall dashboard overview — HRAdmin/SuperAdmin only. */
export const fetchDashboardOverview = async (): Promise<OverviewDashboardDto> => {
  const response = await api.get<ApiResponse<OverviewDashboardDto>>('/dashboards/overview');
  return response.data.data;
};

/** Fetches team leave overview — Manager/HRAdmin/SuperAdmin. */
export const fetchTeamLeave = async (managerId?: string): Promise<TeamLeaveOverviewDto> => {
  const params = managerId ? { managerId } : {};
  const response = await api.get<ApiResponse<TeamLeaveOverviewDto>>('/dashboards/team-leave', { params });
  return response.data.data;
};

/** Fetches approval queue metrics — Manager/HRAdmin/SuperAdmin. */
export const fetchApprovalSummary = async (): Promise<ApprovalDashboardSummaryDto> => {
  const response = await api.get<ApiResponse<ApprovalDashboardSummaryDto>>('/dashboards/approvals');
  return response.data.data;
};

/** Fetches comp-off summary — HRAdmin/SuperAdmin only. */
export const fetchCompOffSummary = async (): Promise<CompOffSummaryDto> => {
  const response = await api.get<ApiResponse<CompOffSummaryDto>>('/dashboards/comp-off');
  return response.data.data;
};
