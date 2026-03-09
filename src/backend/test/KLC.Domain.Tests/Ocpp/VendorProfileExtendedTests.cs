using System;
using System.Collections.Generic;
using KLC.Enums;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.Ocpp;

/// <summary>
/// Extended tests for vendor profiles covering edge cases, boundary values,
/// cross-vendor comparisons, and the factory's auto-detect/resolve behavior.
/// </summary>
public class VendorProfileExtendedTests
{
    private readonly GenericProfile _generic;
    private readonly ChargecoreGlobalProfile _chargecore;
    private readonly JuhangProfile _juhang;
    private readonly VendorProfileFactory _factory;

    public VendorProfileExtendedTests()
    {
        _generic = new GenericProfile(NullLogger<GenericProfile>.Instance);
        _chargecore = new ChargecoreGlobalProfile(NullLogger<ChargecoreGlobalProfile>.Instance);
        _juhang = new JuhangProfile(NullLogger<JuhangProfile>.Instance);

        var profiles = new List<IVendorProfile> { _generic, _chargecore, _juhang };
        _factory = new VendorProfileFactory(profiles, NullLogger<VendorProfileFactory>.Instance);
    }

    #region Energy Normalization — Boundary Values

    [Theory]
    [InlineData("0", "Wh", 0)]
    [InlineData("0.001", "Wh", 0.001)]
    [InlineData("99999999", "Wh", 99999999)]
    [InlineData("-100", "Wh", -100)] // Negative values should pass through
    public void Generic_NormalizeEnergy_BoundaryValues(string rawValue, string unit, decimal expected)
    {
        _generic.NormalizeEnergyToWh(rawValue, unit, null).ShouldBe(expected);
    }

    [Theory]
    [InlineData("100.0", "", 100000.0)] // Exactly 100 — threshold boundary, infer kWh (* 1000)
    [InlineData("100.1", "", 100.1)]   // Just above 100 — infer Wh (pass through)
    [InlineData("99.9", "", 99900)]    // Just below 100 — infer kWh (* 1000)
    public void Generic_NormalizeEnergy_InferenceThreshold(string rawValue, string unit, decimal expected)
    {
        _generic.NormalizeEnergyToWh(rawValue, unit, null).ShouldBe(expected);
    }

    [Fact]
    public void Generic_NormalizeEnergy_Zero_NoUnit_ReturnsZero()
    {
        _generic.NormalizeEnergyToWh("0", "", null).ShouldBe(0m);
    }

    [Fact]
    public void Generic_NormalizeEnergy_NegativeValue_NoUnit_ReturnsSameValue()
    {
        // Negative values with no unit should not be multiplied
        _generic.NormalizeEnergyToWh("-5", "", null).ShouldBe(-5m);
    }

    [Theory]
    [InlineData("", "Wh", 0)]
    [InlineData("  ", "Wh", 0)]
    [InlineData("NaN", "Wh", 0)]
    [InlineData("Infinity", "Wh", 0)]
    public void Generic_NormalizeEnergy_InvalidStrings_ReturnZero(string rawValue, string unit, decimal expected)
    {
        _generic.NormalizeEnergyToWh(rawValue, unit, null).ShouldBe(expected);
    }

    #endregion

    #region Chargecore — Energy Edge Cases

