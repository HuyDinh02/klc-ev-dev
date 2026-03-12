using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.PowerSharing;

/// <summary>
/// Records power usage snapshots for a power sharing group.
/// Used for analytics, billing, and dynamic load balancing decisions.
/// </summary>
public class SiteLoadProfile : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the power sharing group.
    /// </summary>
    public Guid PowerSharingGroupId { get; private set; }

    /// <summary>
    /// Timestamp of this load measurement.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Total power being consumed by all active sessions in kW.
    /// </summary>
    public decimal TotalLoadKw { get; private set; }

    /// <summary>
    /// Available (unused) power capacity in kW.
    /// </summary>
    public decimal AvailableCapacityKw { get; private set; }

    /// <summary>
    /// Number of active charging sessions at this point.
    /// </summary>
    public int ActiveSessionCount { get; private set; }

    /// <summary>
    /// Number of connectors in the group at this point.
    /// </summary>
    public int TotalConnectorCount { get; private set; }

    /// <summary>
    /// Peak power recorded in this measurement window in kW.
    /// </summary>
    public decimal PeakLoadKw { get; private set; }

    protected SiteLoadProfile()
    {
        // Required by EF Core
    }

    public SiteLoadProfile(
        Guid id,
        Guid powerSharingGroupId,
        DateTime timestamp,
        decimal totalLoadKw,
        decimal availableCapacityKw,
        int activeSessionCount,
        int totalConnectorCount,
        decimal peakLoadKw)
        : base(id)
    {
        PowerSharingGroupId = powerSharingGroupId;
        Timestamp = timestamp;
        TotalLoadKw = totalLoadKw;
        AvailableCapacityKw = availableCapacityKw;
        ActiveSessionCount = activeSessionCount;
        TotalConnectorCount = totalConnectorCount;
        PeakLoadKw = peakLoadKw;
    }
}
