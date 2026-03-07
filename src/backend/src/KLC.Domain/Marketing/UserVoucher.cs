using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Marketing;

/// <summary>
/// Maps a voucher to a user (tracks redemption).
/// </summary>
public class UserVoucher : CreationAuditedEntity<Guid>, ISoftDelete
{
    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Reference to the AppUser.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Reference to the Voucher.
    /// </summary>
    public Guid VoucherId { get; private set; }

    /// <summary>
    /// Whether this voucher has been used by the user.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When the voucher was used.
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    protected UserVoucher()
    {
        // Required by EF Core
    }

    public UserVoucher(Guid id, Guid userId, Guid voucherId)
        : base(id)
    {
        UserId = userId;
        VoucherId = voucherId;
        IsUsed = false;
    }

    public void MarkUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}
