import axios from 'axios';
import type { ApiResponse } from '../types';

export interface ApprovalRecord {
  id: string;
  leaveRequestId?: string;
  compOffRequestId?: string;
  approverId: string;
  level: 'L1' | 'L2';
  action: 'APPROVED' | 'REJECTED';
  actedAt: string;
  comments?: string;
}

export interface PendingApproval {
  id: string;
  requestType: 'Leave' | 'CompOff';
  employeeName: string;
  requestedDays?: number;
  startDate?: string;
  endDate?: string;
  reason?: string;
  level: 'L1' | 'L2';
  submittedAt: string;
}

export const fetchPendingApprovals = async (): Promise<PendingApproval[]> => {
  const res = await axios.get<ApiResponse<PendingApproval[]>>('/api/approvals/pending');
  return res.data.data;
};

export const fetchApprovalHistory = async (): Promise<ApprovalRecord[]> => {
  const res = await axios.get<ApiResponse<ApprovalRecord[]>>('/api/approvals/history');
  return res.data.data;
};

export const escalateApproval = async (id: string, reason: string): Promise<void> => {
  await axios.post(`/api/approvals/${id}/escalate`, { reason });
};

export const fetchApprovalStats = async (): Promise<{ pending: number; approved: number; rejected: number }> => {
  const res = await axios.get<ApiResponse<{ pending: number; approved: number; rejected: number }>>('/api/approvals/stats');
  return res.data.data;
};
