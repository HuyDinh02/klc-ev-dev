using System;
using System.Globalization;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Vendor profile for Chargecore Global chargers (Australian/APAC manufacturer).
///
/// Known quirks:
/// - Reports energy in kWh (not Wh) with unit field sometimes missing
/// - Firmware versions may include build metadata in vendor string
/// - Heartbeat interval preference: 60 seconds
/// - May send StatusNotification before BootNotification on power cycle
///
/// TODO: Validate against actual Chargecore Global OCPP log samples.
/// </summary>
public class ChargecoreGlobalProfile : VendorProfileBase
{
    public ChargecoreGlobalProfile(ILogger<ChargecoreGlobalProfile> logger) : base(logger) { }

    public override VendorProfileType ProfileType => VendorProfileType.ChargecoreGlobal;
    public override int HeartbeatIntervalSeconds => 60;
    public override bool MayRetryStartTransaction => true;

    public override bool MatchesVendor(string? chargePointVendor, string? chargePointModel)
    {
        if (string.IsNullOrWhiteSpace(chargePointVendor))
            return false;

        var vendor = chargePointVendor.Trim();
        return vendor.Contains("Chargecore", StringComparison.OrdinalIgnoreCase)
               || vendor.Contains("ChargeCore Global", StringComparison.OrdinalIgnoreCase)
               || vendor.Contains("CCG", StringComparison.OrdinalIgnoreCase);
    }

    public override decimal NormalizeEnergyToWh(string rawValue, string? unit, string? measurand)
    {
        if (!decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            Logger.LogWarning("[ChargecoreGlobal] Failed to parse energy '{RawValue}'", rawValue);
            return 0;
        }

        var normalizedUnit = (unit ?? "").Trim().ToUpperInvariant();

        // Chargecore Global typically reports in kWh even when unit field is empty
        if (string.IsNullOrEmpty(normalizedUnit) || normalizedUnit == "KWH")
        {
            return value * 1000m; // Convert kWh → Wh
        }

        if (normalizedUnit == "WH")
            return value;

        // Fallback with warning
        Logger.LogWarning("[ChargecoreGlobal] Unknown energy unit '{Unit}' for value {Value}, " +
                          "assuming kWh", unit, value);
        return value * 1000m;
    }
}
