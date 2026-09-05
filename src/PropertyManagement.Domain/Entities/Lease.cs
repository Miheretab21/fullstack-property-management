using PropertyManagement.Domain.Common;

namespace PropertyManagement.Domain.Entities;

public class Lease : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Unit? Unit { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
