using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Application.Models.Leases;

public class LeaseDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateLeaseDto
{
    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, 1000000)]
    public decimal MonthlyRent { get; set; }

    [Range(0, 1000000)]
    public decimal SecurityDeposit { get; set; }

    public bool RecordInitialPayment { get; set; } = false;

    [MaxLength(100)]
    public string? PaymentMethod { get; set; } = "Check";

    [MaxLength(100)]
    public string? CheckNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [Range(0, 1000000)]
    public decimal? InitialPaymentAmount { get; set; }

    public bool IsPaymentCleared { get; set; } = true;
}

public class UpdateLeaseDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, 1000000)]
    public decimal MonthlyRent { get; set; }

    [Range(0, 1000000)]
    public decimal SecurityDeposit { get; set; }

    public bool IsActive { get; set; }
}
