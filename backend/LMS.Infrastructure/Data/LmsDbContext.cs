using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Data;

public class LmsDbContext : DbContext
{
    public LmsDbContext(DbContextOptions<LmsDbContext> options) : base(options)
    {
    }

    // Auth / Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Department> Departments => Set<Department>();

    // Leave Policy
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();

    // Notifications (F-09)
    public DbSet<Notification> Notifications => Set<Notification>();

    // Public Holidays / Master Data
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    // Employee Management
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    // Leave Balance Management (F-05)
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<CompOffCredit> CompOffCredits => Set<CompOffCredit>();

    // Leave Application & Workflow (F-06)
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveRequestAttachment> LeaveRequestAttachments => Set<LeaveRequestAttachment>();

    // Comp-Off Management (F-07)
    public DbSet<CompOffRequest> CompOffRequests => Set<CompOffRequest>();

    // Approval Workflow (F-08)
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

    // Background Jobs — application-level tracking (F-15)
    public DbSet<JobLog> JobLogs => Set<JobLog>();

    // Audit Trail (F-13) — append-only, never updated or deleted
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = DateTime.UtcNow;
        var nowOffset = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added)
                    user.CreatedAt = now;
                user.UpdatedAt = now;
            }
            else if (entry.Entity is Department department)
            {
                if (entry.State == EntityState.Added)
                    department.CreatedAt = now;
                department.UpdatedAt = now;
            }
            else if (entry.Entity is Role role && entry.State == EntityState.Added)
            {
                role.CreatedAt = now;
            }
            else if (entry.Entity is UserRole userRole && entry.State == EntityState.Added)
            {
                userRole.AssignedAt = now;
            }
            else if (entry.Entity is LeaveType leaveType)
            {
                if (entry.State == EntityState.Added)
                    leaveType.CreatedAt = now;
                leaveType.UpdatedAt = now;
            }
            else if (entry.Entity is LeavePolicy leavePolicy)
            {
                if (entry.State == EntityState.Added)
                    leavePolicy.CreatedAt = now;
                leavePolicy.UpdatedAt = now;
            }
            else if (entry.Entity is Notification notification)
            {
                if (entry.State == EntityState.Added)
                    notification.CreatedAt = now;
                notification.UpdatedAt = now;
            }
            else if (entry.Entity is PublicHoliday publicHoliday)
            {
                if (entry.State == EntityState.Added)
                    publicHoliday.CreatedAt = now;
                publicHoliday.UpdatedAt = now;
            }
            else if (entry.Entity is SystemConfig systemConfig)
            {
                if (entry.State == EntityState.Added)
                    systemConfig.CreatedAt = nowOffset;
                systemConfig.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is EmployeeProfile employeeProfile)
            {
                if (entry.State == EntityState.Added)
                    employeeProfile.CreatedAt = nowOffset;
                employeeProfile.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is EmployeeLeaveBalance employeeLeaveBalance)
            {
                if (entry.State == EntityState.Added)
                    employeeLeaveBalance.CreatedAt = nowOffset;
                employeeLeaveBalance.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is EmployeeDocument employeeDocument)
            {
                if (entry.State == EntityState.Added)
                    employeeDocument.CreatedAt = nowOffset;
            }
            else if (entry.Entity is LeaveBalance leaveBalance)
            {
                if (entry.State == EntityState.Added)
                    leaveBalance.CreatedAt = nowOffset;
                leaveBalance.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is CompOffCredit compOffCredit)
            {
                if (entry.State == EntityState.Added)
                    compOffCredit.CreatedAt = nowOffset;
                compOffCredit.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is LeaveRequest leaveRequest)
            {
                if (entry.State == EntityState.Added)
                    leaveRequest.CreatedAt = nowOffset;
                leaveRequest.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is LeaveRequestAttachment leaveRequestAttachment && entry.State == EntityState.Added)
            {
                leaveRequestAttachment.CreatedAt = nowOffset;
            }
            else if (entry.Entity is CompOffRequest compOffRequest)
            {
                if (entry.State == EntityState.Added)
                    compOffRequest.CreatedAt = nowOffset;
                compOffRequest.UpdatedAt = nowOffset;
            }
            else if (entry.Entity is ApprovalRecord approvalRecord && entry.State == EntityState.Added)
            {
                approvalRecord.CreatedAt = now;
            }
            else if (entry.Entity is JobLog jobLog && entry.State == EntityState.Added)
            {
                jobLog.CreatedAt = now;
                jobLog.ExecutedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
