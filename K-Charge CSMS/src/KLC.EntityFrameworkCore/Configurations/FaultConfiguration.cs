using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.Faults;

namespace KLC.EntityFrameworkCore.Configurations;

public class FaultConfiguration : IEntityTypeConfiguration<Fault>
{
    public void Configure(EntityTypeBuilder<Fault> builder)
    {
        builder.ToTable("klc_faults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChargePointId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ConnectorId)
            .IsRequired(false);

        builder.Property(x => x.ErrorCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.FaultType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Severity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ResolvedAt)
            .IsRequired(false);

        builder.Property(x => x.Resolution)
            .HasMaxLength(500);

        builder.Property(x => x.TransactionId)
            .IsRequired(false);

        // Composite index
        builder.HasIndex(x => new { x.ChargePointId, x.CreationTime })
            .IsDescending(false, true);

        // Filtered index for unresolved faults
        builder.HasIndex(x => new { x.ChargePointId, x.CreationTime })
            .IsDescending(false, true)
            .HasFilter("[ResolvedAt] IS NULL")
            .HasName("idx_klc_faults_unresolved");

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
