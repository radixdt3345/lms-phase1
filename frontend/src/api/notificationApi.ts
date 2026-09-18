import api from './authApi';
import type { ApiResponse, PagedResult } from '../types';

export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  body: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
}

export type NotificationPageDto = PagedResult<NotificationDto>;

export const fetchNotifications = async (
  page = 1,
  pageSize = 20,
): Promise<NotificationPageDto> => {
  const res = await api.get<ApiResponse<NotificationPageDto>>('/notifications', {
    params: { page, pageSize },
  });
  return res.data.data;
};

export const getUnreadCount = async (): Promise<number> => {
  const res = await api.get<ApiResponse<number>>('/notifications/unread-count');
  return res.data.data;
};

export const markRead = async (id: string): Promise<boolean> => {
  const res = await api.put<ApiResponse<boolean>>(`/notifications/${id}/read`);
  return res.data.data;
};

export const markAllRead = async (): Promise<boolean> => {
  const res = await api.put<ApiResponse<boolean>>('/notifications/read-all');
  return res.data.data;
};

export const deleteNotification = async (id: string): Promise<boolean> => {
  const res = await api.delete<ApiResponse<boolean>>(`/notifications/${id}`);
  return res.data.data;
};
