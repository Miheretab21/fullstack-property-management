using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Application.Common;

public interface IApplicationDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<Unit> Units { get; }
    DbSet<Lease> Leases { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<MaintenanceRequest> MaintenanceRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
