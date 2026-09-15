using LMS.Application.DTOs.Employee;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Employee Management API — F-02 API layer.
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize]
[Produces("application/json")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Returns a paged list of all active employee profiles.
    /// Optionally filtered by departmentId.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeProfileDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await _employeeService.GetAllAsync(page, pageSize, departmentId);
        return Ok(ApiResponse<PagedResult<EmployeeProfileDto>>.Ok(result));
    }

    /// <summary>
    /// Returns a single employee profile by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> GetById(Guid id)
    {
        var profile = await _employeeService.GetByIdAsync(id);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"No active employee profile with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<EmployeeProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Returns an employee profile by their User ID.
    /// </summary>
    [HttpGet("by-user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> GetByUserId(Guid userId)
    {
        var profile = await _employeeService.GetByUserIdAsync(userId);

        if (profile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"No active employee profile for user '{userId}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<EmployeeProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Creates a new employee profile. Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> Create([FromBody] CreateEmployeeProfileDto dto)
    {
        try
        {
            var profile = await _employeeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = profile.Id },
                ApiResponse<EmployeeProfileDto>.Ok(profile));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateEmployee — code conflict");
            return Conflict(new ProblemDetails
            {
                Title = "Code conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "EMPLOYEE_CODE_CONFLICT" }
            });
        }
    }

    /// <summary>
    /// Updates an existing employee profile. Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> Update(Guid id, [FromBody] UpdateEmployeeProfileDto dto)
    {
        try
        {
            var profile = await _employeeService.UpdateAsync(id, dto);
            return Ok(ApiResponse<EmployeeProfileDto>.Ok(profile));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "UpdateEmployee — not found");
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "UpdateEmployee — code conflict");
            return Conflict(new ProblemDetails
            {
                Title = "Code conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "EMPLOYEE_CODE_CONFLICT" }
            });
        }
    }

    /// <summary>
    /// Soft-deletes an employee profile (sets DeletedAt). Restricted to HRAdmin and SuperAdmin.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRAdmin,SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var deleted = await _employeeService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = $"No active employee profile with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Returns all leave balances for an employee profile.
    /// </summary>
    [HttpGet("{id:guid}/leave-balances")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeLeaveBalanceDto>>>> GetLeaveBalances(Guid id)
    {
        try
        {
            var balances = await _employeeService.GetLeaveBalancesAsync(id);
            return Ok(ApiResponse<IEnumerable<EmployeeLeaveBalanceDto>>.Ok(balances));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "GetLeaveBalances — employee not found");
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }
    }

    /// <summary>
    /// Returns all documents for an employee profile.
    /// </summary>
    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeDocumentDto>>>> GetDocuments(Guid id)
    {
        try
        {
            var documents = await _employeeService.GetDocumentsAsync(id);
            return Ok(ApiResponse<IEnumerable<EmployeeDocumentDto>>.Ok(documents));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "GetDocuments — employee not found");
            return NotFound(new ProblemDetails
            {
                Title = "Employee not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "EMPLOYEE_NOT_FOUND" }
            });
        }
    }
}
