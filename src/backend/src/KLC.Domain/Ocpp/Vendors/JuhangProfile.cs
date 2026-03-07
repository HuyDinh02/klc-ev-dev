using System;
using System.Globalization;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Vendor profile for JUHANG chargers (Chinese manufacturer, widely deployed in Vietnam).
///
/// Known quirks:
/// - Reports energy in Wh (standard) but sometimes omits unit field
/// - May omit transactionId in MeterValues messages
/// - Timestamp format may use space instead of 'T' separator
/// - May send non-standard vendor error codes in StatusNotification
/// - Power values reported in W (standard)
/// - Heartbeat interval preference: 60 seconds
///
/// TODO: Validate against actual JUHANG OCPP log samples from field deployment.
/// </summary>
public class JuhangProfile : VendorProfileBase
{
    public JuhangProfile(ILogger<JuhangProfile> logger) : base(logger) { }

    public override VendorProfileType ProfileType => VendorProfileType.Juhang;
    public override bool MeterValuesMayOmitTransactionId => true;
    public override int HeartbeatIntervalSeconds => 60;
    public override bool MayRetryStartTransaction => true;

    public override bool MatchesVendor(string? chargePointVendor, string? chargePointModel)
    {
        if (string.IsNullOrWhiteSpace(chargePointVendor))
            return false;

        var vendor = chargePointVendor.Trim();
        return vendor.Contains("JUHANG", StringComparison.OrdinalIgnoreCase)
               || vendor.Contains("JuHang", StringComparison.OrdinalIgnoreCase)
               || vendor.Contains("Ju Hang", StringComparison.OrdinalIgnoreCase);
    }

    public override DateTime? ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return null;

        // JUHANG sometimes uses "yyyy-MM-dd HH:mm:ss" without timezone
        // Treat as UTC when no timezone info present
        string[] juhangFormats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.fffK"
        ];

        if (DateTime.TryParseExact(timestamp, juhangFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
            return result;

        // Fallback to base parser
        return base.ParseTimestamp(timestamp);
    }

    public override decimal NormalizeEnergyToWh(string rawValue, string? unit, string? measurand)
    {
        if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            Logger.LogWarning("[Juhang] Failed to parse energy '{RawValue}'", rawValue);
            return 0;
        }

        var normalizedUnit = (unit ?? "").Trim().ToUpperInvariant();

        // JUHANG typically reports in Wh; when unit is missing, infer from magnitude
        if (normalizedUnit == "WH" || string.IsNullOrEmpty(normalizedUnit))
        {
            // If the value is very small (< 100), it's probably kWh with missing unit
            if (string.IsNullOrEmpty(normalizedUnit) && value > 0 && value < 100)
            {
                Logger.LogWarning("[Juhang] Small energy value {Value} with no unit — assuming kWh", value);
                return value * 1000m;
            }
            return value; // Already Wh
        }

        if (normalizedUnit == "KWH")
            return value * 1000m;

        return InferEnergyUnit(value, measurand);
    }
}
