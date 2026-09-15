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

export interface LeaveTypeDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  annualDays: number;
  requiresAttachment: boolean;
  requiresHrApproval: boolean;
  isActive: boolean;
}
export interface LeavePolicyDto {
  id: string;
  leaveTypeId: string;
  leaveTypeName: string;
  annualAllotment: number;
  maxCarryForward: number;
  maxConsecutiveDays: number;
  minNoticeDays: number;
  accruedMonthly: boolean;
  accrualRate: number;
  effectiveFrom: string;
  effectiveTo?: string;
}
export interface CreateLeaveTypeDto {
  name: string; code: string; annualDays?: number;
  requiresAttachment?: boolean; requiresHrApproval?: boolean; description?: string;
}
export interface CreateLeavePolicyDto {
  leaveTypeId: string; annualAllotment: number; maxCarryForward?: number;
  maxConsecutiveDays?: number; minNoticeDays?: number;
  accruedMonthly?: boolean; accrualRate?: number; effectiveFrom: string; effectiveTo?: string;
}
