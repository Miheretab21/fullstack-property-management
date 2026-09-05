using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Maintenance;

public class MaintenanceRequestDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? AssignedTechnician { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class CreateMaintenanceRequestDto
{
    [Required]
    public Guid UnitId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    [MaxLength(1000)]
    public string? PhotoUrl { get; set; }
}

public class UpdateMaintenanceStatusDto
{
    [Required]
    public MaintenanceStatus Status { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    [MaxLength(200)]
    public string? AssignedTechnician { get; set; }
}
