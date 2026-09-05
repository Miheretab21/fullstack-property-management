using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Leases;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class LeaseService : ILeaseService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService? _currentUserService;

    public LeaseService(
        IApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<List<LeaseDto>>> GetLeasesAsync(Guid? tenantId = null, Guid? unitId = null, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .AsQueryable();

        // If caller is PropertyManager (and not Admin and not Tenant), filter by assigned property
        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            query = query.Where(l => l.Unit != null && l.Unit.Property != null && l.Unit.Property.AssignedManagerId == managerId);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(l => l.TenantId == tenantId.Value);
        }

        if (unitId.HasValue)
        {
            query = query.Where(l => l.UnitId == unitId.Value);
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(l => l.IsActive == activeOnly.Value);
        }

        var leases = await query.ToListAsync(cancellationToken);
        var dtos = new List<LeaseDto>();

        foreach (var l in leases)
        {
            var tenant = await _userManager.FindByIdAsync(l.TenantId.ToString());
            dtos.Add(new LeaseDto
            {
                Id = l.Id,
                UnitId = l.UnitId,
                UnitNumber = l.Unit?.UnitNumber ?? string.Empty,
                PropertyId = l.Unit?.PropertyId ?? Guid.Empty,
                PropertyName = l.Unit?.Property?.Name ?? string.Empty,
                TenantId = l.TenantId,
                TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown",
                TenantEmail = tenant?.Email ?? string.Empty,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                MonthlyRent = l.MonthlyRent,
                SecurityDeposit = l.SecurityDeposit,
                IsActive = l.IsActive,
                CreatedAtUtc = l.CreatedAtUtc
            });
        }

        return ServiceResult<List<LeaseDto>>.Success(dtos);
    }

    public async Task<ServiceResult<LeaseDto>> GetLeaseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lease == null)
        {
            return ServiceResult<LeaseDto>.Failure("Lease not found.");
        }

        var tenant = await _userManager.FindByIdAsync(lease.TenantId.ToString());
        var dto = new LeaseDto
        {
            Id = lease.Id,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
            PropertyId = lease.Unit?.PropertyId ?? Guid.Empty,
            PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
            TenantId = lease.TenantId,
            TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown",
            TenantEmail = tenant?.Email ?? string.Empty,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent,
            SecurityDeposit = lease.SecurityDeposit,
            IsActive = lease.IsActive,
            CreatedAtUtc = lease.CreatedAtUtc
        };

        return ServiceResult<LeaseDto>.Success(dto);
    }

    public async Task<ServiceResult<LeaseDto>> CreateLeaseAsync(CreateLeaseDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            return ServiceResult<LeaseDto>.Failure("Lease Start Date must be earlier than End Date.");
        }

        var unit = await _context.Units
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == dto.UnitId, cancellationToken);

        if (unit == null)
        {
            return ServiceResult<LeaseDto>.Failure("Referenced Unit does not exist.");
        }

        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (unit.Property?.AssignedManagerId != managerId)
            {
                return ServiceResult<LeaseDto>.Failure("You can only create leases for units in properties assigned to your management portfolio.");
            }
        }

        var tenant = await _userManager.FindByIdAsync(dto.TenantId.ToString());
        if (tenant == null)
        {
            return ServiceResult<LeaseDto>.Failure("Referenced Tenant user does not exist.");
        }

        // Relational Overlap Validation: Ensure unit is not double-leased
        var isOverlapping = await _context.Leases
            .AnyAsync(l => l.UnitId == dto.UnitId 
                        && l.IsActive 
                        && l.StartDate < dto.EndDate 
                        && dto.StartDate < l.EndDate,
                      cancellationToken);

        if (isOverlapping)
        {
            return ServiceResult<LeaseDto>.Failure("Double-lease conflict: This unit already has an active lease covering the specified dates.");
        }

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = dto.UnitId,
            TenantId = dto.TenantId,
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc),
            MonthlyRent = dto.MonthlyRent,
            SecurityDeposit = dto.SecurityDeposit,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Automatically set Unit status to Occupied
        unit.Status = UnitStatus.Occupied;

        // Auto-generate initial rent charge or record initial payment (e.g. check payment)
        Transaction initialTransaction;
        if (dto.RecordInitialPayment)
        {
            var method = !string.IsNullOrWhiteSpace(dto.PaymentMethod) ? dto.PaymentMethod.Trim() : "Check";
            if (!string.IsNullOrWhiteSpace(dto.CheckNumber))
            {
                method += $" #{dto.CheckNumber.Trim()}";
            }
            if (!string.IsNullOrWhiteSpace(dto.BankName))
            {
                method += $" ({dto.BankName.Trim()})";
            }

            var paidAmount = dto.InitialPaymentAmount.HasValue && dto.InitialPaymentAmount.Value > 0
                ? dto.InitialPaymentAmount.Value
                : lease.MonthlyRent;

            initialTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                LeaseId = lease.Id,
                Amount = paidAmount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = method,
                Status = dto.IsPaymentCleared ? TransactionStatus.Paid : TransactionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
        else
        {
            initialTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                LeaseId = lease.Id,
                Amount = lease.MonthlyRent,
                PaymentDate = lease.StartDate,
                PaymentMethod = "Scheduled Rent",
                Status = TransactionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        _context.Leases.Add(lease);
        _context.Transactions.Add(initialTransaction);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new LeaseDto
        {
            Id = lease.Id,
            UnitId = unit.Id,
            UnitNumber = unit.UnitNumber,
            PropertyId = unit.PropertyId,
            PropertyName = unit.Property?.Name ?? string.Empty,
            TenantId = tenant.Id,
            TenantName = $"{tenant.FirstName} {tenant.LastName}".Trim(),
            TenantEmail = tenant.Email ?? string.Empty,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent,
            SecurityDeposit = lease.SecurityDeposit,
            IsActive = lease.IsActive,
            CreatedAtUtc = lease.CreatedAtUtc
        };

        return ServiceResult<LeaseDto>.Success(resultDto, "Lease created and unit marked as Occupied.");
    }

    public async Task<ServiceResult<LeaseDto>> UpdateLeaseAsync(Guid id, UpdateLeaseDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            return ServiceResult<LeaseDto>.Failure("Lease Start Date must be earlier than End Date.");
        }

        var lease = await _context.Leases
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lease == null)
        {
            return ServiceResult<LeaseDto>.Failure("Lease not found.");
        }

        if (dto.IsActive)
        {
            // Check overlap excluding current lease
            var isOverlapping = await _context.Leases
                .AnyAsync(l => l.UnitId == lease.UnitId 
                            && l.Id != id 
                            && l.IsActive 
                            && l.StartDate < dto.EndDate 
                            && dto.StartDate < l.EndDate,
                          cancellationToken);

            if (isOverlapping)
            {
                return ServiceResult<LeaseDto>.Failure("Double-lease conflict: This unit already has another active lease covering the specified dates.");
            }
        }

        lease.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        lease.EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);
        lease.MonthlyRent = dto.MonthlyRent;
        lease.SecurityDeposit = dto.SecurityDeposit;
        lease.IsActive = dto.IsActive;

        // If deactivated, check if unit should be set to Vacant
        if (!dto.IsActive && lease.Unit != null)
        {
            var otherActive = await _context.Leases
                .AnyAsync(l => l.UnitId == lease.UnitId && l.Id != id && l.IsActive, cancellationToken);

            if (!otherActive)
            {
                lease.Unit.Status = UnitStatus.Vacant;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var tenant = await _userManager.FindByIdAsync(lease.TenantId.ToString());
        var resultDto = new LeaseDto
        {
            Id = lease.Id,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber ?? string.Empty,
            PropertyId = lease.Unit?.PropertyId ?? Guid.Empty,
            PropertyName = lease.Unit?.Property?.Name ?? string.Empty,
            TenantId = lease.TenantId,
            TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown",
            TenantEmail = tenant?.Email ?? string.Empty,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent,
            SecurityDeposit = lease.SecurityDeposit,
            IsActive = lease.IsActive,
            CreatedAtUtc = lease.CreatedAtUtc
        };

        return ServiceResult<LeaseDto>.Success(resultDto, "Lease updated successfully.");
    }

    public async Task<ServiceResult> TerminateLeaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lease == null)
        {
            return ServiceResult.Failure("Lease not found.");
        }

        if (!lease.IsActive)
        {
            return ServiceResult.Failure("Lease is already inactive.");
        }

        lease.IsActive = false;

        if (lease.Unit != null)
        {
            var otherActive = await _context.Leases
                .AnyAsync(l => l.UnitId == lease.UnitId && l.Id != id && l.IsActive, cancellationToken);

            if (!otherActive)
            {
                lease.Unit.Status = UnitStatus.Vacant;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Lease terminated successfully and unit status updated.");
    }

    public async Task<ServiceResult> DeleteLeaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lease = await _context.Leases
            .Include(l => l.Unit)
                .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lease == null)
        {
            return ServiceResult.Failure("Lease not found.");
        }

        if (_currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin"))
        {
            var managerId = _currentUserService.UserId ?? Guid.Empty;
            if (lease.Unit?.Property?.AssignedManagerId != managerId)
            {
                return ServiceResult.Failure("You can only delete leases for properties in your assigned management portfolio.");
            }
        }

        var unit = lease.Unit;
        var unitId = lease.UnitId;

        _context.Leases.Remove(lease);

        if (unit != null)
        {
            var otherActive = await _context.Leases
                .AnyAsync(l => l.UnitId == unitId && l.Id != id && l.IsActive, cancellationToken);

            if (!otherActive && unit.Status == UnitStatus.Occupied)
            {
                unit.Status = UnitStatus.Vacant;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Lease permanently deleted and unit occupancy updated.");
    }
}
