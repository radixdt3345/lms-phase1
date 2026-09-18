import api from './authApi';
import type { ApiResponse } from '../types';

export interface PublicHolidayDto {
  id: string;
  name: string;
  date: string;       // ISO 8601 "YYYY-MM-DD"
  year: number;
  description?: string;
  countryCode: string; // ISO 3166-1 alpha-2, e.g. "IN"
  isOptional: boolean; // true = optional/restricted holiday; false = mandatory gazetted
  isActive: boolean;
}

export interface CreatePublicHolidayDto {
  name: string;
  date: string;        // "YYYY-MM-DD"
  description?: string;
  countryCode?: string;
  isOptional?: boolean;
}

export type UpdatePublicHolidayDto = Partial<CreatePublicHolidayDto> & {
  isActive?: boolean;
};

export const fetchPublicHolidays = async (year: number): Promise<PublicHolidayDto[]> => {
  const res = await api.get<ApiResponse<PublicHolidayDto[]>>(`/public-holidays?year=${year}`);
  return res.data.data;
};

export const fetchPublicHolidayById = async (id: string): Promise<PublicHolidayDto> => {
  const res = await api.get<ApiResponse<PublicHolidayDto>>(`/public-holidays/${id}`);
  return res.data.data;
};

export const createPublicHoliday = async (dto: CreatePublicHolidayDto): Promise<PublicHolidayDto> => {
  const res = await api.post<ApiResponse<PublicHolidayDto>>('/public-holidays', dto);
  return res.data.data;
};

export const updatePublicHoliday = async (id: string, dto: UpdatePublicHolidayDto): Promise<PublicHolidayDto> => {
  const res = await api.put<ApiResponse<PublicHolidayDto>>(`/public-holidays/${id}`, dto);
  return res.data.data;
};

export const deletePublicHoliday = async (id: string): Promise<void> => {
  await api.delete(`/public-holidays/${id}`);
};

export const bulkCreatePublicHolidays = async (dtos: CreatePublicHolidayDto[]): Promise<PublicHolidayDto[]> => {
  const res = await api.post<ApiResponse<PublicHolidayDto[]>>('/public-holidays/bulk', dtos);
  return res.data.data;
};
