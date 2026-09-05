using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Maintenance;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService? _currentUserService;

    public MaintenanceService(
        IApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<List<MaintenanceRequestDto>>> GetRequestsAsync(Guid? tenantId = null, Guid? unitId = null, MaintenanceStatus? status = null, MaintenancePriority? priority = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MaintenanceRequests
            .AsNoTracking()
            .Include(m => m.Unit)
                .ThenInclude(u => u!.Property)
            .AsQueryable();

        // If caller is PropertyManager (and not Admin), filter by assigned property
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            query = query.Where(m => m.Unit != null && m.Unit.Property != null && m.Unit.Property.AssignedManagerId == managerId);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(m => m.TenantId == tenantId.Value);
        }

        if (unitId.HasValue)
        {
            query = query.Where(m => m.UnitId == unitId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(m => m.Priority == priority.Value);
        }

        var requests = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = new List<MaintenanceRequestDto>();
        foreach (var m in requests)
        {
            var tenant = await _userManager.FindByIdAsync(m.TenantId.ToString());
            var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

            dtos.Add(new MaintenanceRequestDto
            {
                Id = m.Id,
                UnitId = m.UnitId,
                UnitNumber = m.Unit?.UnitNumber ?? string.Empty,
                PropertyName = m.Unit?.Property?.Name ?? string.Empty,
                TenantId = m.TenantId,
                TenantName = tenantName,
                Title = m.Title,
                Description = m.Description,
                Priority = m.Priority,
                Status = m.Status,
                PhotoUrl = m.PhotoUrl,
                ResolutionNotes = m.ResolutionNotes,
                AssignedTechnician = m.AssignedTechnician,
                CreatedAtUtc = m.CreatedAtUtc,
                ResolvedAtUtc = m.ResolvedAtUtc
            });
        }

        return ServiceResult<List<MaintenanceRequestDto>>.Success(dtos);
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var m = await _context.MaintenanceRequests
            .AsNoTracking()
            .Include(m => m.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (m == null)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure("Maintenance request not found.");
        }

        var tenant = await _userManager.FindByIdAsync(m.TenantId.ToString());
        var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

        var dto = new MaintenanceRequestDto
        {
            Id = m.Id,
            UnitId = m.UnitId,
            UnitNumber = m.Unit?.UnitNumber ?? string.Empty,
            PropertyName = m.Unit?.Property?.Name ?? string.Empty,
            TenantId = m.TenantId,
            TenantName = tenantName,
            Title = m.Title,
            Description = m.Description,
            Priority = m.Priority,
            Status = m.Status,
            PhotoUrl = m.PhotoUrl,
            ResolutionNotes = m.ResolutionNotes,
            AssignedTechnician = m.AssignedTechnician,
            CreatedAtUtc = m.CreatedAtUtc,
            ResolvedAtUtc = m.ResolvedAtUtc
        };

        return ServiceResult<MaintenanceRequestDto>.Success(dto);
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> CreateRequestAsync(CreateMaintenanceRequestDto dto, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var unit = await _context.Units
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken);

        if (unit == null)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure("Referenced Unit does not exist.");
        }

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = dto.UnitId,
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            Status = MaintenanceStatus.Open,
            PhotoUrl = dto.PhotoUrl,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.MaintenanceRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(tenantId.ToString());
        var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

        var resultDto = new MaintenanceRequestDto
        {
            Id = request.Id,
            UnitId = request.UnitId,
            UnitNumber = unit.UnitNumber,
            PropertyName = unit.Property?.Name ?? string.Empty,
            TenantId = tenantId,
            TenantName = tenantName,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            PhotoUrl = request.PhotoUrl,
            CreatedAtUtc = request.CreatedAtUtc
        };

        return ServiceResult<MaintenanceRequestDto>.Success(resultDto, "Maintenance request submitted successfully.");
    }

    public async Task<ServiceResult<MaintenanceRequestDto>> UpdateRequestStatusAsync(Guid id, UpdateMaintenanceStatusDto dto, CancellationToken cancellationToken = default)
    {
        var request = await _context.MaintenanceRequests
            .Include(m => m.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (request == null)
        {
            return ServiceResult<MaintenanceRequestDto>.Failure("Maintenance request not found.");
        }

        request.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.ResolutionNotes))
        {
            request.ResolutionNotes = dto.ResolutionNotes;
        }

        if (!string.IsNullOrEmpty(dto.AssignedTechnician))
        {
            request.AssignedTechnician = dto.AssignedTechnician;
        }

        if (dto.Status == MaintenanceStatus.Closed)
        {
            request.ResolvedAtUtc = DateTime.UtcNow;
        }
        else
        {
            request.ResolvedAtUtc = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(request.TenantId.ToString());
        var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

        var resultDto = new MaintenanceRequestDto
        {
            Id = request.Id,
            UnitId = request.UnitId,
            UnitNumber = request.Unit?.UnitNumber ?? string.Empty,
            PropertyName = request.Unit?.Property?.Name ?? string.Empty,
            TenantId = request.TenantId,
            TenantName = tenantName,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            PhotoUrl = request.PhotoUrl,
            ResolutionNotes = request.ResolutionNotes,
            AssignedTechnician = request.AssignedTechnician,
            CreatedAtUtc = request.CreatedAtUtc,
            ResolvedAtUtc = request.ResolvedAtUtc
        };

        return ServiceResult<MaintenanceRequestDto>.Success(resultDto, "Maintenance request updated successfully.");
    }

    public async Task<ServiceResult> DeleteRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _context.MaintenanceRequests
            .Include(m => m.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (request == null)
        {
            return ServiceResult.Failure("Maintenance request not found.");
        }

        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (request.Unit?.Property?.AssignedManagerId != managerId)
            {
                return ServiceResult.Failure("You can only delete maintenance tickets for properties in your assigned portfolio.");
            }
        }

        _context.MaintenanceRequests.Remove(request);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success("Maintenance request deleted successfully.");
    }
}
