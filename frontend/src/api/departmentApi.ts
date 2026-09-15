import api from './authApi';
import type { ApiResponse, DepartmentDto, CreateDepartmentDto, UpdateDepartmentDto } from '../types';

export const fetchDepartments = async (): Promise<DepartmentDto[]> => {
  const res = await api.get<ApiResponse<DepartmentDto[]>>('/departments');
  return res.data.data;
};

export const createDepartment = async (dto: CreateDepartmentDto): Promise<DepartmentDto> => {
  const res = await api.post<ApiResponse<DepartmentDto>>('/departments', dto);
  return res.data.data;
};

export const updateDepartment = async (id: string, dto: UpdateDepartmentDto): Promise<DepartmentDto> => {
  const res = await api.put<ApiResponse<DepartmentDto>>(`/departments/${id}`, dto);
  return res.data.data;
};

export const deleteDepartment = async (id: string): Promise<boolean> => {
  const res = await api.delete<ApiResponse<boolean>>(`/departments/${id}`);
  return res.data.data;
};
