using System;
using Volo.Abp.Domain.Entities;

namespace KLC.Notifications;

/// <summary>
/// Per-user notification preferences for the mobile app.
/// </summary>
public class NotificationPreference : Entity<Guid>
{
    /// <summary>
    /// Reference to the AppUser.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Notify when charging is complete.
    /// </summary>
    public bool ChargingComplete { get; private set; }

    /// <summary>
    /// Notify on payment/wallet events.
    /// </summary>
    public bool PaymentAlerts { get; private set; }

    /// <summary>
    /// Notify on station faults affecting user's session.
    /// </summary>
    public bool FaultAlerts { get; private set; }

    /// <summary>
    /// Notify about promotions and marketing offers.
    /// </summary>
    public bool Promotions { get; private set; }

    protected NotificationPreference()
    {
        // Required by EF Core
    }

    public NotificationPreference(Guid id, Guid userId)
        : base(id)
    {
        UserId = userId;
        // Default: all notifications enabled
        ChargingComplete = true;
        PaymentAlerts = true;
        FaultAlerts = true;
        Promotions = true;
    }

    public void Update(bool chargingComplete, bool paymentAlerts, bool faultAlerts, bool promotions)
    {
        ChargingComplete = chargingComplete;
        PaymentAlerts = paymentAlerts;
        FaultAlerts = faultAlerts;
        Promotions = promotions;
    }
}
