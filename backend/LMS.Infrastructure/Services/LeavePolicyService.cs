using LMS.Application.DTOs.LeavePolicy;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Services;

public class LeavePolicyService : ILeavePolicyService
{
    private readonly LmsDbContext _context;

    public LeavePolicyService(LmsDbContext context)
    {
        _context = context;
    }

    // ── Leave Types ───────────────────────────────────────────────────────────

    public async Task<IEnumerable<LeaveTypeDto>> GetAllLeaveTypesAsync()
    {
        var types = await _context.LeaveTypes
            .Where(lt => lt.DeletedAt == null && lt.IsActive)
            .OrderBy(lt => lt.Name)
            .ToListAsync();

        return types.Select(MapLeaveType);
    }

    public async Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(Guid id)
    {
        var lt = await _context.LeaveTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);

        return lt is null ? null : MapLeaveType(lt);
    }

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto)
    {
        var codeExists = await _context.LeaveTypes
            .AnyAsync(lt => lt.Code == dto.Code && lt.DeletedAt == null);

        if (codeExists)
            throw new InvalidOperationException($"A leave type with code '{dto.Code}' already exists.");

        var entity = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code.ToUpperInvariant(),
            Description = dto.Description,
            AnnualDays = dto.AnnualDays,
            RequiresAttachment = dto.RequiresAttachment,
            RequiresHrApproval = dto.RequiresHrApproval,
            IsActive = true
        };

        _context.LeaveTypes.Add(entity);
        await _context.SaveChangesAsync();
        return MapLeaveType(entity);
    }

    public async Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto)
    {
        var entity = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == id && lt.DeletedAt == null)
            ?? throw new KeyNotFoundException($"LeaveType {id} not found.");

        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.AnnualDays.HasValue) entity.AnnualDays = dto.AnnualDays.Value;
        if (dto.RequiresAttachment.HasValue) entity.RequiresAttachment = dto.RequiresAttachment.Value;
        if (dto.RequiresHrApproval.HasValue) entity.RequiresHrApproval = dto.RequiresHrApproval.Value;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();
        return MapLeaveType(entity);
    }

    // ── Leave Policies ────────────────────────────────────────────────────────

    public async Task<IEnumerable<LeavePolicyDto>> GetPoliciesForLeaveTypeAsync(Guid leaveTypeId)
    {
        var policies = await _context.LeavePolicies
            .Include(p => p.LeaveType)
            .Where(p => p.LeaveTypeId == leaveTypeId)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync();

        return policies.Select(MapPolicy);
    }

    public async Task<LeavePolicyDto?> GetActivePolicyAsync(Guid leaveTypeId)
    {
        var now = DateTime.UtcNow;

        var policy = await _context.LeavePolicies
            .Include(p => p.LeaveType)
            .Where(p => p.LeaveTypeId == leaveTypeId
                     && p.IsActive
                     && (p.EffectiveTo == null || p.EffectiveTo > now))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync();

        return policy is null ? null : MapPolicy(policy);
    }

    public async Task<LeavePolicyDto> CreatePolicyAsync(CreateLeavePolicyDto dto)
    {
        var leaveTypeExists = await _context.LeaveTypes
            .AnyAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

        if (!leaveTypeExists)
            throw new KeyNotFoundException($"LeaveType {dto.LeaveTypeId} not found.");

        var entity = new LeavePolicy
        {
            Id = Guid.NewGuid(),
            LeaveTypeId = dto.LeaveTypeId,
            Name = dto.Name ?? string.Empty,
            AnnualAllotment = dto.AnnualAllotment,
            MaxCarryForward = dto.MaxCarryForward,
            MaxConsecutiveDays = dto.MaxConsecutiveDays,
            MinNoticeDays = dto.MinNoticeDays,
            AccrualMonthly = dto.AccrualMonthly,
            AccrualRate = dto.AccrualRate,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo
        };

        _context.LeavePolicies.Add(entity);
        await _context.SaveChangesAsync();

        // Reload with navigation
        await _context.Entry(entity).Reference(p => p.LeaveType).LoadAsync();
        return MapPolicy(entity);
    }

    public async Task<LeavePolicyDto> UpdatePolicyAsync(Guid id, UpdateLeavePolicyDto dto)
    {
        var entity = await _context.LeavePolicies
            .Include(p => p.LeaveType)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"LeavePolicy {id} not found.");

        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.AnnualAllotment.HasValue) entity.AnnualAllotment = dto.AnnualAllotment.Value;
        if (dto.MaxCarryForward.HasValue) entity.MaxCarryForward = dto.MaxCarryForward.Value;
        if (dto.MaxConsecutiveDays.HasValue) entity.MaxConsecutiveDays = dto.MaxConsecutiveDays.Value;
        if (dto.MinNoticeDays.HasValue) entity.MinNoticeDays = dto.MinNoticeDays.Value;
        if (dto.AccrualMonthly.HasValue) entity.AccrualMonthly = dto.AccrualMonthly.Value;
        if (dto.AccrualRate.HasValue) entity.AccrualRate = dto.AccrualRate.Value;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.EffectiveFrom.HasValue) entity.EffectiveFrom = dto.EffectiveFrom.Value;
        if (dto.EffectiveTo.HasValue) entity.EffectiveTo = dto.EffectiveTo.Value;

        await _context.SaveChangesAsync();
        return MapPolicy(entity);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static LeaveTypeDto MapLeaveType(LeaveType lt) => new()
    {
        Id = lt.Id,
        Code = lt.Code,
        Name = lt.Name,
        Description = lt.Description,
        AnnualDays = lt.AnnualDays,
        RequiresAttachment = lt.RequiresAttachment,
        RequiresHrApproval = lt.RequiresHrApproval,
        IsActive = lt.IsActive
    };

    private static LeavePolicyDto MapPolicy(LeavePolicy p) => new()
    {
        Id = p.Id,
        LeaveTypeId = p.LeaveTypeId,
        LeaveTypeName = p.LeaveType?.Name ?? string.Empty,
        Name = p.Name,
        AnnualAllotment = p.AnnualAllotment,
        MaxCarryForward = p.MaxCarryForward,
        MaxConsecutiveDays = p.MaxConsecutiveDays,
        MinNoticeDays = p.MinNoticeDays,
        AccrualMonthly = p.AccrualMonthly,
        AccrualRate = p.AccrualRate,
        IsActive = p.IsActive,
        EffectiveFrom = p.EffectiveFrom,
        EffectiveTo = p.EffectiveTo
    };
}
