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

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    // Public Holidays / Master Data
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

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
                var nowOffset = DateTimeOffset.UtcNow;
                if (entry.State == EntityState.Added)
                    systemConfig.CreatedAt = nowOffset;
                systemConfig.UpdatedAt = nowOffset;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
