using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.ChargingStations;

namespace KLC.EntityFrameworkCore.Configurations;

public class ChargingStationConfiguration : IEntityTypeConfiguration<ChargingStation>
{
    public void Configure(EntityTypeBuilder<ChargingStation> builder)
    {
        builder.ToTable("klc_charging_stations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChargePointId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Vendor)
            .HasMaxLength(100);

        builder.Property(x => x.Model)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .HasDefaultValue("Unavailable");

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(100);

        builder.Property(x => x.FirmwareVersion)
            .HasMaxLength(100);

        builder.Property(x => x.StationGroupId)
            .HasMaxLength(100);

        builder.Property(x => x.LastHeartbeat)
            .IsRequired(false);

        builder.Property(x => x.IsOnline)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(x => x.ChargePointId)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.StationGroupId);

        // Relationships
        builder.HasMany(x => x.Connectors)
            .WithOne(x => x.ChargingStation)
            .HasForeignKey(x => x.ChargingStationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit properties
        builder.Property(x => x.CreationTime).IsRequired();
        builder.Property(x => x.CreatorId).IsRequired(false);
        builder.Property(x => x.LastModificationTime).IsRequired(false);
        builder.Property(x => x.LastModifierId).IsRequired(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletionTime).IsRequired(false);
        builder.Property(x => x.DeleterId).IsRequired(false);
    }
}
