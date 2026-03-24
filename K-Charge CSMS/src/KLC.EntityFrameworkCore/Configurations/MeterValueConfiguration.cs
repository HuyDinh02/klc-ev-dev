using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.ChargingSessions;

namespace KLC.EntityFrameworkCore.Configurations;

public class MeterValueConfiguration : IEntityTypeConfiguration<MeterValue>
{
    public void Configure(EntityTypeBuilder<MeterValue> builder)
    {
        builder.ToTable("klc_meter_values");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChargingSessionId)
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Value)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Measurand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Unit)
            .HasMaxLength(20);

        builder.Property(x => x.Context)
            .HasMaxLength(50);

        // Composite index
        builder.HasIndex(x => new { x.ChargingSessionId, x.Timestamp });

        // Index on timestamp
        builder.HasIndex(x => x.Timestamp)
            .IsDescending();

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
