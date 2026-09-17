import api from './authApi';
import type {
  ApiResponse,
  EmployeeProfileDto,
  CreateEmployeeDto,
  UpdateEmployeeDto,
  PagedResult,
  EmployeeLeaveBalanceDto,
  EmployeeDocumentDto,
} from '../types';

export interface EmployeeQueryParams {
  pageNumber?: number;
  pageSize?: number;
  departmentId?: string;
  search?: string;
}

export const fetchEmployees = async (params: EmployeeQueryParams = {}): Promise<PagedResult<EmployeeProfileDto>> => {
  const res = await api.get<ApiResponse<PagedResult<EmployeeProfileDto>>>('/employees', { params });
  return res.data.data;
};

export const fetchEmployee = async (id: string): Promise<EmployeeProfileDto> => {
  const res = await api.get<ApiResponse<EmployeeProfileDto>>(`/employees/${id}`);
  return res.data.data;
};

export const createEmployee = async (dto: CreateEmployeeDto): Promise<EmployeeProfileDto> => {
  const res = await api.post<ApiResponse<EmployeeProfileDto>>('/employees', dto);
  return res.data.data;
};

export const updateEmployee = async (id: string, dto: UpdateEmployeeDto): Promise<EmployeeProfileDto> => {
  const res = await api.put<ApiResponse<EmployeeProfileDto>>(`/employees/${id}`, dto);
  return res.data.data;
};

export const deleteEmployee = async (id: string): Promise<boolean> => {
  const res = await api.delete<ApiResponse<boolean>>(`/employees/${id}`);
  return res.data.data;
};

export const fetchEmployeeLeaveBalances = async (id: string): Promise<EmployeeLeaveBalanceDto[]> => {
  const res = await api.get<ApiResponse<EmployeeLeaveBalanceDto[]>>(`/employees/${id}/leave-balances`);
  return res.data.data;
};

export const fetchEmployeeDocuments = async (id: string): Promise<EmployeeDocumentDto[]> => {
  const res = await api.get<ApiResponse<EmployeeDocumentDto[]>>(`/employees/${id}/documents`);
  return res.data.data;
};
