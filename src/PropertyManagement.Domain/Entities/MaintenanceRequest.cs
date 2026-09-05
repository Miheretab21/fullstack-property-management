using PropertyManagement.Domain.Common;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Entities;

public class MaintenanceRequest : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public string? PhotoUrl { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? AssignedTechnician { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    // Navigation property
    public Unit? Unit { get; set; }
}
