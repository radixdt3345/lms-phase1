using LMS.Application.DTOs.Auth;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user.
    /// On first call, provisions the user record from Azure AD claims.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetCurrentUser()
    {
        var oid = User.FindFirst("oid")?.Value
               ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        if (string.IsNullOrEmpty(oid))
        {
            _logger.LogWarning("GET /api/auth/me — Azure AD object ID claim missing from token");
            return Unauthorized(new ProblemDetails
            {
                Title = "Missing identity claim",
                Detail = "The JWT does not contain an Azure AD object identifier.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["error_code"] = "MISSING_OID_CLAIM" }
            });
        }

        var email = User.FindFirst("preferred_username")?.Value
                 ?? User.FindFirst("email")?.Value
                 ?? string.Empty;

        var displayName = User.FindFirst("name")?.Value ?? string.Empty;

        var profile = await _userService.GetOrCreateFromAzureAdAsync(oid, email, displayName);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "USER_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Returns all active users. Restricted to HR Admin and Super Admin.
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserProfileDto>>>> GetAllUsers()
    {
        var users = await _userService.GetAllActiveUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserProfileDto>>.Ok(users));
    }

    /// <summary>
    /// Assigns a role to a user. Restricted to HR Admin and Super Admin.
    /// </summary>
    [HttpPost("users/{userId:guid}/roles")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> AssignRole(
        Guid userId,
        [FromBody] UserRoleAssignmentDto dto)
    {
        var assignedBy = User.FindFirst("preferred_username")?.Value ?? "admin";

        try
        {
            var updated = await _userService.AssignRoleAsync(userId, dto.RoleId, assignedBy);
            return Ok(ApiResponse<UserProfileDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "AssignRole — entity not found");
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "NOT_FOUND" }
            });
        }
    }

    /// <summary>
    /// Removes a role from a user. Restricted to HR Admin and Super Admin.
    /// </summary>
    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> RemoveRole(Guid userId, Guid roleId)
    {
        try
        {
            var updated = await _userService.RemoveRoleAsync(userId, roleId);
            return Ok(ApiResponse<UserProfileDto>.Ok(updated));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "RemoveRole — entity not found");
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "NOT_FOUND" }
            });
        }
    }

    /// <summary>
    /// Returns all available system roles.
    /// </summary>
    [HttpGet("roles")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles()
    {
        var roles = await _userService.GetAllRolesAsync();
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
    }
}
