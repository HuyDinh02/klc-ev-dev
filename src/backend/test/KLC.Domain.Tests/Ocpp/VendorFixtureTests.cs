using System;
using System.Collections.Generic;
using KLC.Enums;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.Ocpp;

/// <summary>
/// Fixture-based integration tests simulating realistic OCPP 1.6J message patterns
/// from each vendor's chargers. Uses field-representative data values, message structures,
/// and vendor-specific quirks observed in production deployments.
/// </summary>
public class VendorFixtureTests
{
    private readonly GenericProfile _generic;
    private readonly ChargecoreGlobalProfile _chargecore;
    private readonly JuhangProfile _juhang;
    private readonly VendorProfileFactory _factory;

    public VendorFixtureTests()
    {
        _generic = new GenericProfile(NullLogger<GenericProfile>.Instance);
        _chargecore = new ChargecoreGlobalProfile(NullLogger<ChargecoreGlobalProfile>.Instance);
        _juhang = new JuhangProfile(NullLogger<JuhangProfile>.Instance);

        var profiles = new List<IVendorProfile> { _generic, _chargecore, _juhang };
        _factory = new VendorProfileFactory(profiles, NullLogger<VendorProfileFactory>.Instance);
    }

    // =====================================================================
    // Chargecore Global Fixtures — Realistic charger log data
    // =====================================================================

    #region Chargecore — BootNotification Fixtures

