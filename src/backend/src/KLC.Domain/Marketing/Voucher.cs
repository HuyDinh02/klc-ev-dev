using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Marketing;

/// <summary>
/// Represents a voucher/coupon code for discounts or credits.
/// </summary>
public class Voucher : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Unique voucher code.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Type of voucher discount.
    /// </summary>
    public VoucherType Type { get; private set; }

    /// <summary>
    /// Discount value (VND for FixedAmount, percent for Percentage).
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>
    /// Minimum order/top-up amount to use this voucher.
    /// </summary>
    public decimal? MinOrderAmount { get; private set; }

    /// <summary>
    /// Maximum discount amount (for Percentage type).
    /// </summary>
    public decimal? MaxDiscountAmount { get; private set; }

    /// <summary>
    /// When the voucher expires.
    /// </summary>
    public DateTime ExpiryDate { get; private set; }

    /// <summary>
    /// Total quantity available.
    /// </summary>
    public int TotalQuantity { get; private set; }

    /// <summary>
    /// Quantity already used.
    /// </summary>
    public int UsedQuantity { get; private set; }

    /// <summary>
    /// Whether the voucher is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Description of the voucher for display.
    /// </summary>
    public string? Description { get; private set; }

    protected Voucher()
    {
        // Required by EF Core
    }

    public Voucher(
        Guid id,
        string code,
        VoucherType type,
        decimal value,
        DateTime expiryDate,
        int totalQuantity,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null,
        string? description = null)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), maxLength: 50);
        Type = type;
        Value = value;
        ExpiryDate = expiryDate;
        TotalQuantity = totalQuantity;
        UsedQuantity = 0;
        IsActive = true;
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        Description = description;
    }

    public bool IsValid()
    {
        return IsActive && ExpiryDate > DateTime.UtcNow && UsedQuantity < TotalQuantity;
    }

    public void IncrementUsage()
    {
        if (!IsValid())
            throw new BusinessException(KLCDomainErrorCodes.Voucher.NotValid);
        UsedQuantity++;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Update(string? description, DateTime? expiryDate, int? totalQuantity, bool? isActive)
    {
        if (description != null) Description = description;
        if (expiryDate.HasValue) ExpiryDate = expiryDate.Value;
        if (totalQuantity.HasValue) TotalQuantity = totalQuantity.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
    }
}
