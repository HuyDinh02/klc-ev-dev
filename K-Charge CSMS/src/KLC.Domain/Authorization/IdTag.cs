using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Authorization;

public class IdTag : AuditedEntity<Guid>
{
    public string TagId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? ParentTagId { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    protected IdTag()
    {
    }

    public IdTag(
        Guid id,
        string tagId,
        Guid? userId = null,
        string? parentTagId = null,
        DateTime? expiryDate = null) : base(id)
    {
        TagId = Check.NotNullOrWhiteSpace(tagId, nameof(tagId));
        UserId = userId;
        ParentTagId = parentTagId;
        ExpiryDate = expiryDate;
        IsBlocked = false;
    }

    public void Block()
    {
        IsBlocked = true;
    }

    public void Unblock()
    {
        IsBlocked = false;
    }

    public void SetExpiry(DateTime expiryDate)
    {
        ExpiryDate = expiryDate;
    }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;

    public bool IsValid => !IsBlocked && !IsExpired;
}
