// ApiResponse<T> — mirrors backend
export interface ApiResponse<T> {
  data: T;
}

// Auth types
export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  departmentId?: string;
  status: string;
}
