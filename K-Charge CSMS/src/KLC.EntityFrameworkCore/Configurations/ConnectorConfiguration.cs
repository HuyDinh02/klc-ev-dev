using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.ChargingStations;

namespace KLC.EntityFrameworkCore.Configurations;

public class ConnectorConfiguration : IEntityTypeConfiguration<Connector>
{
    public void Configure(EntityTypeBuilder<Connector> builder)
    {
        builder.ToTable("klc_connectors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConnectorId)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(50);

        builder.Property(x => x.MaxPowerKw)
            .IsRequired();

        builder.Property(x => x.ChargingStationId)
            .IsRequired();

        // Composite unique index
        builder.HasIndex(x => new { x.ChargingStationId, x.ConnectorId })
            .IsUnique();

        // Index on status
        builder.HasIndex(x => x.Status);

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
