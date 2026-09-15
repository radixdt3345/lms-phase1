import api from './authApi';
import type { ApiResponse, LeaveTypeDto, LeavePolicyDto, CreateLeaveTypeDto, CreateLeavePolicyDto } from '../types';

export const fetchLeaveTypes = async (): Promise<LeaveTypeDto[]> => {
  const res = await api.get<ApiResponse<LeaveTypeDto[]>>('/leave-types');
  return res.data.data;
};

export const fetchLeaveType = async (id: string): Promise<LeaveTypeDto> => {
  const res = await api.get<ApiResponse<LeaveTypeDto>>(`/leave-types/${id}`);
  return res.data.data;
};

export const createLeaveType = async (dto: CreateLeaveTypeDto): Promise<LeaveTypeDto> => {
  const res = await api.post<ApiResponse<LeaveTypeDto>>('/leave-types', dto);
  return res.data.data;
};

export const updateLeaveType = async (id: string, dto: Partial<CreateLeaveTypeDto>): Promise<LeaveTypeDto> => {
  const res = await api.put<ApiResponse<LeaveTypeDto>>(`/leave-types/${id}`, dto);
  return res.data.data;
};

export const fetchPoliciesForLeaveType = async (leaveTypeId: string): Promise<LeavePolicyDto[]> => {
  const res = await api.get<ApiResponse<LeavePolicyDto[]>>(`/leave-types/${leaveTypeId}/policies`);
  return res.data.data;
};

export const fetchActivePolicyForLeaveType = async (leaveTypeId: string): Promise<LeavePolicyDto> => {
  const res = await api.get<ApiResponse<LeavePolicyDto>>(`/leave-types/${leaveTypeId}/policies/active`);
  return res.data.data;
};

export const createLeavePolicy = async (dto: CreateLeavePolicyDto): Promise<LeavePolicyDto> => {
  const res = await api.post<ApiResponse<LeavePolicyDto>>('/leave-policies', dto);
  return res.data.data;
};

export const updateLeavePolicy = async (id: string, dto: Partial<CreateLeavePolicyDto>): Promise<LeavePolicyDto> => {
  const res = await api.put<ApiResponse<LeavePolicyDto>>(`/leave-policies/${id}`, dto);
  return res.data.data;
};
