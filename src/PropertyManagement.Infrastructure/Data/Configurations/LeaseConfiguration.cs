using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.ToTable("Leases");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.MonthlyRent)
            .HasPrecision(18, 2);

        builder.Property(l => l.SecurityDeposit)
            .HasPrecision(18, 2);

        builder.Property(l => l.StartDate)
            .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(l => l.EndDate)
            .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasMany(l => l.Transactions)
            .WithOne(t => t.Lease)
            .HasForeignKey(t => t.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
