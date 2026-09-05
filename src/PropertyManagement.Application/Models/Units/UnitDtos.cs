using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Units;

public class UnitDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string SubCity { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public decimal SquareMeters { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public decimal RentAmount { get; set; }
    public UnitStatus Status { get; set; }
    public string? FinishingNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateUnitDto
{
    [Required]
    public Guid PropertyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(-5, 150)]
    public int FloorNumber { get; set; } = 1;

    [Range(0, 50000)]
    public decimal SquareMeters { get; set; }

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(0, 20)]
    public decimal Bathrooms { get; set; }

    [Range(0, 1000000)]
    public decimal RentAmount { get; set; }

    public UnitStatus Status { get; set; } = UnitStatus.Vacant;

    [MaxLength(1000)]
    public string? FinishingNotes { get; set; }
}

public class UpdateUnitDto
{
    [Required]
    [MaxLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(-5, 150)]
    public int FloorNumber { get; set; } = 1;

    [Range(0, 50000)]
    public decimal SquareMeters { get; set; }

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(0, 20)]
    public decimal Bathrooms { get; set; }

    [Range(0, 1000000)]
    public decimal RentAmount { get; set; }

    public UnitStatus Status { get; set; }

    [MaxLength(1000)]
    public string? FinishingNotes { get; set; }
}

public class UpdateUnitStatusDto
{
    [Required]
    public UnitStatus Status { get; set; }
}
