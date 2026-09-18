import api from './authApi';
import type { ApiResponse } from '../types';

export interface LeaveBalanceDto {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeCode: string;
  year: number;
  totalDays: number;
  usedDays: number;
  pendingDays: number;
  adjustedDays: number;
  availableDays: number;
  createdAt: string;
  updatedAt: string;
}

export interface AdjustBalanceDto {
  employeeId: string;
  leaveTypeId: string;
  year: number;
  adjustment: number;
  reason: string;
}

/** Returns the current employee's leave balances (GET /api/leave-balances/my). */
export const fetchMyLeaveBalances = async (): Promise<LeaveBalanceDto[]> => {
  const response = await api.get<ApiResponse<LeaveBalanceDto[]>>('/leave-balances/my');
  return response.data.data;
};

/** Returns all leave balances — HRAdmin only (GET /api/leave-balances). */
export const fetchAllLeaveBalances = async (): Promise<LeaveBalanceDto[]> => {
  const response = await api.get<ApiResponse<LeaveBalanceDto[]>>('/leave-balances');
  return response.data.data;
};

/** Returns leave balances for a specific employee — Manager/HRAdmin only. */
export const fetchLeaveBalancesByEmployee = async (employeeId: string): Promise<LeaveBalanceDto[]> => {
  const response = await api.get<ApiResponse<LeaveBalanceDto[]>>(`/leave-balances/employee/${employeeId}`);
  return response.data.data;
};

/** Applies a manual adjustment to an employee's leave balance — HRAdmin only. */
export const adjustLeaveBalance = async (dto: AdjustBalanceDto): Promise<LeaveBalanceDto> => {
  const response = await api.post<ApiResponse<LeaveBalanceDto>>('/leave-balances/adjust', dto);
  return response.data.data;
};
