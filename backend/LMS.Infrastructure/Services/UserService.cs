using LMS.Application.DTOs.Auth;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(LmsDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfileDto?> GetOrCreateFromAzureAdAsync(
        string azureAdObjectId,
        string email,
        string displayName)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

        if (user is not null)
        {
            _logger.LogInformation("Existing user {UserId} authenticated via Azure AD", user.Id);
            return MapToDto(user);
        }

        // First SSO login — provision the account with default Employee role.
        var employeeRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == RoleNames.Employee);

        user = new User
        {
            Id = Guid.NewGuid(),
            AzureAdObjectId = azureAdObjectId,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            Status = UserStatus.Active,
            FailedLoginAttempts = 0
        };

        _context.Users.Add(user);

        if (employeeRole is not null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = employeeRole.Id,
                AssignedBy = "system"
            });
        }

        await _context.SaveChangesAsync();

        // Re-load with navigation properties for mapping.
        user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id);

        _logger.LogInformation("Provisioned new user {UserId} for Azure AD object {Oid}", user.Id, azureAdObjectId);
        return MapToDto(user);
    }

    public async Task<UserProfileDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAdObjectId);

        return user is null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllActiveUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserProfileDto> AssignRoleAsync(Guid userId, Guid roleId, string assignedBy)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
            throw new KeyNotFoundException($"Role {roleId} not found.");

        var alreadyAssigned = user.UserRoles.Any(ur => ur.RoleId == roleId);
        if (!alreadyAssigned)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = assignedBy
            });
            await _context.SaveChangesAsync();

            // Re-load to pick up the new role's navigation properties.
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == userId);
        }

        _logger.LogInformation("Role {RoleId} assigned to user {UserId} by {AssignedBy}", roleId, userId, assignedBy);
        return MapToDto(user);
    }

    public async Task<UserProfileDto> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId)
            ?? throw new KeyNotFoundException($"User {userId} does not have role {roleId}.");

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == userId);

        _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
        return MapToDto(user);
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static UserProfileDto MapToDto(User user) => new()
    {
        Id = user.Id,
        AzureAdObjectId = user.AzureAdObjectId,
        Email = user.Email,
        DisplayName = user.DisplayName,
        EmployeeCode = user.EmployeeCode,
        Department = user.Department,
        Roles = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role.Name)
            .ToArray(),
        IsActive = user.IsActive,
        Status = user.Status.ToString()
    };
}