    [Theory]
    [InlineData("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU")]
    [InlineData("ChargeCore Global v3.0.0-rc.2", "CCG-DC50-VN")]
    [InlineData("Chargecore v1.8.4", "AC-7")]
    [InlineData("CCG Firmware 2.0.0", "DC-120-DUAL")]
    public void Chargecore_BootNotification_DetectedFromFirmwareVendorString(
        string vendorString, string model)
    {
        var profile = _factory.Detect(vendorString, model);
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    [Fact]
    public void Chargecore_BootNotification_HeartbeatInterval_60s()
    {
        var profile = _factory.Detect("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU");
        profile.HeartbeatIntervalSeconds.ShouldBe(60);
    }

    [Fact]
    public void Chargecore_BootNotification_MayRetryStartTransaction()
    {
        // Chargecore chargers are known to retry StartTransaction on reconnection
        var profile = _factory.Detect("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU");
        profile.MayRetryStartTransaction.ShouldBeTrue();
    }

    #endregion

    #region Chargecore — MeterValues Fixtures (Energy in kWh)

    [Theory]
    [InlineData("0.000", null, 0)]           // Session start: 0 kWh
    [InlineData("1.234", null, 1234)]        // Early session: 1.234 kWh → 1234 Wh
    [InlineData("7.500", null, 7500)]        // Mid-session: 7.5 kWh → 7500 Wh
    [InlineData("22.150", null, 22150)]      // Near end: 22.15 kWh → 22150 Wh
    [InlineData("45.678", null, 45678)]      // Long DC session: 45.678 kWh → 45678 Wh
    [InlineData("99.999", null, 99999)]      // Large session near 100 kWh
    public void Chargecore_MeterValues_Energy_NoUnit_ConvertsKWhToWh(
        string rawKwh, string? unit, decimal expectedWh)
    {
        // Chargecore always sends energy in kWh, often without the unit field
        _chargecore.NormalizeEnergyToWh(rawKwh, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("0.000", "kWh", 0)]
    [InlineData("12.345", "kWh", 12345)]
    [InlineData("55.000", "kWh", 55000)]
    public void Chargecore_MeterValues_Energy_ExplicitKWh_ConvertsToWh(
        string rawKwh, string unit, decimal expectedWh)
    {
        _chargecore.NormalizeEnergyToWh(rawKwh, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Fact]
    public void Chargecore_MeterValues_Power_InKW()
    {
        // Chargecore reports power in kW for AC chargers
        _chargecore.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
        _chargecore.NormalizePowerToW("22.0", "kW").ShouldBe(22000m);
    }

    [Fact]
    public void Chargecore_MeterValues_Power_DC_InKW()
    {
        // DC charger power readings
        _chargecore.NormalizePowerToW("50.0", "kW").ShouldBe(50000m);
        _chargecore.NormalizePowerToW("120.5", "kW").ShouldBe(120500m);
    }

    #endregion

    #region Chargecore — StartTransaction/StopTransaction Fixtures

    [Fact]
    public void Chargecore_StartTransaction_MeterStart_0kWh()
    {
        // Chargecore sends meterStart=0 in kWh, which we normalize to Wh
        var meterStartWh = _chargecore.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        meterStartWh.ShouldBe(0m);
    }

    [Fact]
    public void Chargecore_StopTransaction_MeterStop_RealisticSession()
    {
        // Simulates a 30-minute AC charge session:
        // Start: 0 kWh, Stop: 11.2 kWh (approx 7.2 kW * 1.55h)
        var meterStartWh = _chargecore.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        var meterStopWh = _chargecore.NormalizeEnergyToWh("11.2", null, "Energy.Active.Import.Register");

        var energyDeliveredWh = meterStopWh - meterStartWh;
        energyDeliveredWh.ShouldBe(11200m); // 11.2 kWh = 11200 Wh
    }

    [Fact]
    public void Chargecore_StopTransaction_DCFastCharge_Session()
    {
        // DC fast charge: 20 minutes at ~50 kW
        // Start: 0, Stop: 16.67 kWh
        var meterStartWh = _chargecore.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        var meterStopWh = _chargecore.NormalizeEnergyToWh("16.67", null, "Energy.Active.Import.Register");

        var energyDeliveredWh = meterStopWh - meterStartWh;
        energyDeliveredWh.ShouldBe(16670m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_MultipliedBy1000_KWhToWh()
    {
        // Core contract: Chargecore's no-unit values are ALWAYS multiplied by 1000
        var values = new[] { "0.5", "1.0", "5.5", "10.0", "25.3", "50.0", "75.8" };
        foreach (var val in values)
        {
            var parsed = decimal.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
            _chargecore.NormalizeEnergyToWh(val, null, null).ShouldBe(parsed * 1000m,
                $"Value {val} should be multiplied by 1000 (kWh->Wh)");
        }
    }

    #endregion

    #region Chargecore — StatusNotification Fixtures

    [Fact]
    public void Chargecore_StatusNotification_Timestamp_ISO8601()
    {
        // Chargecore sends standard ISO 8601 timestamps
        var ts = _chargecore.ParseTimestamp("2026-03-09T14:30:00Z");
        ts.ShouldNotBeNull();
        ts!.Value.Year.ShouldBe(2026);
        ts.Value.Month.ShouldBe(3);
        ts.Value.Day.ShouldBe(9);
        ts.Value.Hour.ShouldBe(14);
        ts.Value.Minute.ShouldBe(30);
        ts.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Chargecore_StatusNotification_Timestamp_WithOffset_Vietnam()
    {
        // Chargecore charger in Vietnam sending UTC+7 timestamp
        var ts = _chargecore.ParseTimestamp("2026-03-09T21:30:00+07:00");
        ts.ShouldNotBeNull();
        ts!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        ts.Value.Hour.ShouldBe(14); // 21:30 +07:00 = 14:30 UTC
        ts.Value.Minute.ShouldBe(30);
    }

    [Fact]
    public void Chargecore_MeterValuesMayOmitTransactionId_False()
    {
        // Chargecore always includes transactionId in MeterValues
        _chargecore.MeterValuesMayOmitTransactionId.ShouldBeFalse();
    }

    #endregion

    // =====================================================================
    // JUHANG Fixtures — Realistic charger log data
    // =====================================================================

    #region JUHANG — BootNotification Fixtures

    [Theory]
    [InlineData("JUHANG", "JH-DC120-VN")]
    [InlineData("JUHANG Electric", "DC-60")]
    [InlineData("JuHang Technology Co. Ltd", "JH-AC7-S2")]
    [InlineData("Ju Hang New Energy", "JH-DC240-DUAL")]
    public void Juhang_BootNotification_DetectedFromVendorString(
        string vendorString, string model)
    {
        var profile = _factory.Detect(vendorString, model);
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Juhang_BootNotification_ChineseCharacters_NotMatched()
    {
        // Chinese characters alone are not in the MatchesVendor check;
        // would need "JUHANG" substring to match
        var profile = _factory.Detect("\u5DE8\u822A", "DC-120");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Juhang_BootNotification_ChineseWithJuhang_Matched()
    {
        // If vendor string includes both Chinese and Latin
        var profile = _factory.Detect("\u5DE8\u822A JUHANG", "DC-120");
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Juhang_BootNotification_Properties()
    {
        var profile = _factory.Detect("JUHANG", "JH-DC120-VN");
        profile.HeartbeatIntervalSeconds.ShouldBe(60);
        profile.MayRetryStartTransaction.ShouldBeTrue();
        profile.MeterValuesMayOmitTransactionId.ShouldBeTrue();
    }

    #endregion

    #region JUHANG — MeterValues Fixtures (Energy with Inference)

    [Theory]
    [InlineData("0", null, 0)]             // Session start
    [InlineData("1500", null, 1500)]       // 1500 Wh (>= 100, inferred Wh)
    [InlineData("3200", null, 3200)]       // 3200 Wh
    [InlineData("7500", null, 7500)]       // 7500 Wh
    [InlineData("22000", null, 22000)]     // 22000 Wh = 22 kWh
    public void Juhang_MeterValues_Energy_NoUnit_LargeValues_InferredAsWh(
        string rawValue, string? unit, decimal expectedWh)
    {
        // JUHANG typically sends in Wh; values >= 100 stay as Wh
        _juhang.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("1.5", null, 1500)]      // 1.5 kWh (< 100, inferred kWh)
    [InlineData("7.2", null, 7200)]      // 7.2 kWh
    [InlineData("22.5", null, 22500)]    // 22.5 kWh
    [InlineData("50.0", null, 50000)]    // 50 kWh
    [InlineData("99.9", null, 99900)]    // 99.9 kWh (just below threshold)
    public void Juhang_MeterValues_Energy_NoUnit_SmallValues_InferredAsKWh(
        string rawValue, string? unit, decimal expectedWh)
    {
        // JUHANG: values < 100 with no unit → assumed kWh
        _juhang.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Fact]
    public void Juhang_MeterValues_Energy_ExactlyAt100_Boundary()
    {
        // 100 with no unit: NOT < 100, so treated as Wh (passes through)
        _juhang.NormalizeEnergyToWh("100", null, "Energy.Active.Import.Register")
            .ShouldBe(100m);

        // 99.99 with no unit: < 100, treated as kWh → 99990 Wh
        _juhang.NormalizeEnergyToWh("99.99", null, "Energy.Active.Import.Register")
            .ShouldBe(99990m);
    }

    [Theory]
    [InlineData("5000", "Wh", 5000)]
    [InlineData("12345", "Wh", 12345)]
    [InlineData("22000", "Wh", 22000)]
    public void Juhang_MeterValues_Energy_ExplicitWh_NoConversion(
        string rawValue, string unit, decimal expectedWh)
    {
        _juhang.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("5.0", "kWh", 5000)]
    [InlineData("22.5", "kWh", 22500)]
    public void Juhang_MeterValues_Energy_ExplicitKWh_Converted(
        string rawValue, string unit, decimal expectedWh)
    {
        _juhang.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Fact]
    public void Juhang_MeterValues_Power_InW()
    {
        // JUHANG sends power in W (standard)
        _juhang.NormalizePowerToW("7200", "W").ShouldBe(7200m);
        _juhang.NormalizePowerToW("60000", "W").ShouldBe(60000m);
        _juhang.NormalizePowerToW("120000", "W").ShouldBe(120000m);
    }

    #endregion

    #region JUHANG — Timestamp Fixtures (Space Separator Quirk)

    [Theory]
    [InlineData("2026-03-09 14:30:00", 14, 30, 0)]
    [InlineData("2026-03-09 08:15:30", 8, 15, 30)]
    [InlineData("2026-03-09 00:00:00", 0, 0, 0)]
    [InlineData("2026-03-09 23:59:59", 23, 59, 59)]
    public void Juhang_Timestamp_SpaceSeparator_ParsedCorrectly(
        string timestamp, int expectedHour, int expectedMinute, int expectedSecond)
    {
        var result = _juhang.ParseTimestamp(timestamp);
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc); // Assumed UTC when no TZ
        result.Value.Hour.ShouldBe(expectedHour);
        result.Value.Minute.ShouldBe(expectedMinute);
        result.Value.Second.ShouldBe(expectedSecond);
    }

    [Theory]
    [InlineData("2026-03-09T14:30:00Z", 14, 30)]
    [InlineData("2026-03-09T14:30:00.500Z", 14, 30)]
    [InlineData("2026-03-09T14:30:00+07:00", 7, 30)]   // 14:30 +07 → 07:30 UTC
    [InlineData("2026-03-09T14:30:00.123+07:00", 7, 30)]
    public void Juhang_Timestamp_ISO8601_Variants(
        string timestamp, int expectedUtcHour, int expectedUtcMinute)
    {
        var result = _juhang.ParseTimestamp(timestamp);
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(expectedUtcHour);
        result.Value.Minute.ShouldBe(expectedUtcMinute);
    }

    [Fact]
    public void Juhang_Timestamp_SpaceSeparator_TreatedAsUtc()
    {
        // JUHANG space-separated timestamps have no timezone; assumed UTC
        var result = _juhang.ParseTimestamp("2026-06-15 16:45:00");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Year.ShouldBe(2026);
        result.Value.Month.ShouldBe(6);
        result.Value.Day.ShouldBe(15);
        result.Value.Hour.ShouldBe(16);
    }

    #endregion

    #region JUHANG — MeterValuesMayOmitTransactionId

    [Fact]
    public void Juhang_MeterValues_OmitsTransactionId()
    {
        // JUHANG chargers are known to omit transactionId in MeterValues.
        // The handler should resolve the transaction by cpId + connectorId.
        _juhang.MeterValuesMayOmitTransactionId.ShouldBeTrue();
    }

    [Fact]
    public void Juhang_MayRetryStartTransaction_OnReconnect()
    {
        // JUHANG chargers may resend StartTransaction after a websocket reconnect
        _juhang.MayRetryStartTransaction.ShouldBeTrue();
    }

    #endregion

    #region JUHANG — Full Charging Session Simulation

    [Fact]
    public void Juhang_FullSession_DCFastCharge_WhValues()
    {
        // Simulate a JUHANG DC fast charging session (120 kW charger)
        // Session duration: ~25 minutes, delivering ~50 kWh

        // MeterValues reported in Wh by JUHANG (values >= 100)
        var meterStart = _juhang.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        var meter5min = _juhang.NormalizeEnergyToWh("10200", null, "Energy.Active.Import.Register");
        var meter10min = _juhang.NormalizeEnergyToWh("20100", null, "Energy.Active.Import.Register");
        var meter15min = _juhang.NormalizeEnergyToWh("30500", null, "Energy.Active.Import.Register");
        var meter20min = _juhang.NormalizeEnergyToWh("40300", null, "Energy.Active.Import.Register");
        var meterStop = _juhang.NormalizeEnergyToWh("50100", null, "Energy.Active.Import.Register");

        meterStart.ShouldBe(0m);
        meter5min.ShouldBe(10200m);
        meter10min.ShouldBe(20100m);
        meter15min.ShouldBe(30500m);
        meter20min.ShouldBe(40300m);
        meterStop.ShouldBe(50100m);

        var totalEnergyWh = meterStop - meterStart;
        totalEnergyWh.ShouldBe(50100m); // ~50.1 kWh
    }

    #endregion

    // =====================================================================
    // Generic Profile Fixtures — Unknown/unrecognized vendor fallback
    // =====================================================================

    #region Generic — BootNotification Fixtures (Fallback)

    [Theory]
    [InlineData("ABB", "Terra AC W22-T-R-0")]
    [InlineData("Schneider Electric", "EVlink Pro AC")]
    [InlineData("Siemens", "VersiCharge Gen3")]
    [InlineData("Unknown Vendor Co.", "Model X-100")]
    [InlineData("Delta Electronics", "AC MAX")]
    public void Generic_BootNotification_UnknownVendor_FallsBackToGeneric(
        string vendorString, string model)
    {
        var profile = _factory.Detect(vendorString, model);
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Generic_BootNotification_NullVendor_FallsBackToGeneric()
    {
        var profile = _factory.Detect(null, null);
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Generic_BootNotification_EmptyVendor_FallsBackToGeneric()
    {
        var profile = _factory.Detect("", "");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Generic_BootNotification_DefaultProperties()
    {
        _generic.HeartbeatIntervalSeconds.ShouldBe(60);
        _generic.MayRetryStartTransaction.ShouldBeFalse();
        _generic.MeterValuesMayOmitTransactionId.ShouldBeFalse();
    }

    #endregion

    #region Generic — MeterValues Fixtures (Explicit Units)

    [Theory]
    [InlineData("5000", "Wh", 5000)]
    [InlineData("12345.6", "Wh", 12345.6)]
    [InlineData("0", "Wh", 0)]
    public void Generic_MeterValues_Energy_ExplicitWh(
        string rawValue, string unit, decimal expectedWh)
    {
        _generic.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("5.0", "kWh", 5000)]
    [InlineData("12.345", "kWh", 12345)]
    [InlineData("0", "kWh", 0)]
    [InlineData("100.0", "kWh", 100000)]
    public void Generic_MeterValues_Energy_ExplicitKWh(
        string rawValue, string unit, decimal expectedWh)
    {
        _generic.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    #endregion

    #region Generic — MeterValues Fixtures (No Unit — Inference)

    [Theory]
    [InlineData("0.5", "", 500)]       // 0.5 <= 100, > 0 → kWh → 500 Wh
    [InlineData("5.5", "", 5500)]      // 5.5 <= 100, > 0 → kWh → 5500 Wh
    [InlineData("50", "", 50000)]      // 50 <= 100, > 0 → kWh → 50000 Wh
    [InlineData("100", "", 100000)]    // 100 <= 100, > 0 → kWh → 100000 Wh
    public void Generic_MeterValues_Energy_NoUnit_SmallValues_InferKWh(
        string rawValue, string unit, decimal expectedWh)
    {
        // Generic InferEnergyUnit: value > 0 && value <= 100 → kWh * 1000
        _generic.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("100.1", "", 100.1)]    // 100.1 > 100 → Wh (pass through)
    [InlineData("500", "", 500)]        // 500 > 100 → Wh
    [InlineData("5000", "", 5000)]      // 5000 > 100 → Wh
    [InlineData("22000", "", 22000)]    // 22000 > 100 → Wh
    public void Generic_MeterValues_Energy_NoUnit_LargeValues_InferWh(
        string rawValue, string unit, decimal expectedWh)
    {
        // Generic InferEnergyUnit: value > 100 → Wh (pass through)
        _generic.NormalizeEnergyToWh(rawValue, unit, "Energy.Active.Import.Register")
            .ShouldBe(expectedWh);
    }

    [Fact]
    public void Generic_MeterValues_Energy_NoUnit_Zero_ReturnsZero()
    {
        // 0 is not > 0, so InferEnergyUnit returns value as-is (0)
        _generic.NormalizeEnergyToWh("0", "", "Energy.Active.Import.Register")
            .ShouldBe(0m);
    }

    [Fact]
    public void Generic_MeterValues_Energy_NoUnit_Negative_ReturnsSameValue()
    {
        // Negative values: not > 0, passes through
        _generic.NormalizeEnergyToWh("-10", "", "Energy.Active.Import.Register")
            .ShouldBe(-10m);
    }

    #endregion

    #region Generic — Timestamp Parsing Fixtures (Various Formats)

    [Theory]
    [InlineData("2026-03-09T14:30:00Z")]
    [InlineData("2026-03-09T14:30:00.000Z")]
    [InlineData("2026-03-09T14:30:00.123Z")]
    public void Generic_Timestamp_ISO8601_WithZ(string timestamp)
    {
        var result = _generic.ParseTimestamp(timestamp);
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(14);
        result.Value.Minute.ShouldBe(30);
    }

    [Theory]
    [InlineData("2026-03-09T14:30:00+07:00", 7, 30)]
    [InlineData("2026-03-09T14:30:00+00:00", 14, 30)]
    [InlineData("2026-03-09T14:30:00-05:00", 19, 30)]
    [InlineData("2026-03-09T14:30:00.500+07:00", 7, 30)]
    public void Generic_Timestamp_ISO8601_WithOffset(
        string timestamp, int expectedUtcHour, int expectedUtcMinute)
    {
        var result = _generic.ParseTimestamp(timestamp);
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(expectedUtcHour);
        result.Value.Minute.ShouldBe(expectedUtcMinute);
    }

    [Fact]
    public void Generic_Timestamp_WithoutTimezone_AssumedUtc()
    {
        var result = _generic.ParseTimestamp("2026-03-09T14:30:00");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(14);
    }

    [Fact]
    public void Generic_Timestamp_SpaceSeparator_Supported()
    {
        // Base class supports space separator as a format
        var result = _generic.ParseTimestamp("2026-03-09 14:30:00");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(14);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-timestamp")]
    [InlineData("hello world 123")]
    public void Generic_Timestamp_InvalidOrNull_ReturnsNull(string? timestamp)
    {
        _generic.ParseTimestamp(timestamp).ShouldBeNull();
    }

    [Fact]
    public void Generic_Timestamp_SlashFormat_ParsedByFallback()
    {
        // The base class fallback DateTime.TryParse can handle slash formats
        var result = _generic.ParseTimestamp("2026/03/09");
        result.ShouldNotBeNull();
    }

    #endregion

    // =====================================================================
    // Cross-Vendor Comparison Tests
    // =====================================================================

    #region Cross-Vendor — Same Raw Value, Different Results

    [Theory]
    [InlineData("50.5")]
    [InlineData("25.0")]
    [InlineData("7.2")]
    [InlineData("99.9")]
    public void CrossVendor_SmallEnergyValue_NoUnit_AllAssumeKWh(string rawValue)
    {
        // For values < 100 with no unit, all three vendors assume kWh:
        // Generic: InferEnergyUnit → value <= 100 → kWh * 1000
        // Chargecore: always kWh → * 1000
        // JUHANG: value < 100 → kWh * 1000
        var parsed = decimal.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture);
        var expectedWh = parsed * 1000m;

        _generic.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(expectedWh,
            $"Generic should convert {rawValue} (no unit) to {expectedWh} Wh");
        _chargecore.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(expectedWh,
            $"Chargecore should convert {rawValue} (no unit) to {expectedWh} Wh");
        _juhang.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(expectedWh,
            $"JUHANG should convert {rawValue} (no unit) to {expectedWh} Wh");
    }

    [Theory]
    [InlineData("500")]
    [InlineData("5000")]
    [InlineData("22000")]
    public void CrossVendor_LargeEnergyValue_NoUnit_ChargecoreDiffers(string rawValue)
    {
        // For values > 100 with no unit:
        // Generic: InferEnergyUnit → value > 100 → Wh (pass through)
        // Chargecore: ALWAYS kWh → * 1000 (DIFFERENT!)
        // JUHANG: value >= 100, no unit → Wh (pass through)
        var parsed = decimal.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture);

        _generic.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(parsed,
            "Generic infers large values as Wh");
        _chargecore.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(parsed * 1000m,
            "Chargecore ALWAYS assumes kWh, even for large values");
        _juhang.NormalizeEnergyToWh(rawValue, null, null).ShouldBe(parsed,
            "JUHANG infers large values as Wh");
    }

    [Fact]
    public void CrossVendor_ExactlyAt100_NoUnit_DivergentBehavior()
    {
        // "100" with no unit:
        // Generic: 100 is NOT > 100, and 100 > 0 and 100 <= 100 → kWh → 100000
        // Chargecore: always kWh → 100 * 1000 = 100000
        // JUHANG: 100 is NOT < 100, treated as Wh → 100
        _generic.NormalizeEnergyToWh("100", null, null).ShouldBe(100000m);
        _chargecore.NormalizeEnergyToWh("100", null, null).ShouldBe(100000m);
        _juhang.NormalizeEnergyToWh("100", null, null).ShouldBe(100m); // KEY DIFFERENCE
    }

    [Fact]
    public void CrossVendor_ExactlyAt100Point1_NoUnit_DivergentBehavior()
    {
        // "100.1" with no unit:
        // Generic: 100.1 > 100 → Wh → 100.1
        // Chargecore: always kWh → 100100
        // JUHANG: 100.1 >= 100 → Wh → 100.1
        _generic.NormalizeEnergyToWh("100.1", null, null).ShouldBe(100.1m);
        _chargecore.NormalizeEnergyToWh("100.1", null, null).ShouldBe(100100m);
        _juhang.NormalizeEnergyToWh("100.1", null, null).ShouldBe(100.1m);
    }

    [Fact]
    public void CrossVendor_ExplicitUnits_AllAgree()
    {
        // When units are explicit, all vendors should produce the same result
        _generic.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);
        _chargecore.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);
        _juhang.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);

        _generic.NormalizeEnergyToWh("5.0", "kWh", null).ShouldBe(5000m);
        _chargecore.NormalizeEnergyToWh("5.0", "kWh", null).ShouldBe(5000m);
        _juhang.NormalizeEnergyToWh("5.0", "kWh", null).ShouldBe(5000m);
    }

    #endregion

    #region Cross-Vendor — Factory Detection with Realistic Vendor Strings

    [Theory]
    [InlineData("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU", VendorProfileType.ChargecoreGlobal)]
    [InlineData("ChargeCore Global v3.0.0-rc.2", "CCG-DC50-VN", VendorProfileType.ChargecoreGlobal)]
    [InlineData("CCG OEM", "CustomModel", VendorProfileType.ChargecoreGlobal)]
    [InlineData("JUHANG", "JH-DC120-VN", VendorProfileType.Juhang)]
    [InlineData("JuHang Technology Co. Ltd", "JH-AC7-S2", VendorProfileType.Juhang)]
    [InlineData("Ju Hang New Energy", "JH-DC240", VendorProfileType.Juhang)]
    [InlineData("ABB", "Terra AC W22-T-R-0", VendorProfileType.Generic)]
    [InlineData("Schneider Electric", "EVlink Pro AC", VendorProfileType.Generic)]
    [InlineData("", "", VendorProfileType.Generic)]
    [InlineData(null, null, VendorProfileType.Generic)]
    public void CrossVendor_FactoryDetection_CorrectProfile(
        string? vendor, string? model, VendorProfileType expectedType)
    {
        var profile = _factory.Detect(vendor, model);
        profile.ProfileType.ShouldBe(expectedType);
    }

    [Theory]
    [InlineData("chargecore", VendorProfileType.ChargecoreGlobal)]     // lowercase
    [InlineData("CHARGECORE", VendorProfileType.ChargecoreGlobal)]     // uppercase
    [InlineData("ChArGeCoRe", VendorProfileType.ChargecoreGlobal)]     // mixed case
    [InlineData("juhang", VendorProfileType.Juhang)]                   // lowercase
    [InlineData("JUHANG", VendorProfileType.Juhang)]                   // uppercase
    [InlineData("JuHang", VendorProfileType.Juhang)]                   // mixed case
    public void CrossVendor_FactoryDetection_CaseInsensitive(
        string vendor, VendorProfileType expectedType)
    {
        var profile = _factory.Detect(vendor, "SomeModel");
        profile.ProfileType.ShouldBe(expectedType);
    }

    [Theory]
    [InlineData("Chargecore-Custom-Build-v3")]
    [InlineData("My Chargecore Device")]
    [InlineData("CCG International Ltd.")]
    [InlineData("Official ChargeCore Global Partner")]
    public void CrossVendor_FactoryDetection_PartialMatch_Chargecore(string vendor)
    {
        _factory.Detect(vendor, null).ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    [Theory]
    [InlineData("JUHANG Electric Co. Ltd")]
    [InlineData("Shenzhen JUHANG New Energy")]
    [InlineData("Ju Hang Technology")]
    [InlineData("Official JuHang Authorized")]
    public void CrossVendor_FactoryDetection_PartialMatch_Juhang(string vendor)
    {
        _factory.Detect(vendor, null).ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    #endregion

    #region Cross-Vendor — Factory Resolve by ProfileType

    [Fact]
    public void CrossVendor_FactoryResolve_AllTypes()
    {
        _factory.Resolve(VendorProfileType.Generic).ProfileType
            .ShouldBe(VendorProfileType.Generic);
        _factory.Resolve(VendorProfileType.ChargecoreGlobal).ProfileType
            .ShouldBe(VendorProfileType.ChargecoreGlobal);
        _factory.Resolve(VendorProfileType.Juhang).ProfileType
            .ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void CrossVendor_FactoryResolve_UnknownType_FallsToGeneric()
    {
        _factory.Resolve((VendorProfileType)999).ProfileType
            .ShouldBe(VendorProfileType.Generic);
    }

    #endregion

    // =====================================================================
    // Full Charging Session Simulations (End-to-End Scenarios)
    // =====================================================================

    #region Chargecore — Full AC Session Simulation

    [Fact]
    public void Chargecore_FullSession_AC22kW_30Minutes()
    {
        // Simulate Chargecore AC 22kW charger, 30-minute session
        // Chargecore reports energy in kWh (no unit field)
        var profile = _factory.Detect("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU");

        // BootNotification → profile detected
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);

        // StartTransaction: meterStart = 0 kWh
        var meterStartWh = profile.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        meterStartWh.ShouldBe(0m);

        // MeterValues during session (every 5 minutes), reported in kWh
        var ts1 = profile.ParseTimestamp("2026-03-09T10:00:00Z");
        var energy5min = profile.NormalizeEnergyToWh("1.8", null, "Energy.Active.Import.Register");
        var power5min = profile.NormalizePowerToW("21.6", "kW");

        var ts2 = profile.ParseTimestamp("2026-03-09T10:05:00Z");
        var energy10min = profile.NormalizeEnergyToWh("3.6", null, "Energy.Active.Import.Register");

        var ts3 = profile.ParseTimestamp("2026-03-09T10:10:00Z");
        var energy15min = profile.NormalizeEnergyToWh("5.4", null, "Energy.Active.Import.Register");

        var ts4 = profile.ParseTimestamp("2026-03-09T10:15:00Z");
        var energy20min = profile.NormalizeEnergyToWh("7.1", null, "Energy.Active.Import.Register");

        var ts5 = profile.ParseTimestamp("2026-03-09T10:20:00Z");
        var energy25min = profile.NormalizeEnergyToWh("8.8", null, "Energy.Active.Import.Register");

        // StopTransaction: meterStop = 10.5 kWh
        var ts6 = profile.ParseTimestamp("2026-03-09T10:30:00Z");
        var meterStopWh = profile.NormalizeEnergyToWh("10.5", null, "Energy.Active.Import.Register");

        // Verify all timestamps parsed
        ts1.ShouldNotBeNull();
        ts6.ShouldNotBeNull();

        // Verify energy progression (all in Wh after normalization)
        energy5min.ShouldBe(1800m);
        energy10min.ShouldBe(3600m);
        energy15min.ShouldBe(5400m);
        energy20min.ShouldBe(7100m);
        energy25min.ShouldBe(8800m);
        meterStopWh.ShouldBe(10500m);

        // Verify power reading
        power5min.ShouldBe(21600m);

        // Total energy delivered
        var totalWh = meterStopWh - meterStartWh;
        totalWh.ShouldBe(10500m); // 10.5 kWh
    }

    #endregion

    #region JUHANG — Full DC Session Simulation

    [Fact]
    public void Juhang_FullSession_DC120kW_25Minutes()
    {
        // Simulate JUHANG DC 120kW charger, 25-minute session
        // JUHANG reports energy in Wh (large values), timestamps with space separator
        var profile = _factory.Detect("JUHANG Electric", "JH-DC120-VN");

        // BootNotification → profile detected
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
        profile.MeterValuesMayOmitTransactionId.ShouldBeTrue();

        // StartTransaction: meterStart = 0 Wh
        var meterStartWh = profile.NormalizeEnergyToWh("0", null, "Energy.Active.Import.Register");
        meterStartWh.ShouldBe(0m);

        // MeterValues (every 60s), JUHANG uses space-separated timestamps
        var ts1 = profile.ParseTimestamp("2026-03-09 14:00:00");
        var energy1min = profile.NormalizeEnergyToWh("2000", null, "Energy.Active.Import.Register");
        var power1min = profile.NormalizePowerToW("115000", "W");

        var ts2 = profile.ParseTimestamp("2026-03-09 14:05:00");
        var energy5min = profile.NormalizeEnergyToWh("10200", null, "Energy.Active.Import.Register");

        var ts3 = profile.ParseTimestamp("2026-03-09 14:10:00");
        var energy10min = profile.NormalizeEnergyToWh("19800", null, "Energy.Active.Import.Register");

        var ts4 = profile.ParseTimestamp("2026-03-09 14:15:00");
        var energy15min = profile.NormalizeEnergyToWh("28500", null, "Energy.Active.Import.Register");

        var ts5 = profile.ParseTimestamp("2026-03-09 14:20:00");
        var energy20min = profile.NormalizeEnergyToWh("36700", null, "Energy.Active.Import.Register");

        // StopTransaction: meterStop = 45300 Wh
        var ts6 = profile.ParseTimestamp("2026-03-09 14:25:00");
        var meterStopWh = profile.NormalizeEnergyToWh("45300", null, "Energy.Active.Import.Register");

        // Verify timestamps
        ts1.ShouldNotBeNull();
        ts1!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        ts6.ShouldNotBeNull();

        // Verify energy progression (all Wh, no conversion needed for large values)
        energy1min.ShouldBe(2000m);
        energy5min.ShouldBe(10200m);
        energy10min.ShouldBe(19800m);
        energy15min.ShouldBe(28500m);
        energy20min.ShouldBe(36700m);
        meterStopWh.ShouldBe(45300m);

        // Verify power reading
        power1min.ShouldBe(115000m); // 115 kW in W

        // Total energy delivered
        var totalWh = meterStopWh - meterStartWh;
        totalWh.ShouldBe(45300m); // ~45.3 kWh
    }

    #endregion

    #region Generic — Full Session Simulation (Unknown ABB Charger)

    [Fact]
    public void Generic_FullSession_ABB_AC_WithExplicitUnits()
    {
        // Simulate an ABB charger (falls back to Generic) that sends explicit Wh units
        var profile = _factory.Detect("ABB", "Terra AC W22-T-R-0");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);

        // StartTransaction
        var meterStartWh = profile.NormalizeEnergyToWh("0", "Wh", "Energy.Active.Import.Register");
        meterStartWh.ShouldBe(0m);

        // MeterValues with explicit Wh units
        var ts1 = profile.ParseTimestamp("2026-03-09T10:00:00Z");
        var energy5min = profile.NormalizeEnergyToWh("1800", "Wh", "Energy.Active.Import.Register");
        var power5min = profile.NormalizePowerToW("7200", "W");

        var ts2 = profile.ParseTimestamp("2026-03-09T10:10:00Z");
        var energy10min = profile.NormalizeEnergyToWh("3600", "Wh", "Energy.Active.Import.Register");

        // StopTransaction
        var ts3 = profile.ParseTimestamp("2026-03-09T10:30:00Z");
        var meterStopWh = profile.NormalizeEnergyToWh("10500", "Wh", "Energy.Active.Import.Register");

        ts1.ShouldNotBeNull();
        ts3.ShouldNotBeNull();

        energy5min.ShouldBe(1800m);
        energy10min.ShouldBe(3600m);
        meterStopWh.ShouldBe(10500m);
        power5min.ShouldBe(7200m);

        var totalWh = meterStopWh - meterStartWh;
        totalWh.ShouldBe(10500m); // 10.5 kWh
    }

    [Fact]
    public void Generic_FullSession_UnknownVendor_WithKWhUnits()
    {
        // Simulate an unknown charger sending energy in kWh
        var profile = _factory.Detect("Unknown Vendor", "Model X");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);

        var meterStartWh = profile.NormalizeEnergyToWh("0", "kWh", "Energy.Active.Import.Register");
        var meterStopWh = profile.NormalizeEnergyToWh("15.3", "kWh", "Energy.Active.Import.Register");

        meterStartWh.ShouldBe(0m);
        meterStopWh.ShouldBe(15300m);
    }

    #endregion

    // =====================================================================
    // Edge Case / Regression Scenarios
    // =====================================================================

    #region Edge Cases — Decimal Precision

    [Theory]
    [InlineData("0.001", null, 1)]             // Chargecore: 0.001 kWh → 1 Wh
    [InlineData("0.0001", null, 0.1)]          // Chargecore: 0.0001 kWh → 0.1 Wh
    [InlineData("99.999", null, 99999)]        // Chargecore: 99.999 kWh → 99999 Wh
    public void Chargecore_DecimalPrecision(string rawValue, string? unit, decimal expectedWh)
    {
        _chargecore.NormalizeEnergyToWh(rawValue, unit, null).ShouldBe(expectedWh);
    }

    [Theory]
    [InlineData("0.001", null, 1)]             // JUHANG: 0.001 < 100, infer kWh → 1 Wh
    [InlineData("99.999", null, 99999)]        // JUHANG: 99.999 < 100, infer kWh → 99999 Wh
    [InlineData("100.001", null, 100.001)]     // JUHANG: 100.001 >= 100, stays Wh
    public void Juhang_DecimalPrecision(string rawValue, string? unit, decimal expectedWh)
    {
        _juhang.NormalizeEnergyToWh(rawValue, unit, null).ShouldBe(expectedWh);
    }

    #endregion

    #region Edge Cases — Invalid Data Handling

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("--5")]
    public void AllVendors_InvalidEnergyValue_ReturnsZero(string rawValue)
    {
        _generic.NormalizeEnergyToWh(rawValue, "Wh", null).ShouldBe(0m);
        _chargecore.NormalizeEnergyToWh(rawValue, "Wh", null).ShouldBe(0m);
        _juhang.NormalizeEnergyToWh(rawValue, "Wh", null).ShouldBe(0m);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("NaN")]
    public void AllVendors_InvalidPowerValue_ReturnsZero(string rawValue)
    {
        _generic.NormalizePowerToW(rawValue, "W").ShouldBe(0m);
        _chargecore.NormalizePowerToW(rawValue, "W").ShouldBe(0m);
        _juhang.NormalizePowerToW(rawValue, "W").ShouldBe(0m);
    }

    #endregion

    #region Edge Cases — Power Inference (No Unit)

    [Fact]
    public void Generic_Power_NoUnit_ThresholdAt500()
    {
        // Generic: value > 500 → W, value <= 500 → kW * 1000
        _generic.NormalizePowerToW("500", "").ShouldBe(500000m);   // 500 <= 500 → kW
        _generic.NormalizePowerToW("501", "").ShouldBe(501m);     // 501 > 500 → W
        _generic.NormalizePowerToW("7.2", "").ShouldBe(7200m);    // 7.2 <= 500 → kW
        _generic.NormalizePowerToW("7200", "").ShouldBe(7200m);   // 7200 > 500 → W
    }

    [Fact]
    public void AllVendors_Power_WithUnit_SameResults()
    {
        // With explicit units, all vendors behave identically for power
        _generic.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
        _chargecore.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
        _juhang.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);

        _generic.NormalizePowerToW("7200", "W").ShouldBe(7200m);
        _chargecore.NormalizePowerToW("7200", "W").ShouldBe(7200m);
        _juhang.NormalizePowerToW("7200", "W").ShouldBe(7200m);
    }

    #endregion

    #region Edge Cases — Chargecore Duplicate StartTransaction

    [Fact]
    public void Chargecore_MayRetryStartTransaction_FlaggedForIdempotency()
    {
        // Chargecore chargers may retry StartTransaction on websocket reconnection.
        // The handler must check for existing active sessions to handle idempotently.
        var profile = _factory.Detect("ChargeCore Global v2.3.1-build.87", "CCG-AC22-AU");
        profile.MayRetryStartTransaction.ShouldBeTrue();
    }

    [Fact]
    public void Generic_DoesNot_RetryStartTransaction()
    {
        var profile = _factory.Detect("ABB", "Terra AC");
        profile.MayRetryStartTransaction.ShouldBeFalse();
    }

    #endregion
}
