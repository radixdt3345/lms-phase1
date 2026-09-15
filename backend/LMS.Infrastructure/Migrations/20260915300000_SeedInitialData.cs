using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        // Fixed UUIDs ensure ON CONFLICT DO NOTHING idempotency across repeated runs.
        // Roles
        private const string RoleSuperAdminId  = "00000001-0000-0000-0000-000000000001";
        private const string RoleHrAdminId     = "00000001-0000-0000-0000-000000000002";
        private const string RoleManagerId     = "00000001-0000-0000-0000-000000000003";
        private const string RoleEmployeeId    = "00000001-0000-0000-0000-000000000004";
        private const string RoleDirectorId    = "00000001-0000-0000-0000-000000000005";
        // Department
        private const string DeptHrId          = "00000002-0000-0000-0000-000000000001";
        private const string DeptEngId         = "00000002-0000-0000-0000-000000000002";
        private const string DeptFinId         = "00000002-0000-0000-0000-000000000003";
        private const string DeptOpsId         = "00000002-0000-0000-0000-000000000004";
        // Leave Types
        private const string LeaveTypeCLId     = "00000003-0000-0000-0000-000000000001";
        private const string LeaveTypeSLId     = "00000003-0000-0000-0000-000000000002";
        private const string LeaveTypeELId     = "00000003-0000-0000-0000-000000000003";
        private const string LeaveTypeCOId     = "00000003-0000-0000-0000-000000000004";
        private const string LeaveTypeULId     = "00000003-0000-0000-0000-000000000005";
        // Users
        private const string UserSuperAdminId  = "00000004-0000-0000-0000-000000000001";
        private const string UserHrAdminId     = "00000004-0000-0000-0000-000000000002";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgcrypto for BCrypt password hashing (idempotent — IF NOT EXISTS)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            // ----------------------------------------------------------------
            // 1. Roles — SuperAdmin, HRAdmin, Manager, Employee, Director
            //    Conflict target: roles.name (unique index idx_roles_name)
            // ----------------------------------------------------------------
            migrationBuilder.Sql($@"
INSERT INTO roles (id, name, description, created_at)
VALUES
    ('{RoleSuperAdminId}', 'SuperAdmin', 'Super Administrator — system configuration and user unlock access',            NOW()),
    ('{RoleHrAdminId}',    'HRAdmin',    'HR Administrator — full access to employee, leave, and policy management',     NOW()),
    ('{RoleManagerId}',    'Manager',    'Line Manager — can approve/reject direct report leave requests',               NOW()),
    ('{RoleEmployeeId}',   'Employee',   'Standard Employee — can submit and view own leave requests',                   NOW()),
    ('{RoleDirectorId}',   'Director',   'Director — second-level approver for backdated and escalated leave requests',  NOW())
ON CONFLICT (id) DO NOTHING;
");

            // ----------------------------------------------------------------
            // 2. Departments — HR, Engineering, Finance, Operations
            //    Conflict target: id (primary key, unique)
            //    departments.code also has a unique index; using id is simpler.
            // ----------------------------------------------------------------
            migrationBuilder.Sql($@"
INSERT INTO departments (id, name, code, description, overlap_limit, is_active, created_at, updated_at, deleted_at)
VALUES
    ('{DeptHrId}',  'Human Resources', 'HR',  'HR Administration and People Operations',                   2, TRUE, NOW(), NOW(), NULL),
    ('{DeptEngId}', 'Engineering',     'ENG', 'Software Engineering and Technical Development',            3, TRUE, NOW(), NOW(), NULL),
    ('{DeptFinId}', 'Finance',         'FIN', 'Finance, Accounting and Budgeting',                         2, TRUE, NOW(), NOW(), NULL),
    ('{DeptOpsId}', 'Operations',      'OPS', 'Business Operations and Process Management',                2, TRUE, NOW(), NOW(), NULL)
ON CONFLICT (id) DO NOTHING;
");

            // ----------------------------------------------------------------
            // 3. Leave Types — CL, SL, EL, CO, UL  (FR-91 / AC-28)
            //    Conflict target: id
            // ----------------------------------------------------------------
            migrationBuilder.Sql($@"
INSERT INTO leave_types (id, name, code, description, annual_days, requires_attachment, requires_hr_approval, is_active, created_at, updated_at, deleted_at)
VALUES
    ('{LeaveTypeCLId}', 'Casual Leave',  'CL', 'Casual / personal leave for short-notice personal needs.',                                    12, FALSE, FALSE, TRUE, NOW(), NOW(), NULL),
    ('{LeaveTypeSLId}', 'Sick Leave',    'SL', 'Medical leave for illness or injury. Medical certificate required for 3+ consecutive days.',   6,  TRUE,  TRUE,  TRUE, NOW(), NOW(), NULL),
    ('{LeaveTypeELId}', 'Earned Leave',  'EL', 'Earned / privileged leave accrued through service. 1 day granted per year.',                  1,  FALSE, FALSE, TRUE, NOW(), NOW(), NULL),
    ('{LeaveTypeCOId}', 'Comp-off',      'CO', 'Compensatory off granted for working on a holiday or weekend.',                               0,  FALSE, FALSE, TRUE, NOW(), NOW(), NULL),
    ('{LeaveTypeULId}', 'Unpaid Leave',  'UL', 'Leave without pay, taken when paid leave balance is exhausted.',                             0,  FALSE, FALSE, TRUE, NOW(), NOW(), NULL)
ON CONFLICT (id) DO NOTHING;
");

            // ----------------------------------------------------------------
            // 4. Seed users — Super Admin and HR Admin (FR-91)
            //    Default emails: configurable post-deployment via UPDATE statement.
            //    Default password: Admin@123 (BCrypt cost 10 via pgcrypto).
            //    Conflict target: id
            //    azure_ad_object_id is NOT NULL — use synthetic sentinel values for
            //    seed accounts that authenticate via password, not Azure AD.
            // ----------------------------------------------------------------
            migrationBuilder.Sql($@"
INSERT INTO users (
    id, azure_ad_object_id, email, display_name, password_hash,
    employee_code, department, department_id, manager_azure_ad_object_id,
    is_active, status, failed_login_attempts,
    locked_at, refresh_token, refresh_token_expires_at,
    created_at, updated_at, deleted_at
)
VALUES (
    '{UserSuperAdminId}',
    'seed-superadmin-azure-sentinel',
    'superadmin@company.com',
    'Super Admin',
    crypt('Admin@123', gen_salt('bf', 10)),
    'EMP-SEED-001', 'Human Resources', '{DeptHrId}', NULL,
    TRUE, 'Active', 0,
    NULL, NULL, NULL,
    NOW(), NOW(), NULL
),
(
    '{UserHrAdminId}',
    'seed-hradmin-azure-sentinel',
    'hradmin@company.com',
    'HR Admin',
    crypt('Admin@123', gen_salt('bf', 10)),
    'EMP-SEED-002', 'Human Resources', '{DeptHrId}', NULL,
    TRUE, 'Active', 0,
    NULL, NULL, NULL,
    NOW(), NOW(), NULL
)
ON CONFLICT (id) DO NOTHING;
");

            // ----------------------------------------------------------------
            // 5. User-role assignments
            //    Conflict target: primary key (user_id, role_id)
            // ----------------------------------------------------------------
            migrationBuilder.Sql($@"
INSERT INTO user_roles (user_id, role_id, assigned_at, assigned_by)
VALUES
    ('{UserSuperAdminId}', '{RoleSuperAdminId}', NOW(), 'seed-migration'),
    ('{UserHrAdminId}',    '{RoleHrAdminId}',    NOW(), 'seed-migration')
ON CONFLICT (user_id, role_id) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded user-role assignments
            migrationBuilder.Sql($@"
DELETE FROM user_roles
WHERE (user_id = '{UserSuperAdminId}' AND role_id = '{RoleSuperAdminId}')
   OR (user_id = '{UserHrAdminId}'    AND role_id = '{RoleHrAdminId}');
");

            // Remove seeded users
            migrationBuilder.Sql($@"
DELETE FROM users
WHERE id IN ('{UserSuperAdminId}', '{UserHrAdminId}');
");

            // Remove seeded leave types
            migrationBuilder.Sql($@"
DELETE FROM leave_types
WHERE id IN (
    '{LeaveTypeCLId}', '{LeaveTypeSLId}', '{LeaveTypeELId}',
    '{LeaveTypeCOId}', '{LeaveTypeULId}'
);
");

            // Remove seeded departments
            migrationBuilder.Sql($@"
DELETE FROM departments
WHERE id IN ('{DeptHrId}', '{DeptEngId}', '{DeptFinId}', '{DeptOpsId}');
");

            // Remove seeded roles
            migrationBuilder.Sql($@"
DELETE FROM roles
WHERE id IN (
    '{RoleSuperAdminId}', '{RoleHrAdminId}', '{RoleManagerId}',
    '{RoleEmployeeId}', '{RoleDirectorId}'
);
");
        }
    }
}
