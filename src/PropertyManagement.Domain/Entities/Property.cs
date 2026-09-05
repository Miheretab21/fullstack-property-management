using PropertyManagement.Domain.Common;

namespace PropertyManagement.Domain.Entities;

public class Property : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SubCity { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid? AssignedManagerId { get; set; }

    public int TotalFloors { get; set; } = 1;
    public decimal TotalSquareMeters { get; set; }
    public string ConstructionStatus { get; set; } = "Completed";
    public string? FinishingNotes { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
