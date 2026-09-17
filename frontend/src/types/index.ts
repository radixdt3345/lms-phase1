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

// Audit Log types
export interface AuditLogDto {
  id: string;
  actorUserId: string;
  actorEmail: string;
  actionType: string;
  recordType: string;
  recordId: string;
  oldValue: string | null;
  newValue: string | null;
  ipAddress: string;
  timestamp: string;
}

export interface AuditLogFilters {
  userId?: string;
  actionType?: string;
  recordType?: string;
  dateFrom?: string;
  dateTo?: string;
  page: number;
  pageSize: number;
}

export interface AuditLogPagedResult {
  items: AuditLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Generic paged result
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Employee types
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Intern';
export type EmployeeStatus = 'Active' | 'Inactive' | 'OnLeave' | 'Terminated';

export interface EmployeeProfileDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  employeeCode: string;
  jobTitle: string;
  departmentId: string;
  departmentName: string;
  dateOfJoining: string;
  employmentType: EmploymentType;
  status: EmployeeStatus;
  userId?: string;
}

export interface CreateEmployeeDto {
  firstName: string;
  lastName: string;
  email: string;
  employeeCode: string;
  jobTitle: string;
  departmentId: string;
  dateOfJoining: string;
  employmentType: EmploymentType;
  status?: EmployeeStatus;
}

export interface UpdateEmployeeDto {
  firstName?: string;
  lastName?: string;
  email?: string;
  jobTitle?: string;
  departmentId?: string;
  dateOfJoining?: string;
  employmentType?: EmploymentType;
  status?: EmployeeStatus;
}

export interface EmployeeLeaveBalanceDto {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  leaveTypeName: string;
  totalAllotted: number;
  used: number;
  remaining: number;
  year: number;
}

export interface EmployeeDocumentDto {
  id: string;
  employeeId: string;
  documentType: string;
  fileName: string;
  uploadedAt: string;
  url: string;
}
