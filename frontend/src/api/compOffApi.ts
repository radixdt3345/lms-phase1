import api from './authApi';
import type { ApiResponse } from '../types';

export type CompOffStatus = 'Pending' | 'Approved' | 'Rejected';

export interface CompOffRequestDto {
  id: string;
  employeeId: string;
  employeeName: string;
  workedDate: string;
  hoursWorked: number;
  creditDays: number;
  description: string;
  status: CompOffStatus;
  rejectionReason?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCompOffRequestDto {
  workedDate: string;
  hoursWorked: number;
  description: string;
}

export interface RejectCompOffRequestDto {
  rejectionReason: string;
}

export interface CompOffCreditDto {
  employeeId: string;
  totalCredits: number;
  usedCredits: number;
  availableCredits: number;
  expiringCredits: number;
  expiryDate?: string;
}

export const fetchCompOffRequests = async (): Promise<CompOffRequestDto[]> => {
  const res = await api.get<ApiResponse<CompOffRequestDto[]>>('/comp-off-requests');
  return res.data.data;
};

export const createCompOffRequest = async (dto: CreateCompOffRequestDto): Promise<CompOffRequestDto> => {
  const res = await api.post<ApiResponse<CompOffRequestDto>>('/comp-off-requests', dto);
  return res.data.data;
};

export const approveCompOffRequest = async (id: string): Promise<CompOffRequestDto> => {
  const res = await api.post<ApiResponse<CompOffRequestDto>>(`/comp-off-requests/${id}/approve`);
  return res.data.data;
};

export const rejectCompOffRequest = async (id: string, rejectionReason: string): Promise<CompOffRequestDto> => {
  const res = await api.post<ApiResponse<CompOffRequestDto>>(`/comp-off-requests/${id}/reject`, { rejectionReason });
  return res.data.data;
};

export const fetchCompOffCredits = async (): Promise<CompOffCreditDto> => {
  const res = await api.get<ApiResponse<CompOffCreditDto>>('/comp-off-credits');
  return res.data.data;
};
