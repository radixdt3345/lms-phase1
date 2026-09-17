import api from './authApi';
import type { ApiResponse, AuditLogFilters, AuditLogPagedResult } from '../types';

export const fetchAuditLogs = async (filters: AuditLogFilters): Promise<AuditLogPagedResult> => {
  const params: Record<string, string | number> = {
    page: filters.page,
    pageSize: filters.pageSize,
  };
  if (filters.userId) params.userId = filters.userId;
  if (filters.actionType) params.actionType = filters.actionType;
  if (filters.recordType) params.recordType = filters.recordType;
  if (filters.dateFrom) params.dateFrom = filters.dateFrom;
  if (filters.dateTo) params.dateTo = filters.dateTo;

  const res = await api.get<ApiResponse<AuditLogPagedResult>>('/audit-log', { params });
  return res.data.data;
};
