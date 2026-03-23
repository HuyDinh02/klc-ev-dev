using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KLC.Authorization;

namespace KLC.EntityFrameworkCore.Configurations;

public class IdTagConfiguration : IEntityTypeConfiguration<IdTag>
{
    public void Configure(EntityTypeBuilder<IdTag> builder)
    {
        builder.ToTable("klc_id_tags");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TagId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ParentTagId)
            .HasMaxLength(50);

        builder.Property(x => x.ExpiryDate)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.UserId)
            .HasMaxLength(36);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        // Unique index
        builder.HasIndex(x => x.TagId)
            .IsUnique();

        // Index on active status
        builder.HasIndex(x => x.IsActive);

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
