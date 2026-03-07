using System;
using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.Ocpp;

public class VendorProfileTests
{
    private readonly GenericProfile _generic;
    private readonly ChargecoreGlobalProfile _chargecore;
    private readonly JuhangProfile _juhang;
    private readonly VendorProfileFactory _factory;

    public VendorProfileTests()
    {
        _generic = new GenericProfile(NullLogger<GenericProfile>.Instance);
        _chargecore = new ChargecoreGlobalProfile(NullLogger<ChargecoreGlobalProfile>.Instance);
        _juhang = new JuhangProfile(NullLogger<JuhangProfile>.Instance);

        var profiles = new List<IVendorProfile> { _generic, _chargecore, _juhang };
        _factory = new VendorProfileFactory(profiles, NullLogger<VendorProfileFactory>.Instance);
    }

    #region Factory Detection

    [Fact]
    public void Factory_Should_Detect_ChargecoreGlobal()
    {
        var profile = _factory.Detect("Chargecore", "AC-22");
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    [Fact]
    public void Factory_Should_Detect_ChargecoreGlobal_CCG()
    {
        var profile = _factory.Detect("CCG", "DC-50");
        profile.ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
    }

    [Fact]
    public void Factory_Should_Detect_Juhang()
    {
        var profile = _factory.Detect("JUHANG", "JH-DC120");
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Factory_Should_Detect_Juhang_CaseInsensitive()
    {
        var profile = _factory.Detect("JuHang Technology", "Model X");
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Factory_Should_Detect_Juhang_WithSpace()
    {
        var profile = _factory.Detect("Ju Hang", "Model Y");
        profile.ProfileType.ShouldBe(VendorProfileType.Juhang);
    }

    [Fact]
    public void Factory_Should_FallBack_To_Generic()
    {
        var profile = _factory.Detect("UnknownVendor", "Model Z");
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Factory_Should_FallBack_To_Generic_For_Null_Vendor()
    {
        var profile = _factory.Detect(null, null);
        profile.ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    [Fact]
    public void Factory_Resolve_Should_Return_Correct_Profile()
    {
        _factory.Resolve(VendorProfileType.Juhang).ProfileType.ShouldBe(VendorProfileType.Juhang);
        _factory.Resolve(VendorProfileType.ChargecoreGlobal).ProfileType.ShouldBe(VendorProfileType.ChargecoreGlobal);
        _factory.Resolve(VendorProfileType.Generic).ProfileType.ShouldBe(VendorProfileType.Generic);
    }

    #endregion

    #region Generic Profile - Energy Normalization

    [Fact]
    public void Generic_NormalizeEnergy_Wh()
    {
        _generic.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);
    }

    [Fact]
    public void Generic_NormalizeEnergy_KWh()
    {
        _generic.NormalizeEnergyToWh("5.5", "kWh", null).ShouldBe(5500m);
    }

    [Fact]
    public void Generic_NormalizeEnergy_NoUnit_LargeValue_InferWh()
    {
        _generic.NormalizeEnergyToWh("5000", "", null).ShouldBe(5000m);
    }

    [Fact]
    public void Generic_NormalizeEnergy_NoUnit_SmallValue_InferKWh()
    {
        _generic.NormalizeEnergyToWh("5.5", "", null).ShouldBe(5500m);
    }

    [Fact]
    public void Generic_NormalizeEnergy_InvalidValue_ReturnsZero()
    {
        _generic.NormalizeEnergyToWh("abc", "Wh", null).ShouldBe(0m);
    }

    #endregion

    #region Generic Profile - Power Normalization

    [Fact]
    public void Generic_NormalizePower_W()
    {
        _generic.NormalizePowerToW("7200", "W").ShouldBe(7200m);
    }

    [Fact]
    public void Generic_NormalizePower_KW()
    {
        _generic.NormalizePowerToW("7.2", "kW").ShouldBe(7200m);
    }

    [Fact]
    public void Generic_NormalizePower_NoUnit_LargeValue_InferW()
    {
        _generic.NormalizePowerToW("7200", "").ShouldBe(7200m);
    }

    [Fact]
    public void Generic_NormalizePower_NoUnit_SmallValue_InferKW()
    {
        _generic.NormalizePowerToW("7.2", "").ShouldBe(7200m);
    }

    #endregion

    #region Chargecore Global - Energy

    [Fact]
    public void Chargecore_NormalizeEnergy_NoUnit_AssumesKWh()
    {
        // Chargecore always assumes kWh when unit is missing
        _chargecore.NormalizeEnergyToWh("5.5", "", null).ShouldBe(5500m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_KWh()
    {
        _chargecore.NormalizeEnergyToWh("10.0", "kWh", null).ShouldBe(10000m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_Wh()
    {
        _chargecore.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);
    }

    [Fact]
    public void Chargecore_NormalizeEnergy_UnknownUnit_AssumesKWh()
    {
        _chargecore.NormalizeEnergyToWh("3.0", "MWh", null).ShouldBe(3000m);
    }

    #endregion

    #region JUHANG - Energy

    [Fact]
    public void Juhang_NormalizeEnergy_Wh()
    {
        _juhang.NormalizeEnergyToWh("5000", "Wh", null).ShouldBe(5000m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_NoUnit_LargeValue_AssumesWh()
    {
        _juhang.NormalizeEnergyToWh("5000", "", null).ShouldBe(5000m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_NoUnit_SmallValue_AssumesKWh()
    {
        // JUHANG: < 100 with no unit → kWh
        _juhang.NormalizeEnergyToWh("5.5", "", null).ShouldBe(5500m);
    }

    [Fact]
    public void Juhang_NormalizeEnergy_KWh()
    {
        _juhang.NormalizeEnergyToWh("10.0", "kWh", null).ShouldBe(10000m);
    }

    #endregion

    #region JUHANG - Timestamp Parsing

    [Fact]
    public void Juhang_ParseTimestamp_SpaceSeparator()
    {
        var result = _juhang.ParseTimestamp("2026-03-05 14:30:00");
        result.ShouldNotBeNull();
        result.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(14);
        result.Value.Minute.ShouldBe(30);
    }

    [Fact]
    public void Juhang_ParseTimestamp_ISO8601()
    {
        var result = _juhang.ParseTimestamp("2026-03-05T14:30:00Z");
        result.ShouldNotBeNull();
        result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Juhang_ParseTimestamp_Null_ReturnsNull()
    {
        _juhang.ParseTimestamp(null).ShouldBeNull();
    }

    [Fact]
    public void Juhang_ParseTimestamp_Empty_ReturnsNull()
    {
        _juhang.ParseTimestamp("").ShouldBeNull();
    }

    #endregion

    #region Generic - Timestamp Parsing

    [Fact]
    public void Generic_ParseTimestamp_ISO8601_WithZ()
    {
        var result = _generic.ParseTimestamp("2026-03-05T14:30:00Z");
        result.ShouldNotBeNull();
        result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Generic_ParseTimestamp_ISO8601_WithMillis()
    {
        var result = _generic.ParseTimestamp("2026-03-05T14:30:00.123Z");
        result.ShouldNotBeNull();
        result.Value.Millisecond.ShouldBe(123);
    }

    [Fact]
    public void Generic_ParseTimestamp_ISO8601_WithOffset()
    {
        var result = _generic.ParseTimestamp("2026-03-05T14:30:00+07:00");
        result.ShouldNotBeNull();
        result.Value.Kind.ShouldBe(DateTimeKind.Utc);
        result.Value.Hour.ShouldBe(7); // 14:30 +07:00 = 07:30 UTC
    }

    [Fact]
    public void Generic_ParseTimestamp_Invalid_ReturnsNull()
    {
        _generic.ParseTimestamp("not a date").ShouldBeNull();
    }

    #endregion

    #region Vendor Matching

    [Fact]
    public void Generic_MatchesVendor_Always_False()
    {
        _generic.MatchesVendor("anything", "anything").ShouldBeFalse();
    }

    [Theory]
    [InlineData("Chargecore", "AC-22", true)]
    [InlineData("ChargeCore Global", "DC-50", true)]
    [InlineData("CCG", null, true)]
    [InlineData("ccg", "test", true)]
    [InlineData("CHARGECORE", null, true)]
    [InlineData("OtherVendor", null, false)]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    public void Chargecore_MatchesVendor(string? vendor, string? model, bool expected)
    {
        _chargecore.MatchesVendor(vendor, model).ShouldBe(expected);
    }

    [Theory]
    [InlineData("JUHANG", "JH-DC120", true)]
    [InlineData("JuHang", null, true)]
    [InlineData("Ju Hang Technology", "Model X", true)]
    [InlineData("juhang", null, true)]
    [InlineData("OtherVendor", null, false)]
    [InlineData(null, null, false)]
    public void Juhang_MatchesVendor(string? vendor, string? model, bool expected)
    {
        _juhang.MatchesVendor(vendor, model).ShouldBe(expected);
    }

    #endregion

    #region Profile Properties

    [Fact]
    public void Juhang_MeterValuesMayOmitTransactionId_True()
    {
        _juhang.MeterValuesMayOmitTransactionId.ShouldBeTrue();
    }

    [Fact]
    public void Generic_MeterValuesMayOmitTransactionId_False()
    {
        _generic.MeterValuesMayOmitTransactionId.ShouldBeFalse();
    }

    [Fact]
    public void Chargecore_MayRetryStartTransaction_True()
    {
        _chargecore.MayRetryStartTransaction.ShouldBeTrue();
    }

    [Fact]
    public void Juhang_MayRetryStartTransaction_True()
    {
        _juhang.MayRetryStartTransaction.ShouldBeTrue();
    }

    [Fact]
    public void Generic_MayRetryStartTransaction_False()
    {
        _generic.MayRetryStartTransaction.ShouldBeFalse();
    }

    #endregion
}
