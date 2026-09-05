using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Properties;
using PropertyManagement.Application.Models.Units;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class PropertyService : IPropertyService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser>? _userManager;
    private readonly ICurrentUserService? _currentUserService;

    public PropertyService(IApplicationDbContext context)
    {
        _context = context;
    }

    public PropertyService(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    private async Task<string?> GetManagerNameAsync(Guid? managerId)
    {
        if (!managerId.HasValue || _userManager == null) return null;
        var user = await _userManager.FindByIdAsync(managerId.Value.ToString());
        return user != null ? $"{user.FirstName} {user.LastName}".Trim() : null;
    }

    public async Task<ServiceResult<List<PropertyDto>>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var query = _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
            .AsQueryable();

        // If caller is PropertyManager (and not Admin), scope to properties assigned to them
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            query = query.Where(p => p.AssignedManagerId == managerId);
        }

        var properties = await query
            .Select(p => new PropertyDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                SubCity = p.SubCity,
                City = p.City,
                OwnerId = p.OwnerId,
                AssignedManagerId = p.AssignedManagerId,
                TotalFloors = p.TotalFloors,
                TotalSquareMeters = p.TotalSquareMeters,
                ConstructionStatus = p.ConstructionStatus,
                FinishingNotes = p.FinishingNotes,
                TotalUnits = p.Units.Count,
                OccupiedUnits = p.Units.Count(u => u.Status == UnitStatus.Occupied),
                VacantUnits = p.Units.Count(u => u.Status == UnitStatus.Vacant),
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        if (_userManager != null)
        {
            foreach (var prop in properties)
            {
                if (prop.AssignedManagerId.HasValue)
                {
                    prop.AssignedManagerName = await GetManagerNameAsync(prop.AssignedManagerId);
                }
            }
        }

        return ServiceResult<List<PropertyDto>>.Success(properties);
    }

    public async Task<ServiceResult<PropertyDetailDto>> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property == null)
        {
            return ServiceResult<PropertyDetailDto>.Failure("Property not found.");
        }

        // If PropertyManager, check assignment
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (property.AssignedManagerId != managerId)
            {
                return ServiceResult<PropertyDetailDto>.Failure("You are not assigned to manage this property.");
            }
        }

        var managerName = await GetManagerNameAsync(property.AssignedManagerId);

        var dto = new PropertyDetailDto
        {
            Id = property.Id,
            Name = property.Name,
            Address = property.Address,
            SubCity = property.SubCity,
            City = property.City,
            OwnerId = property.OwnerId,
            AssignedManagerId = property.AssignedManagerId,
            AssignedManagerName = managerName,
            TotalFloors = property.TotalFloors,
            TotalSquareMeters = property.TotalSquareMeters,
            ConstructionStatus = property.ConstructionStatus,
            FinishingNotes = property.FinishingNotes,
            TotalUnits = property.Units.Count,
            OccupiedUnits = property.Units.Count(u => u.Status == UnitStatus.Occupied),
            VacantUnits = property.Units.Count(u => u.Status == UnitStatus.Vacant),
            CreatedAtUtc = property.CreatedAtUtc,
            Units = property.Units.Select(u => new UnitDto
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                PropertyName = property.Name,
                SubCity = property.SubCity,
                UnitNumber = u.UnitNumber,
                FloorNumber = u.FloorNumber,
                SquareMeters = u.SquareMeters,
                Bedrooms = u.Bedrooms,
                Bathrooms = u.Bathrooms,
                RentAmount = u.RentAmount,
                Status = u.Status,
                FinishingNotes = u.FinishingNotes,
                CreatedAtUtc = u.CreatedAtUtc
            }).ToList()
        };

        return ServiceResult<PropertyDetailDto>.Success(dto);
    }

    public async Task<ServiceResult<PropertyDto>> CreatePropertyAsync(CreatePropertyDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Guid? assignedManagerId = dto.AssignedManagerId;
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            assignedManagerId = _currentUserService.UserId;
        }

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Address = dto.Address,
            SubCity = dto.SubCity?.Trim() ?? string.Empty,
            City = dto.City,
            OwnerId = dto.OwnerId ?? currentUserId,
            AssignedManagerId = assignedManagerId,
            TotalFloors = dto.TotalFloors > 0 ? dto.TotalFloors : 1,
            TotalSquareMeters = dto.TotalSquareMeters,
            ConstructionStatus = string.IsNullOrWhiteSpace(dto.ConstructionStatus) ? "Completed" : dto.ConstructionStatus.Trim(),
            FinishingNotes = dto.FinishingNotes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync(cancellationToken);

        var managerName = await GetManagerNameAsync(property.AssignedManagerId);

        var result = new PropertyDto
        {
            Id = property.Id,
            Name = property.Name,
            Address = property.Address,
            SubCity = property.SubCity,
            City = property.City,
            OwnerId = property.OwnerId,
            AssignedManagerId = property.AssignedManagerId,
            AssignedManagerName = managerName,
            TotalFloors = property.TotalFloors,
            TotalSquareMeters = property.TotalSquareMeters,
            ConstructionStatus = property.ConstructionStatus,
            FinishingNotes = property.FinishingNotes,
            TotalUnits = 0,
            OccupiedUnits = 0,
            VacantUnits = 0,
            CreatedAtUtc = property.CreatedAtUtc
        };

        return ServiceResult<PropertyDto>.Success(result, "Property created successfully.");
    }

    public async Task<ServiceResult<PropertyDto>> UpdatePropertyAsync(Guid id, UpdatePropertyDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _context.Properties
            .Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property == null)
        {
            return ServiceResult<PropertyDto>.Failure("Property not found.");
        }

        // If PropertyManager, check assignment
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (property.AssignedManagerId != managerId)
            {
                return ServiceResult<PropertyDto>.Failure("You are not assigned to manage this property.");
            }
        }
        else if (_currentUserService == null || _currentUserService.Roles.Contains("Admin"))
        {
            // Admin can assign or reassign manager
            property.AssignedManagerId = dto.AssignedManagerId;
        }

        property.Name = dto.Name;
        property.Address = dto.Address;
        property.SubCity = dto.SubCity?.Trim() ?? string.Empty;
        property.City = dto.City;
        if (dto.OwnerId.HasValue)
        {
            property.OwnerId = dto.OwnerId.Value;
        }
        if (dto.TotalFloors > 0)
        {
            property.TotalFloors = dto.TotalFloors;
        }
        property.TotalSquareMeters = dto.TotalSquareMeters;
        if (!string.IsNullOrWhiteSpace(dto.ConstructionStatus))
        {
            property.ConstructionStatus = dto.ConstructionStatus.Trim();
        }
        property.FinishingNotes = dto.FinishingNotes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var managerName = await GetManagerNameAsync(property.AssignedManagerId);

        var result = new PropertyDto
        {
            Id = property.Id,
            Name = property.Name,
            Address = property.Address,
            SubCity = property.SubCity,
            City = property.City,
            OwnerId = property.OwnerId,
            AssignedManagerId = property.AssignedManagerId,
            AssignedManagerName = managerName,
            TotalFloors = property.TotalFloors,
            TotalSquareMeters = property.TotalSquareMeters,
            ConstructionStatus = property.ConstructionStatus,
            FinishingNotes = property.FinishingNotes,
            TotalUnits = property.Units.Count,
            OccupiedUnits = property.Units.Count(u => u.Status == UnitStatus.Occupied),
            VacantUnits = property.Units.Count(u => u.Status == UnitStatus.Vacant),
            CreatedAtUtc = property.CreatedAtUtc
        };

        return ServiceResult<PropertyDto>.Success(result, "Property updated successfully.");
    }

    public async Task<ServiceResult> DeletePropertyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _context.Properties
            .Include(p => p.Units)
            .ThenInclude(u => u.Leases)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property == null)
        {
            return ServiceResult.Failure("Property not found.");
        }

        var hasActiveLeases = property.Units.Any(u => u.Leases.Any(l => l.IsActive));
        if (hasActiveLeases)
        {
            return ServiceResult.Failure("Cannot delete property with active leases. Terminate all leases first.");
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success("Property deleted successfully.");
    }
}
