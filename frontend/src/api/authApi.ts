import axios from 'axios';
import type { ApiResponse, UserProfile } from '../types';

const api = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '/api' });

// Reads response.data.data (ApiResponse<T> envelope)
export const fetchCurrentUser = async (): Promise<UserProfile> => {
  const response = await api.get<ApiResponse<UserProfile>>('/auth/me');
  return response.data.data;
};

export const fetchAllUsers = async (): Promise<UserProfile[]> => {
  const response = await api.get<ApiResponse<UserProfile[]>>('/auth/users');
  return response.data.data;
};

export default api;
