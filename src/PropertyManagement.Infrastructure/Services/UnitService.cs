using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Units;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Infrastructure.Services;

public class UnitService : IUnitService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUserService;

    public UnitService(IApplicationDbContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<List<UnitDto>>> GetUnitsAsync(Guid? propertyId = null, UnitStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Units
            .AsNoTracking()
            .Include(u => u.Property)
            .AsQueryable();

        // If caller is PropertyManager (and not Admin and not Tenant), filter by properties assigned to them
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            query = query.Where(u => u.Property != null && u.Property.AssignedManagerId == managerId);
        }

        if (propertyId.HasValue)
        {
            query = query.Where(u => u.PropertyId == propertyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var units = await query.Select(u => new UnitDto
        {
            Id = u.Id,
            PropertyId = u.PropertyId,
            PropertyName = u.Property != null ? u.Property.Name : string.Empty,
            SubCity = u.Property != null ? u.Property.SubCity : string.Empty,
            UnitNumber = u.UnitNumber,
            FloorNumber = u.FloorNumber,
            SquareMeters = u.SquareMeters,
            Bedrooms = u.Bedrooms,
            Bathrooms = u.Bathrooms,
            RentAmount = u.RentAmount,
            Status = u.Status,
            FinishingNotes = u.FinishingNotes,
            CreatedAtUtc = u.CreatedAtUtc
        }).ToListAsync(cancellationToken);

        return ServiceResult<List<UnitDto>>.Success(units);
    }

    public async Task<ServiceResult<UnitDto>> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _context.Units
            .AsNoTracking()
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
        {
            return ServiceResult<UnitDto>.Failure("Unit not found.");
        }

        var dto = new UnitDto
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            PropertyName = unit.Property?.Name ?? string.Empty,
            SubCity = unit.Property?.SubCity ?? string.Empty,
            UnitNumber = unit.UnitNumber,
            FloorNumber = unit.FloorNumber,
            SquareMeters = unit.SquareMeters,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            RentAmount = unit.RentAmount,
            Status = unit.Status,
            FinishingNotes = unit.FinishingNotes,
            CreatedAtUtc = unit.CreatedAtUtc
        };

        return ServiceResult<UnitDto>.Success(dto);
    }

    public async Task<ServiceResult<UnitDto>> CreateUnitAsync(CreateUnitDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == dto.PropertyId, cancellationToken);
        if (property == null)
        {
            return ServiceResult<UnitDto>.Failure("Referenced Property does not exist.");
        }

        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (property.AssignedManagerId != managerId)
            {
                return ServiceResult<UnitDto>.Failure("You can only add units to properties assigned to your management portfolio.");
            }
        }

        var exists = await _context.Units.AnyAsync(
            u => u.PropertyId == dto.PropertyId && u.UnitNumber.ToLower() == dto.UnitNumber.ToLower(),
            cancellationToken);

        if (exists)
        {
            return ServiceResult<UnitDto>.Failure($"Unit number '{dto.UnitNumber}' already exists in this property.");
        }

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            PropertyId = dto.PropertyId,
            UnitNumber = dto.UnitNumber.Trim(),
            FloorNumber = dto.FloorNumber,
            SquareMeters = dto.SquareMeters,
            Bedrooms = dto.Bedrooms,
            Bathrooms = dto.Bathrooms,
            RentAmount = dto.RentAmount,
            Status = dto.Status,
            FinishingNotes = dto.FinishingNotes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync(cancellationToken);

        var result = new UnitDto
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            PropertyName = property.Name,
            SubCity = property.SubCity,
            UnitNumber = unit.UnitNumber,
            FloorNumber = unit.FloorNumber,
            SquareMeters = unit.SquareMeters,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            RentAmount = unit.RentAmount,
            Status = unit.Status,
            FinishingNotes = unit.FinishingNotes,
            CreatedAtUtc = unit.CreatedAtUtc
        };

        return ServiceResult<UnitDto>.Success(result, "Unit created successfully.");
    }

    public async Task<ServiceResult<UnitDto>> UpdateUnitAsync(Guid id, UpdateUnitDto dto, CancellationToken cancellationToken = default)
    {
        var unit = await _context.Units
            .Include(u => u.Property)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (unit == null)
        {
            return ServiceResult<UnitDto>.Failure("Unit not found.");
        }

        var duplicate = await _context.Units.AnyAsync(
            u => u.PropertyId == unit.PropertyId && u.Id != id && u.UnitNumber.ToLower() == dto.UnitNumber.ToLower(),
            cancellationToken);

        if (duplicate)
        {
            return ServiceResult<UnitDto>.Failure($"Unit number '{dto.UnitNumber}' already exists in this property.");
        }

        unit.UnitNumber = dto.UnitNumber.Trim();
        if (dto.FloorNumber >= -5)
        {
            unit.FloorNumber = dto.FloorNumber;
        }
        unit.SquareMeters = dto.SquareMeters;
        unit.Bedrooms = dto.Bedrooms;
        unit.Bathrooms = dto.Bathrooms;
        unit.RentAmount = dto.RentAmount;
        unit.Status = dto.Status;
        unit.FinishingNotes = dto.FinishingNotes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var result = new UnitDto
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            PropertyName = unit.Property?.Name ?? string.Empty,
            SubCity = unit.Property?.SubCity ?? string.Empty,
            UnitNumber = unit.UnitNumber,
            FloorNumber = unit.FloorNumber,
            SquareMeters = unit.SquareMeters,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            RentAmount = unit.RentAmount,
            Status = unit.Status,
            FinishingNotes = unit.FinishingNotes,
            CreatedAtUtc = unit.CreatedAtUtc
        };

        return ServiceResult<UnitDto>.Success(result, "Unit updated successfully.");
    }

    public async Task<ServiceResult<UnitDto>> UpdateUnitStatusAsync(Guid id, UnitStatus newStatus, CancellationToken cancellationToken = default)
    {
        var unit = await _context.Units
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
        {
            return ServiceResult<UnitDto>.Failure("Unit not found.");
        }

        unit.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);

        var result = new UnitDto
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            PropertyName = unit.Property?.Name ?? string.Empty,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            RentAmount = unit.RentAmount,
            Status = unit.Status,
            CreatedAtUtc = unit.CreatedAtUtc
        };

        return ServiceResult<UnitDto>.Success(result, "Unit status updated successfully.");
    }

    public async Task<ServiceResult> DeleteUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _context.Units
            .Include(u => u.Leases)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (unit == null)
        {
            return ServiceResult.Failure("Unit not found.");
        }

        if (unit.Leases.Any(l => l.IsActive))
        {
            return ServiceResult.Failure("Cannot delete a unit with an active lease. Terminate the lease first.");
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success("Unit deleted successfully.");
    }
}
