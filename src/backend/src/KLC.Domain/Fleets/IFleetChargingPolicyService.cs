using System;
using System.Threading.Tasks;

namespace KLC.Fleets;

/// <summary>
/// Domain service interface for validating fleet charging policies.
/// </summary>
public interface IFleetChargingPolicyService
{
    /// <summary>
    /// Validates whether a vehicle is allowed to charge at a given station
    /// based on its fleet's charging policy.
    /// </summary>
    Task<FleetChargingValidationResult> ValidateChargingAsync(Guid vehicleId, Guid stationId);
}

/// <summary>
/// Result of a fleet charging policy validation.
/// </summary>
public record FleetChargingValidationResult(bool Allowed, string? DenialReason = null);
