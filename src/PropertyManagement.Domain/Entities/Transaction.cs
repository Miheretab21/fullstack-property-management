using PropertyManagement.Domain.Common;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid LeaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    // Navigation property
    public Lease? Lease { get; set; }
}
