using System;
using KLC.Enums;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Abstracts vendor-specific OCPP 1.6J behavior differences.
/// Each charger vendor may report meter values in different units,
/// use non-standard timestamp formats, or omit required fields.
/// </summary>
public interface IVendorProfile
{
    VendorProfileType ProfileType { get; }

    /// <summary>
    /// Normalizes an energy meter reading to Wh.
    /// Some vendors report in kWh, some in Wh; some omit the unit field entirely.
    /// </summary>
    /// <param name="rawValue">The raw string value from SampledValue.value</param>
    /// <param name="unit">The unit string from SampledValue.unit (may be null)</param>
    /// <param name="measurand">The measurand string (e.g. "Energy.Active.Import.Register")</param>
    /// <returns>Value normalized to Wh</returns>
    decimal NormalizeEnergyToWh(string rawValue, string? unit, string? measurand);

    /// <summary>
    /// Normalizes a power reading to W (watts).
    /// </summary>
    decimal NormalizePowerToW(string rawValue, string? unit);

    /// <summary>
    /// Parses a timestamp from the charger, handling vendor-specific formats.
    /// Returns UTC DateTime.
    /// </summary>
    DateTime? ParseTimestamp(string? timestamp);

    /// <summary>
    /// Whether this vendor omits transactionId in MeterValues messages.
    /// If true, the handler should resolve the transaction by cpId + connectorId.
    /// </summary>
    bool MeterValuesMayOmitTransactionId { get; }

    /// <summary>
    /// The default heartbeat interval (seconds) to send in BootNotification response.
    /// </summary>
    int HeartbeatIntervalSeconds { get; }

    /// <summary>
    /// Whether this vendor is known to send duplicate StartTransaction
    /// requests on reconnection.
    /// </summary>
    bool MayRetryStartTransaction { get; }

    /// <summary>
    /// Returns true if the given vendor/model strings match this profile.
    /// Used for auto-detection during BootNotification.
    /// </summary>
    bool MatchesVendor(string? chargePointVendor, string? chargePointModel);

    /// <summary>
    /// Whether to omit null fields in OCPP JSON responses.
    /// Some chargers (e.g., ChargeCore B0207) report "data mismatch"
    /// when optional fields like expiryDate/parentIdTag are present as null.
    /// Default: false. Currently applied globally via serializer options,
    /// but this flag documents the vendor requirement.
    /// </summary>
    bool OmitNullFieldsInResponse => false;

    /// <summary>
    /// Timezone for Heartbeat/BootNotification currentTime responses.
    /// Some chargers sync their display clock from this timestamp.
    /// Return null to use UTC (OCPP standard). Return a specific timezone
    /// (e.g., Asia/Ho_Chi_Minh) for chargers that display the CPO time directly.
    /// </summary>
    TimeZoneInfo? ResponseTimezone => null;
}
