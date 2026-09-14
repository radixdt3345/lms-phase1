using LMS.Application.DTOs.Auth;

namespace LMS.Application.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Returns the existing user record for the given Azure AD object ID, or creates one on first SSO login.
    /// </summary>
    Task<UserProfileDto?> GetOrCreateFromAzureAdAsync(string azureAdObjectId, string email, string displayName);

    /// <summary>
    /// Looks up a user by Azure AD object ID. Returns null when not found.
    /// </summary>
    Task<UserProfileDto?> GetByAzureAdObjectIdAsync(string azureAdObjectId);

    /// <summary>
    /// Returns all users whose IsActive flag is true.
    /// </summary>
    Task<IEnumerable<UserProfileDto>> GetAllActiveUsersAsync();

    /// <summary>
    /// Assigns a role to a user and returns the updated profile.
    /// </summary>
    Task<UserProfileDto> AssignRoleAsync(Guid userId, Guid roleId, string assignedBy);

    /// <summary>
    /// Removes a role from a user and returns the updated profile.
    /// </summary>
    Task<UserProfileDto> RemoveRoleAsync(Guid userId, Guid roleId);

    /// <summary>
    /// Returns all available system roles.
    /// </summary>
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
}
