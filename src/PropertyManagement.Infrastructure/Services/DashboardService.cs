
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Common;
using PropertyManagement.Application.Models.Dashboard;
using PropertyManagement.Application.Models.Financial;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;

namespace PropertyManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService? _currentUserService;

    public DashboardService(
        IApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<DashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysFromNow = now.AddDays(30);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var isManager = _currentUserService != null &&
            _currentUserService.Roles.Contains("PropertyManager") &&
            !_currentUserService.Roles.Contains("Admin");
        var managerId = isManager ? (_currentUserService?.UserId ?? Guid.Empty) : Guid.Empty;

        // 1. Property & Unit stats
        var propertiesQuery = _context.Properties.AsQueryable();
        var unitsQuery = _context.Units.Include(u => u.Property).AsQueryable();
        var leasesQuery = _context.Leases.Include(l => l.Unit).ThenInclude(u => u!.Property).AsQueryable();
        var transactionsQuery = _context.Transactions.Include(t => t.Lease).ThenInclude(l => l!.Unit).ThenInclude(u => u!.Property).AsQueryable();
        var maintenanceQuery = _context.MaintenanceRequests.Include(m => m.Unit).ThenInclude(u => u!.Property).AsQueryable();

        if (isManager)
        {
            propertiesQuery = propertiesQuery.Where(p => p.AssignedManagerId == managerId);
            unitsQuery = unitsQuery.Where(u => u.Property != null && u.Property.AssignedManagerId == managerId);
            leasesQuery = leasesQuery.Where(l => l.Unit != null && l.Unit.Property != null && l.Unit.Property.AssignedManagerId == managerId);
            transactionsQuery = transactionsQuery.Where(t => t.Lease != null && t.Lease.Unit != null && t.Lease.Unit.Property != null && t.Lease.Unit.Property.AssignedManagerId == managerId);
            maintenanceQuery = maintenanceQuery.Where(m => m.Unit != null && m.Unit.Property != null && m.Unit.Property.AssignedManagerId == managerId);
        }

        var totalProperties = await propertiesQuery.CountAsync(cancellationToken);
        var totalUnits = await unitsQuery.CountAsync(cancellationToken);
        var occupiedUnits = await unitsQuery.CountAsync(u => u.Status == UnitStatus.Occupied, cancellationToken);
        var vacantUnits = await unitsQuery.CountAsync(u => u.Status == UnitStatus.Vacant, cancellationToken);
        var maintenanceUnits = await unitsQuery.CountAsync(u => u.Status == UnitStatus.Maintenance, cancellationToken);
        var finishingUnits = await unitsQuery.CountAsync(u => u.Status == UnitStatus.UnderFinishing, cancellationToken);

        var occupancyRate = totalUnits > 0 
            ? Math.Round(((decimal)occupiedUnits / totalUnits) * 100, 1) 
            : 0;

        // 2. Revenue stats
        var activeLeases = await leasesQuery
            .AsNoTracking()
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

        var projectedMonthlyRevenue = activeLeases.Sum(l => l.MonthlyRent);

        var currentMonthTransactions = await transactionsQuery
            .AsNoTracking()
            .Where(t => t.PaymentDate >= startOfMonth)
            .ToListAsync(cancellationToken);

        var collectedRevenue = currentMonthTransactions
            .Where(t => t.Status == TransactionStatus.Paid)
            .Sum(t => t.Amount);

        var pendingRevenue = currentMonthTransactions
            .Where(t => t.Status == TransactionStatus.Pending)
            .Sum(t => t.Amount);

        // 3. Maintenance requests count
        var activeMaintenance = await maintenanceQuery
            .CountAsync(m => m.Status != MaintenanceStatus.Closed, cancellationToken);

        // 4. Expiring leases (next 30 days)
        var expiringLeasesQuery = await leasesQuery
            .AsNoTracking()
            .Where(l => l.IsActive && l.EndDate >= now && l.EndDate <= thirtyDaysFromNow)
            .OrderBy(l => l.EndDate)
            .ToListAsync(cancellationToken);

        var expiringLeaseDtos = new List<ExpiringLeaseSummaryDto>();
        foreach (var l in expiringLeasesQuery)
        {
            var tenant = await _userManager.FindByIdAsync(l.TenantId.ToString());
            var tenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}".Trim() : "Unknown";

            expiringLeaseDtos.Add(new ExpiringLeaseSummaryDto
            {
                LeaseId = l.Id,
                UnitNumber = l.Unit?.UnitNumber ?? string.Empty,
                PropertyName = l.Unit?.Property?.Name ?? string.Empty,
                TenantName = tenantName,
                EndDate = l.EndDate,
                DaysRemaining = (int)Math.Ceiling((l.EndDate - now).TotalDays),
                MonthlyRent = l.MonthlyRent
            });
        }

        // 5. Recent transactions (top 5)
        var recentTxs = await transactionsQuery
            .AsNoTracking()
            .OrderByDescending(t => t.PaymentDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentTxDtos = new List<TransactionDto>();
        foreach (var t in recentTxs)
        {
            var tenantName = "Unknown";
            if (t.Lease != null)
            {
                var tenant = await _userManager.FindByIdAsync(t.Lease.TenantId.ToString());
                if (tenant != null)
                {
                    tenantName = $"{tenant.FirstName} {tenant.LastName}".Trim();
                }
            }

            recentTxDtos.Add(new TransactionDto
            {
                Id = t.Id,
                LeaseId = t.LeaseId,
                UnitId = t.Lease?.UnitId ?? Guid.Empty,
                UnitNumber = t.Lease?.Unit?.UnitNumber ?? string.Empty,
                PropertyName = t.Lease?.Unit?.Property?.Name ?? string.Empty,
                TenantId = t.Lease?.TenantId ?? Guid.Empty,
                TenantName = tenantName,
                Amount = t.Amount,
                PaymentDate = t.PaymentDate,
                PaymentMethod = t.PaymentMethod,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc
            });
        }

        var metrics = new DashboardMetricsDto
        {
            TotalProperties = totalProperties,
            TotalUnits = totalUnits,
            OccupiedUnits = occupiedUnits,
            VacantUnits = vacantUnits,
            MaintenanceUnits = maintenanceUnits,
            FinishingUnits = finishingUnits,
            OccupancyRate = occupancyRate,
            TotalMonthlyProjectedRevenue = projectedMonthlyRevenue,
            CurrentMonthCollectedRevenue = collectedRevenue,
            CurrentMonthPendingRevenue = pendingRevenue,
            ActiveMaintenanceRequests = activeMaintenance,
            ExpiringLeases = expiringLeaseDtos,
            RecentTransactions = recentTxDtos
        };

        return ServiceResult<DashboardMetricsDto>.Success(metrics);
    }
}
