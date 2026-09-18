import api from './authApi';
import type { ApiResponse } from '../types';

export type LeaveRequestStatus = 'Draft' | 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveRequestDto {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: LeaveRequestStatus;
  rejectionReason?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeaveRequestDto {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
  saveAsDraft?: boolean;
}

export interface RejectLeaveRequestDto {
  reason: string;
}

export const fetchLeaveRequests = async (): Promise<LeaveRequestDto[]> => {
  const res = await api.get<ApiResponse<LeaveRequestDto[]>>('/leave-requests');
  return res.data.data;
};

export const createLeaveRequest = async (dto: CreateLeaveRequestDto): Promise<LeaveRequestDto> => {
  const res = await api.post<ApiResponse<LeaveRequestDto>>('/leave-requests', dto);
  return res.data.data;
};

export const approveLeaveRequest = async (id: string): Promise<LeaveRequestDto> => {
  const res = await api.post<ApiResponse<LeaveRequestDto>>(`/leave-requests/${id}/approve`);
  return res.data.data;
};

export const rejectLeaveRequest = async (id: string, reason: string): Promise<LeaveRequestDto> => {
  const res = await api.post<ApiResponse<LeaveRequestDto>>(`/leave-requests/${id}/reject`, { reason });
  return res.data.data;
};

export const cancelLeaveRequest = async (id: string): Promise<LeaveRequestDto> => {
  const res = await api.put<ApiResponse<LeaveRequestDto>>(`/leave-requests/${id}/cancel`);
  return res.data.data;
};
