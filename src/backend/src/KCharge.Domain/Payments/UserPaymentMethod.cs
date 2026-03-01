using System;
using KCharge.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KCharge.Payments;

/// <summary>
/// Represents a saved payment method for a user.
/// </summary>
public class UserPaymentMethod : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the user.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Payment gateway for this method.
    /// </summary>
    public PaymentGateway Gateway { get; private set; }

    /// <summary>
    /// User-friendly display name.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this is the default payment method.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Secure token reference from the payment gateway.
    /// </summary>
    public string TokenReference { get; private set; } = string.Empty;

    /// <summary>
    /// Last 4 digits for display (if applicable).
    /// </summary>
    public string? LastFourDigits { get; private set; }

    /// <summary>
    /// Whether this payment method is active.
    /// </summary>
    public bool IsActive { get; private set; }

    protected UserPaymentMethod()
    {
        // Required by EF Core
    }

    public UserPaymentMethod(
        Guid id,
        Guid userId,
        PaymentGateway gateway,
        string displayName,
        string tokenReference,
        string? lastFourDigits = null)
        : base(id)
    {
        UserId = userId;
        Gateway = gateway;
        DisplayName = displayName;
        TokenReference = tokenReference;
        LastFourDigits = lastFourDigits;
        IsDefault = false;
        IsActive = true;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
    }
}
