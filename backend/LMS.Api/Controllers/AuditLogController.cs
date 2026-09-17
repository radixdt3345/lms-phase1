using LMS.Application.DTOs.AuditLog;
using LMS.Application.Interfaces;
using LMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Read-only audit trail API — F-13.
/// Only HRAdmin and SuperAdmin may query audit logs.
/// All responses are wrapped in ApiResponse&lt;T&gt;.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "HRAdminOrSuperAdmin")]
[Produces("application/json")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Returns a paginated, filtered list of audit log entries (most-recent first).
    /// Query params: userId, actionType, recordType, dateFrom, dateTo, page, pageSize.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AuditLogPagedResultDto>>> GetPaged(
        [FromQuery] AuditLogQueryParams query)
    {
        _logger.LogInformation(
            "AuditLog query: page={Page} pageSize={PageSize} userId={UserId} actionType={ActionType} recordType={RecordType}",
            query.Page, query.PageSize, query.UserId, query.ActionType, query.RecordType);

        var result = await _auditLogService.GetPagedAsync(query);
        return Ok(ApiResponse<AuditLogPagedResultDto>.Ok(result));
    }
}
