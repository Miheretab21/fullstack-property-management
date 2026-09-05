using System.ComponentModel.DataAnnotations;
using PropertyManagement.Application.Models.Units;

namespace PropertyManagement.Application.Models.Properties;

public class PropertyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SubCity { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid? AssignedManagerId { get; set; }
    public string? AssignedManagerName { get; set; }
    public int TotalFloors { get; set; }
    public decimal TotalSquareMeters { get; set; }
    public string ConstructionStatus { get; set; } = "Completed";
    public string? FinishingNotes { get; set; }
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class PropertyDetailDto : PropertyDto
{
    public List<UnitDto> Units { get; set; } = new();
}

public class CreatePropertyDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SubCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public Guid? OwnerId { get; set; }
    public Guid? AssignedManagerId { get; set; }

    [Range(1, 150)]
    public int TotalFloors { get; set; } = 1;

    [Range(0, 1000000)]
    public decimal TotalSquareMeters { get; set; }

    [MaxLength(50)]
    public string ConstructionStatus { get; set; } = "Completed";

    [MaxLength(1000)]
    public string? FinishingNotes { get; set; }
}

public class UpdatePropertyDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SubCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public Guid? OwnerId { get; set; }
    public Guid? AssignedManagerId { get; set; }

    [Range(1, 150)]
    public int TotalFloors { get; set; } = 1;

    [Range(0, 1000000)]
    public decimal TotalSquareMeters { get; set; }

    [MaxLength(50)]
    public string ConstructionStatus { get; set; } = "Completed";

    [MaxLength(1000)]
    public string? FinishingNotes { get; set; }
}
