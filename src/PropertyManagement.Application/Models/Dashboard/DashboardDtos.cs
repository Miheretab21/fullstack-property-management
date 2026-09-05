using PropertyManagement.Application.Models.Financial;

namespace PropertyManagement.Application.Models.Dashboard;

public class ExpiringLeaseSummaryDto
{
    public Guid LeaseId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal MonthlyRent { get; set; }
}

public class DashboardMetricsDto
{
    public int TotalProperties { get; set; }
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public int MaintenanceUnits { get; set; }
    public int FinishingUnits { get; set; }
    public decimal OccupancyRate { get; set; }

    public decimal TotalMonthlyProjectedRevenue { get; set; }
    public decimal CurrentMonthCollectedRevenue { get; set; }
    public decimal CurrentMonthPendingRevenue { get; set; }

    public int ActiveMaintenanceRequests { get; set; }
    public int ExpiringLeasesCount => ExpiringLeases.Count;
    public List<ExpiringLeaseSummaryDto> ExpiringLeases { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}
