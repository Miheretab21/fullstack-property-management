using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.SubCity)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(p => p.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.Property(p => p.AssignedManagerId)
            .IsRequired(false);

        builder.Property(p => p.TotalFloors)
            .HasDefaultValue(1);

        builder.Property(p => p.TotalSquareMeters)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.ConstructionStatus)
            .HasMaxLength(50)
            .HasDefaultValue("Completed");

        builder.Property(p => p.FinishingNotes)
            .HasMaxLength(1000);

        builder.HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
