using PropertyManagement.Domain.Common;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Entities;

public class Unit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public decimal RentAmount { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Vacant;

    public int FloorNumber { get; set; } = 1;
    public decimal SquareMeters { get; set; }
    public string? FinishingNotes { get; set; }

    // Navigation properties
    public Property? Property { get; set; }
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}
