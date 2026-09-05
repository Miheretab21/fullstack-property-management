using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.RentAmount)
            .HasPrecision(18, 2);

        builder.Property(u => u.Bathrooms)
            .HasPrecision(4, 1);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.FloorNumber)
            .HasDefaultValue(1);

        builder.Property(u => u.SquareMeters)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(u => u.FinishingNotes)
            .HasMaxLength(1000);

        builder.HasIndex(u => new { u.PropertyId, u.UnitNumber })
            .IsUnique();

        builder.HasMany(u => u.Leases)
            .WithOne(l => l.Unit)
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.MaintenanceRequests)
            .WithOne(m => m.Unit)
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