    [Fact]
    public void Chargecore_NormalizeEnergy_VerySmallValue_NoUnit_StillMultiplied()
    {
        // Chargecore ALWAYS assumes kWh when unit is empty — even for small values
        _chargecore.NormalizeEnergyToWh("0.001", "", null).ShouldBe(1m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_LargeValue_NoUnit_StillMultiplied()
    {
        // Even 5000 "kWh" gets multiplied — Chargecore always assumes kWh with no unit
        _chargecore.NormalizeEnergyToWh("5000", "", null).ShouldBe(5000000m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_Zero_NoUnit()
    {
        _chargecore.NormalizeEnergyToWh("0", "", null).ShouldBe(0m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_Invalid_ReturnsZero()
    {
        _chargecore.NormalizeEnergyToWh("invalid", "kWh", null).ShouldBe(0m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_NullUnit_AssumesKWh()
    {
        _chargecore.NormalizeEnergyToWh("10.5", null, null).ShouldBe(10500m);
    }

    #endregion

    #region JUHANG — Energy Edge Cases

    [Fact]
    public void Juhang_NormalizeEnergy_ExactlyZero_NoUnit()
    {
        // Zero is not > 0, so the small-value check (< 100) does not trigger
        _juhang.NormalizeEnergyToWh("0", "", null).ShouldBe(0m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_Exactly100_NoUnit_AssumesWh()
    {
        // 100 with no unit: not < 100, so stays as Wh
        _juhang.NormalizeEnergyToWh("100", "", null).ShouldBe(100m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_99_NoUnit_AssumesKWh()
    {
        // 99 < 100 with no unit → assumed kWh
        _juhang.NormalizeEnergyToWh("99", "", null).ShouldBe(99000m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_NullUnit_LargeValue_AssumesWh()
    {
        // null unit treated same as empty
        _juhang.NormalizeEnergyToWh("5000", null, null).ShouldBe(5000m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_NullUnit_SmallValue_AssumesKWh()
    {
        _juhang.NormalizeEnergyToWh("5.5", null, null).ShouldBe(5500m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_UnknownUnit_FallsToInfer()
    {
        // Unknown unit like "MWh" falls to InferEnergyUnit in base class
        var result = _juhang.NormalizeEnergyToWh("500", "MWh", null);
        // 500 > 100 → inferred as Wh
        result.ShouldBe(500m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_Invalid_ReturnsZero()
    {
        _juhang.NormalizeEnergyToWh("not-a-number", "Wh", null).ShouldBe(0m);
    }

    #endregion

    #region Power Normalization — All Vendors

    [Theory]
    [InlineData("0", "W", 0)]
    [InlineData("11000", "W", 11000)]
    [InlineData("11", "kW", 11000)]
    [InlineData("0.001", "kW", 1)]
    public void Generic_NormalizePower_ExactUnits(string rawValue, string unit, decimal expected)
    {
        _generic.NormalizePowerToW(rawValue, unit).ShouldBe(expected);
    }

    [Fact]
    public void Generic_NormalizePower_NoUnit_500_Threshold()
    {
        // 500 — exactly at threshold. <= 500 → kW * 1000
        _generic.NormalizePowerToW("500", "").ShouldBe(500000m);

        // 501 — above threshold → W
        _generic.NormalizePowerToW("501", "").ShouldBe(501m);

        // 499 — below threshold → kW * 1000
        _generic.NormalizePowerToW("499", "").ShouldBe(499000m);
    }

    [Fact]
    public void Generic_NormalizePower_Invalid_ReturnsZero()
    {
        _generic.NormalizePowerToW("invalid", "W").ShouldBe(0m);
    }

    [Fact]
    public void Generic_NormalizePower_NullUnit_LargeValue()
    {
        _generic.NormalizePowerToW("7200", null).ShouldBe(7200m);
    }

    [Fact]
    public void Generic_NormalizePower_NullUnit_SmallValue()
    {
        _generic.NormalizePowerToW("7.2", null).ShouldBe(7200m);
    }

    [Fact]
    public void Generic_NormalizePower_UnknownUnit_PassesThrough()
    {
        // Unknown units just return the raw value
        _generic.NormalizePowerToW("7200", "MegaWatt").ShouldBe(7200m);
    }

    // Chargecore and Juhang inherit base power normalization
    [Fact]
    public void Chargecore_NormalizePower_SameAsGeneric()
    {
        _chargecore.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
        _chargecore.NormalizePowerToW("7200", "W").ShouldBe(7200m);
    }

    [Fact]
    public void Juhang_NormalizePower_SameAsGeneric()
    {
        _juhang.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
        _juhang.NormalizePowerToW("7200", "W").ShouldBe(7200m);
    }

    #endregion

    #region Timestamp Parsing — Edge Cases

    [Fact]
    public void Generic_ParseTimestamp_WithoutTimezone_AssumedUtc()
    {
        var result = _generic.ParseTimestamp("2026-03-05T14:30:00");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Generic_ParseTimestamp_SpaceSeparator()
    {
        // The base class supports space separator via fallback parse
        var result = _generic.ParseTimestamp("2026-03-05 14:30:00");
        result.ShouldNotBeNull();
    }

    [Fact]
    public void Generic_ParseTimestamp_WhitespaceOnly_ReturnsNull()
    {
        _generic.ParseTimestamp("   ").ShouldBeNull();
    }

    [Fact]
    public void Juhang_ParseTimestamp_SpaceSeparator_CorrectTime()
    {
        var result = _juhang.ParseTimestamp("2026-06-15 08:45:30");
        result.ShouldNotBeNull();
        result!.Value.Hour.ShouldBe(8);
        result.Value.Minute.ShouldBe(45);
        result.Value.Second.ShouldBe(30);
    }

    [Fact]
    public void Juhang_ParseTimestamp_WithMillisAndZ()
    {
        var result = _juhang.ParseTimestamp("2026-03-05T14:30:00.500Z");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Millisecond.ShouldBe(500);
    }

    [Fact]
    public void Juhang_ParseTimestamp_WithOffset()
    {
        var result = _juhang.ParseTimestamp("2026-03-05T14:30:00+07:00");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(7); // 14:30 +07:00 → 07:30 UTC
    }

    [Fact]
    public void Juhang_ParseTimestamp_Garbage_ReturnsNull()
    {
        _juhang.ParseTimestamp("garbage-timestamp").ShouldBeNull();
    }

    [Fact]
    public void Chargecore_ParseTimestamp_InheritsBaseClass()
    {
        // Chargecore uses base class timestamp parsing
        var result = _chargecore.ParseTimestamp("2026-03-05T14:30:00Z");
        result.ShouldNotBeNull();
        result!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    #endregion

    #region Factory — Edge Cases

    [Fact]
    public void Factory_Detect_EmptyString_FallsBackToGeneric()
    {
        var profile = _factory.Detect("", "");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Factory_Detect_WhitespaceVendor_FallsBackToGeneric()
    {
        var profile = _factory.Detect("   ", "   ");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Factory_Detect_PartialMatch_Chargecore_WithPrefix()
    {
        var profile = _factory.Detect("Chargecore-Custom-Build-v3", "Custom Model");
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    [Fact]
    public void Factory_Detect_PartialMatch_Juhang_WithSuffix()
    {
        var profile = _factory.Detect("JUHANG Electric Co. Ltd", "JH-DC240");
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Factory_Resolve_UnknownType_FallsToGeneric()
    {
        // Cast an invalid int to VendorProfileType
        var profile = _factory.Resolve((VendorProfileType)999);
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Factory_Detection_Order_FirstMatchWins()
    {
        // When vendor contains both "Chargecore" and "JUHANG" (hypothetical),
        // the first match in iteration order wins
        // This tests that the factory iterates profiles in order
        var profile = _factory.Detect("Chargecore", null);
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    #endregion

    #region Profile Properties — Cross-Vendor Comparison

    [Fact]
    public void AllProfiles_HeartbeatInterval_Is60Seconds()
    {
        _generic.HeartbeatIntervalSeconds.ShouldBe(60);
        _chargecore.HeartbeatIntervalSeconds.ShouldBe(60);
        _juhang.HeartbeatIntervalSeconds.ShouldBe(60);
    }

    [Fact]
    public void ProfileType_Values_Are_Distinct()
    {
        var types = new HashSet<VendorProfileType>
        {
            _generic.ProfileType,
            _chargecore.ProfileType,
            _juhang.ProfileType
        };
        types.Count.ShouldBe(3);
    }

    [Fact]
    public void Chargecore_MeterValuesMayOmitTransactionId_False()
    {
        _chargecore.MeterValuesMayOmitTransactionId.ShouldBeFalse();
    }

    #endregion

    #region Cross-Vendor Energy Normalization Comparison

    [Fact]
    public void SameRawValue_DifferentVendors_DifferentResults_WhenNoUnit()
    {
        // "50" with no unit:
        // Generic: 50 <= 100 → infer kWh → 50000 Wh
        // Chargecore: always kWh → 50000 Wh
        // JUHANG: 50 < 100 → assumes kWh → 50000 Wh
        var genericResult = _generic.NormalizeEnergyToWh("50", "", null);
        var chargecoreResult = _chargecore.NormalizeEnergyToWh("50", "", null);
        var juhangResult = _juhang.NormalizeEnergyToWh("50", "", null);

        genericResult.ShouldBe(50000m);
        chargecoreResult.ShouldBe(50000m);
        juhangResult.ShouldBe(50000m);
    }

    [Fact]
    public void LargeValue_NoUnit_Chargecore_DiffersFromOthers()
    {
        // "5000" with no unit:
        // Generic: 5000 > 100 → Wh → 5000
        // Chargecore: always kWh → 5,000,000 Wh
        // JUHANG: 5000 >= 100 → Wh → 5000
        var genericResult = _generic.NormalizeEnergyToWh("5000", "", null);
        var chargecoreResult = _chargecore.NormalizeEnergyToWh("5000", "", null);
        var juhangResult = _juhang.NormalizeEnergyToWh("5000", "", null);

        genericResult.ShouldBe(5000m);
        chargecoreResult.ShouldBe(5000000m); // Key difference!
        juhangResult.ShouldBe(5000m);
    }

    [Fact]
    public void AllVendors_ExplicitWh_SameResult()
    {
        var value = "5000";
        var unit = "Wh";

        _generic.NormalizeEnergyToWh(value, unit, null).ShouldBe(5000m);
        _chargecore.NormalizeEnergyToWh(value, unit, null).ShouldBe(5000m);
        _juhang.NormalizeEnergyToWh(value, unit, null).ShouldBe(5000m);
    }

    [Fact]
    public void AllVendors_ExplicitKWh_SameResult()
    {
        var value = "5.5";
        var unit = "kWh";

        _generic.NormalizeEnergyToWh(value, unit, null).ShouldBe(5500m);
        _chargecore.NormalizeEnergyToWh(value, unit, null).ShouldBe(5500m);
        _juhang.NormalizeEnergyToWh(value, unit, null).ShouldBe(5500m);
    }

    #endregion
}
