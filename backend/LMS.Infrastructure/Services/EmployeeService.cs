using LMS.Application.DTOs.Employee;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Implements employee profile CRUD with soft-delete semantics for F-02 API layer.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly LmsDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(LmsDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<PagedResult<EmployeeProfileDto>> GetAllAsync(int page, int pageSize, Guid? departmentId = null)
    {
        var query = _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Where(e => e.DeletedAt == null);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<EmployeeProfileDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeProfileDto?> GetByIdAsync(Guid id)
    {
        var profile = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<EmployeeProfileDto?> GetByUserIdAsync(Guid userId)
    {
        var profile = await _context.EmployeeProfiles
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.DeletedAt == null);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<IEnumerable<EmployeeLeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId)
    {
        var profile = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null);

        if (profile is null)
            throw new KeyNotFoundException($"Employee profile with ID '{employeeId}' was not found.");

        var balances = await _context.EmployeeLeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == profile.UserId)
            .OrderBy(b => b.Year)
            .ThenBy(b => b.LeaveType.Name)
            .ToListAsync();

        return balances.Select(b => new EmployeeLeaveBalanceDto
        {
            Id = b.Id,
            UserId = b.UserId,
            LeaveTypeId = b.LeaveTypeId,
            LeaveTypeName = b.LeaveType?.Name,
            Year = b.Year,
            TotalAllocated = b.TotalAllocated,
            Used = b.Used,
            Carried = b.Carried,
            Available = b.Available
        });
    }

    public async Task<IEnumerable<EmployeeDocumentDto>> GetDocumentsAsync(Guid employeeId)
    {
        var profile = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null);

        if (profile is null)
            throw new KeyNotFoundException($"Employee profile with ID '{employeeId}' was not found.");

        var documents = await _context.EmployeeDocuments
            .Where(d => d.UserId == profile.UserId && d.DeletedAt == null)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(d => new EmployeeDocumentDto
        {
            Id = d.Id,
            UserId = d.UserId,
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            StoragePath = d.StoragePath,
            FileSizeBytes = d.FileSizeBytes,
            ContentType = d.ContentType,
            UploadedAt = d.UploadedAt,
            UploadedByUserId = d.UploadedByUserId
        });
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task<EmployeeProfileDto> CreateAsync(CreateEmployeeProfileDto dto)
    {
        var codeExists = await _context.EmployeeProfiles
            .AnyAsync(e => e.EmployeeCode == dto.EmployeeCode && e.DeletedAt == null);

        if (codeExists)
            throw new InvalidOperationException($"An employee with code '{dto.EmployeeCode}' already exists.");

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            EmployeeCode = dto.EmployeeCode,
            JobTitle = dto.JobTitle,
            JoiningDate = dto.JoiningDate,
            TerminationDate = dto.TerminationDate,
            EmploymentType = dto.EmploymentType,
            ManagerUserId = dto.ManagerUserId,
            DepartmentId = dto.DepartmentId
        };

        _context.EmployeeProfiles.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created employee profile {ProfileId} with code {Code}", profile.Id, profile.EmployeeCode);

        // Reload with navigation properties for the response DTO
        return await GetByIdAsync(profile.Id) ?? MapToDto(profile);
    }

    public async Task<EmployeeProfileDto> UpdateAsync(Guid id, UpdateEmployeeProfileDto dto)
    {
        var profile = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (profile is null)
            throw new KeyNotFoundException($"Employee profile with ID '{id}' was not found.");

        if (dto.EmployeeCode is not null && dto.EmployeeCode != profile.EmployeeCode)
        {
            var codeExists = await _context.EmployeeProfiles
                .AnyAsync(e => e.EmployeeCode == dto.EmployeeCode && e.Id != id && e.DeletedAt == null);

            if (codeExists)
                throw new InvalidOperationException($"An employee with code '{dto.EmployeeCode}' already exists.");

            profile.EmployeeCode = dto.EmployeeCode;
        }

        if (dto.JobTitle is not null)
            profile.JobTitle = dto.JobTitle;

        if (dto.JoiningDate.HasValue)
            profile.JoiningDate = dto.JoiningDate.Value;

        if (dto.TerminationDate.HasValue)
            profile.TerminationDate = dto.TerminationDate.Value;

        if (dto.EmploymentType is not null)
            profile.EmploymentType = dto.EmploymentType;

        if (dto.ManagerUserId.HasValue)
            profile.ManagerUserId = dto.ManagerUserId.Value;

        if (dto.DepartmentId.HasValue)
            profile.DepartmentId = dto.DepartmentId.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated employee profile {ProfileId}", profile.Id);

        return await GetByIdAsync(id) ?? MapToDto(profile);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var profile = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);

        if (profile is null)
            return false;

        profile.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted employee profile {ProfileId}", profile.Id);
        return true;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static EmployeeProfileDto MapToDto(EmployeeProfile e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        EmployeeCode = e.EmployeeCode,
        JobTitle = e.JobTitle,
        JoiningDate = e.JoiningDate,
        TerminationDate = e.TerminationDate,
        EmploymentType = e.EmploymentType,
        ManagerUserId = e.ManagerUserId,
        ManagerName = e.Manager?.DisplayName,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.Name,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
