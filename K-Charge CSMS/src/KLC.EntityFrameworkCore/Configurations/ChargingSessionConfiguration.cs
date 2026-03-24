using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.ChargingSessions;

namespace KLC.EntityFrameworkCore.Configurations;

public class ChargingSessionConfiguration : IEntityTypeConfiguration<ChargingSession>
{
    public void Configure(EntityTypeBuilder<ChargingSession> builder)
    {
        builder.ToTable("klc_charging_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionId)
            .IsRequired()
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ChargePointId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ConnectorId)
            .IsRequired();

        builder.Property(x => x.IdTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.StartTimestamp)
            .IsRequired();

        builder.Property(x => x.StopTimestamp)
            .IsRequired(false);

        builder.Property(x => x.MeterStart)
            .IsRequired();

        builder.Property(x => x.MeterStop)
            .IsRequired(false);

        builder.Property(x => x.ReservationId)
            .HasMaxLength(100);

        builder.Property(x => x.StopReason)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.TransactionId)
            .IsUnique();

        builder.HasIndex(x => x.ChargePointId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.StartTimestamp)
            .IsDescending();

        // Relationships
        builder.HasMany(x => x.MeterValues)
            .WithOne(x => x.ChargingSession)
            .HasForeignKey(x => x.ChargingSessionId)
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
