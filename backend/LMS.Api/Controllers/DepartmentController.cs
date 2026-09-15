using LMS.Application.DTOs.Department;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Department CRUD — F-03 API layer (FR-19 to FR-26).
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
[Produces("application/json")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentService departmentService, ILogger<DepartmentController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all non-deleted departments ordered by name.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll()
    {
        var departments = await _departmentService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(departments));
    }

    /// <summary>
    /// Returns a single department by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(Guid id)
    {
        var department = await _departmentService.GetByIdAsync(id);

        if (department is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Department not found",
                Detail = $"No active department with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "DEPARTMENT_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<DepartmentDto>.Ok(department));
    }

    /// <summary>
    /// Creates a new department. Restricted to HRAdmin and Director.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HRAdmin,Director")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] CreateDepartmentDto dto)
    {
        try
        {
            var department = await _departmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = department.Id },
                ApiResponse<DepartmentDto>.Ok(department));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateDepartment — code conflict");
            return Conflict(new ProblemDetails
            {
                Title = "Code conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "DEPARTMENT_CODE_CONFLICT" }
            });
        }
    }

    /// <summary>
    /// Updates an existing department. Restricted to HRAdmin and Director.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HRAdmin,Director")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        try
        {
            var department = await _departmentService.UpdateAsync(id, dto);
            return Ok(ApiResponse<DepartmentDto>.Ok(department));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "UpdateDepartment — not found");
            return NotFound(new ProblemDetails
            {
                Title = "Department not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "DEPARTMENT_NOT_FOUND" }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "UpdateDepartment — code conflict");
            return Conflict(new ProblemDetails
            {
                Title = "Code conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = { ["error_code"] = "DEPARTMENT_CODE_CONFLICT" }
            });
        }
    }

    /// <summary>
    /// Soft-deletes a department (sets DeletedAt). Restricted to HRAdmin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var deleted = await _departmentService.SoftDeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Department not found",
                Detail = $"No active department with ID '{id}' exists.",
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["error_code"] = "DEPARTMENT_NOT_FOUND" }
            });
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
