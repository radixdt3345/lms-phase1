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

export interface DepartmentDto {
  id: string;
  name: string;
  code: string;
  overlapLimit: number;
  isActive: boolean;
  createdAt: string;
}
export interface CreateDepartmentDto { name: string; code: string; overlapLimit?: number; }
export interface UpdateDepartmentDto { name?: string; code?: string; overlapLimit?: number; isActive?: boolean; }
