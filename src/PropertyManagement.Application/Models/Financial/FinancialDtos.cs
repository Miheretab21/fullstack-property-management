using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Application.Models.Financial;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateRentChargeDto
{
    [Required]
    public Guid LeaseId { get; set; }

    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    public DateTime? DueDate { get; set; }
}

public class RecordPaymentDto
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = "Check";

    [MaxLength(100)]
    public string? CheckNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    public DateTime? PaymentDate { get; set; }
}

public class CreateDirectPaymentDto
{
    [Required]
    public Guid LeaseId { get; set; }

    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = "Check";

    [MaxLength(100)]
    public string? CheckNumber { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    public bool IsCleared { get; set; } = true; // Money came in

    public DateTime? PaymentDate { get; set; }
}

public class TenantLedgerSummaryDto
{
    public Guid LeaseId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance => TotalBilled - TotalPaid;
    public List<TransactionDto> Transactions { get; set; } = new();
}
