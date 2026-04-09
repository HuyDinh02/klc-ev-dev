using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Driver;

/// <summary>
/// Extension methods to set ABP audit fields when using raw DbContext in the BFF.
/// ABP's audit interceptor requires ICurrentUser which is not configured in Minimal API.
/// </summary>
public static class DbContextAuditExtensions
{
    /// <summary>
    /// Sets CreationTime and LastModificationTime on a new entity before adding to DbContext.
    /// Call this before AddAsync() for any FullAuditedAggregateRoot entity.
    /// </summary>
    public static void SetAuditFields<T>(this DbContext dbContext, T entity, Guid? creatorId = null)
        where T : class
    {
        var now = DateTime.UtcNow;
        var entry = dbContext.Entry(entity);

        if (entry.Properties.Any(p => p.Metadata.Name == "CreationTime"))
            entry.Property("CreationTime").CurrentValue = now;

        if (entry.Properties.Any(p => p.Metadata.Name == "LastModificationTime"))
            entry.Property("LastModificationTime").CurrentValue = now;

        if (creatorId.HasValue && entry.Properties.Any(p => p.Metadata.Name == "CreatorId"))
            entry.Property("CreatorId").CurrentValue = creatorId;
    }
}
