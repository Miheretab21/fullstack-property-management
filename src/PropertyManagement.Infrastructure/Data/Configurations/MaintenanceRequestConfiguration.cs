using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("MaintenanceRequests");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.PhotoUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(m => m.AssignedTechnician)
            .HasMaxLength(200);

        builder.Property(m => m.CreatedAtUtc)
            .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(m => m.ResolvedAtUtc)
            .HasConversion(v => v != null ? v.Value.ToUniversalTime() : (DateTime?)null, v => v != null ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
    }
}
