using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.PowerSharing;

/// <summary>
/// Represents a connector that is a member of a power sharing group.
/// </summary>
public class PowerSharingGroupMember : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the power sharing group.
    /// </summary>
    public Guid PowerSharingGroupId { get; private set; }

    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Reference to the specific connector.
    /// </summary>
    public Guid ConnectorId { get; private set; }

    /// <summary>
    /// Priority for power allocation (higher = more priority).
    /// Used in Dynamic distribution strategy.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Current allocated power in kW. Updated by the power sharing service.
    /// </summary>
    public decimal AllocatedPowerKw { get; private set; }

    /// <summary>
    /// Navigation property to parent group.
    /// </summary>
    public PowerSharingGroup? Group { get; private set; }

    protected PowerSharingGroupMember()
    {
        // Required by EF Core
    }

    internal PowerSharingGroupMember(
        Guid id,
        Guid powerSharingGroupId,
        Guid stationId,
        Guid connectorId,
        int priority = 0)
        : base(id)
    {
        PowerSharingGroupId = powerSharingGroupId;
        StationId = stationId;
        ConnectorId = connectorId;
        Priority = priority;
        AllocatedPowerKw = 0;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
    }

    public void UpdateAllocatedPower(decimal powerKw)
    {
        AllocatedPowerKw = Math.Max(0, powerKw);
    }

    internal void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletionTime = DateTime.UtcNow;
    }
}
