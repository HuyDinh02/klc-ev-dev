using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KLC.PowerSharing;

/// <summary>
/// Domain service for power sharing calculations and OCPP SetChargingProfile dispatch.
/// </summary>
public interface IPowerSharingService
{
    /// <summary>
    /// Recalculates power allocation for all active members in a group
    /// and returns the new allocation per connector.
    /// </summary>
    Task<IReadOnlyList<PowerAllocation>> RecalculateAllocationsAsync(Guid powerSharingGroupId);

    /// <summary>
    /// Gets the current power allocation for a specific connector.
    /// Returns null if the connector is not in any power sharing group.
    /// </summary>
    Task<decimal?> GetAllocatedPowerAsync(Guid connectorId);

    /// <summary>
    /// Records a load profile snapshot for a power sharing group.
    /// </summary>
    Task RecordLoadProfileAsync(Guid powerSharingGroupId);
}

/// <summary>
/// Result of power allocation calculation for a single connector.
/// </summary>
public record PowerAllocation(
    Guid ConnectorId,
    Guid StationId,
    decimal AllocatedPowerKw,
    decimal MaxPowerKw);
