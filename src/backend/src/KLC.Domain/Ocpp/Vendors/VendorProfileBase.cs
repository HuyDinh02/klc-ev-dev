using System;
using System.Globalization;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Base implementation with standard OCPP 1.6J behavior.
/// Vendor-specific profiles override methods where behavior diverges.
/// </summary>
public abstract class VendorProfileBase : IVendorProfile
{
    protected readonly ILogger Logger;

    protected VendorProfileBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract VendorProfileType ProfileType { get; }
    public virtual bool MeterValuesMayOmitTransactionId => false;
    public virtual int HeartbeatIntervalSeconds => 60;
    public virtual bool MayRetryStartTransaction => false;
    public virtual bool OmitNullFieldsInResponse => false;
    public virtual TimeZoneInfo? ResponseTimezone => null;
    public abstract bool MatchesVendor(string? chargePointVendor, string? chargePointModel);

    public virtual decimal NormalizeEnergyToWh(string rawValue, string? unit, string? measurand)
    {
        if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            Logger.LogWarning("Failed to parse energy value '{RawValue}', returning 0", rawValue);
            return 0;
        }

        var normalizedUnit = (unit ?? "").Trim().ToUpperInvariant();

        return normalizedUnit switch
        {
            "KWH" => value * 1000m, // kWh → Wh
            "WH" => value,
            "" => InferEnergyUnit(value, measurand),
            _ => InferEnergyUnit(value, measurand)
        };
    }

    public virtual decimal NormalizePowerToW(string rawValue, string? unit)
    {
        if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return 0;

        var normalizedUnit = (unit ?? "").Trim().ToUpperInvariant();
        return normalizedUnit switch
        {
            "KW" => value * 1000m,
            "W" => value,
            "" => value > 500m ? value : value * 1000m, // If < 500, likely kW
            _ => value
        };
    }

    public virtual DateTime? ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return null;

        // Standard ISO 8601 formats
        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.fffK",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss"
        ];

        if (DateTime.TryParseExact(timestamp, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
            return result;

        // Fallback: try general parse
        if (DateTime.TryParse(timestamp, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result))
        {
            Logger.LogWarning("Parsed timestamp '{Timestamp}' with fallback parser", timestamp);
            return result;
        }

        Logger.LogWarning("Failed to parse timestamp '{Timestamp}'", timestamp);
        return null;
    }

    /// <summary>
    /// Infers energy unit when the charger doesn't send one.
    /// Values > 100 are likely Wh; values ≤ 100 are likely kWh.
    /// </summary>
    protected decimal InferEnergyUnit(decimal value, string? measurand)
    {
        if (value <= 0) return value;

        // For cumulative energy register: values > 100 are almost certainly Wh
        if (value > 100m)
        {
            Logger.LogDebug("Inferred Wh for energy value {Value} (measurand: {Measurand})",
                value, measurand ?? "unknown");
            return value;
        }

        // Small values likely kWh
        Logger.LogWarning("Inferred kWh for small energy value {Value} — converting to Wh. " +
                          "Measurand: {Measurand}. Consider configuring vendor profile.",
            value, measurand ?? "unknown");
        return value * 1000m;
    }
}
