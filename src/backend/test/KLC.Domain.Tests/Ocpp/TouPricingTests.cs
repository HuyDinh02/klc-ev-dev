using System;
using System.Collections.Generic;
using KLC.Enums;
using KLC.Tariffs;
using Shouldly;
using Xunit;

namespace KLC.Ocpp;

/// <summary>
/// Tests for TOU (Time-of-Use) pricing integration in the OCPP charging flow.
/// Validates the 3-tier Vietnam EVN schedule:
///   - Off-peak: 23:00–06:00 VN time (UTC 16:00–23:00)
///   - Normal:   06:00–17:00 and 21:00–23:00 VN time
///   - Peak:     17:00–21:00 VN time (UTC 10:00–14:00)
/// </summary>
public class TouPricingTests
{
    private TariffPlan CreateTouTariff(
        decimal offPeakRate = 1500m,
        decimal normalRate = 3500m,
        decimal peakRate = 5000m,
        decimal taxPercent = 10m)
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(),
            "TOU Tariff",
            normalRate, // Base rate (used as fallback)
            taxPercent,
            DateTime.UtcNow.AddYears(-1));
        tariff.SetTouRates(offPeakRate, normalRate, peakRate);
        return tariff;
    }

    private TariffPlan CreateFlatTariff(decimal rate = 3500m, decimal taxPercent = 10m)
    {
        return new TariffPlan(
            Guid.NewGuid(),
            "Flat Tariff",
            rate,
            taxPercent,
            DateTime.UtcNow.AddYears(-1));
    }

    #region TOU Tier Classification

    [Theory]
    // Off-peak: 23:00–06:00 VN time = 16:00–23:00 UTC
    [InlineData(16, 0, "OffPeak")]  // 23:00 VN
    [InlineData(20, 0, "OffPeak")]  // 03:00 VN
    [InlineData(22, 59, "OffPeak")] // 05:59 VN
    // Normal: 06:00–17:00 VN = 23:00–10:00 UTC
    [InlineData(23, 0, "Normal")]   // 06:00 VN
    [InlineData(0, 0, "Normal")]    // 07:00 VN
    [InlineData(5, 0, "Normal")]    // 12:00 VN
    [InlineData(9, 59, "Normal")]   // 16:59 VN
    // Peak: 17:00–21:00 VN = 10:00–14:00 UTC
    [InlineData(10, 0, "Peak")]     // 17:00 VN
    [InlineData(12, 0, "Peak")]     // 19:00 VN
    [InlineData(13, 59, "Peak")]    // 20:59 VN
    // Normal: 21:00–23:00 VN = 14:00–16:00 UTC
    [InlineData(14, 0, "Normal")]   // 21:00 VN
    [InlineData(15, 0, "Normal")]   // 22:00 VN
    [InlineData(15, 59, "Normal")]  // 22:59 VN
    public void GetTierName_ShouldReturn_CorrectTier(int utcHour, int utcMinute, string expectedTier)
    {
        var utcTime = new DateTime(2026, 6, 15, utcHour, utcMinute, 0, DateTimeKind.Utc);
        TariffPlan.GetTierName(utcTime).ShouldBe(expectedTier);
    }

    #endregion

    #region GetRateForTime

    [Fact]
    public void GetRateForTime_FlatTariff_SameRateRegardlessOfTime()
    {
        var tariff = CreateFlatTariff(3500m, 10m);

        // Check different times of day
        var peakTime = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc); // 19:00 VN (Peak)
        var offPeakTime = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc); // 03:00 VN (OffPeak)
        var normalTime = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc); // 12:00 VN (Normal)

        var expectedRate = 3500m * 1.10m; // 3850

        tariff.GetRateForTime(peakTime).ShouldBe(expectedRate);
        tariff.GetRateForTime(offPeakTime).ShouldBe(expectedRate);
        tariff.GetRateForTime(normalTime).ShouldBe(expectedRate);
    }

    [Fact]
    public void GetRateForTime_TouTariff_OffPeakRate()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // 03:00 VN = 20:00 UTC → Off-peak
        var time = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc);
        tariff.GetRateForTime(time).ShouldBe(1500m * 1.10m); // 1650
    }

    [Fact]
    public void GetRateForTime_TouTariff_NormalRate()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // 12:00 VN = 05:00 UTC → Normal
        var time = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc);
        tariff.GetRateForTime(time).ShouldBe(3500m * 1.10m); // 3850
    }

    [Fact]
    public void GetRateForTime_TouTariff_PeakRate()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // 19:00 VN = 12:00 UTC → Peak
        var time = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        tariff.GetRateForTime(time).ShouldBe(5000m * 1.10m); // 5500
    }

    #endregion

    #region CalculateTouCost — Integration Tests

    [Fact]
    public void CalculateTouCost_AllOffPeak()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Charging from 01:00–03:00 VN (off-peak) = 18:00–20:00 UTC
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc), 0m),
            (new DateTime(2026, 6, 15, 19, 0, 0, DateTimeKind.Utc), 5m),
            (new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc), 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        breakdown.OffPeakKwh.ShouldBe(10m);
        breakdown.NormalKwh.ShouldBe(0m);
        breakdown.PeakKwh.ShouldBe(0m);
        breakdown.TotalCost.ShouldBe(Math.Round(10m * 1500m * 1.10m, 0)); // 16500
    }

    [Fact]
    public void CalculateTouCost_AllPeak()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Charging from 18:00–20:00 VN (peak) = 11:00–13:00 UTC
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc), 0m),
            (new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), 5m),
            (new DateTime(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc), 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        breakdown.PeakKwh.ShouldBe(10m);
        breakdown.OffPeakKwh.ShouldBe(0m);
        breakdown.NormalKwh.ShouldBe(0m);
        breakdown.TotalCost.ShouldBe(Math.Round(10m * 5000m * 1.10m, 0)); // 55000
    }

    [Fact]
    public void CalculateTouCost_AllNormal()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Charging from 10:00–12:00 VN (normal) = 03:00–05:00 UTC
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 3, 0, 0, DateTimeKind.Utc), 0m),
            (new DateTime(2026, 6, 15, 4, 0, 0, DateTimeKind.Utc), 5m),
            (new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc), 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        breakdown.NormalKwh.ShouldBe(10m);
        breakdown.OffPeakKwh.ShouldBe(0m);
        breakdown.PeakKwh.ShouldBe(0m);
        breakdown.TotalCost.ShouldBe(Math.Round(10m * 3500m * 1.10m, 0)); // 38500
    }

    [Fact]
    public void CalculateTouCost_MixedTiers()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Session spanning Normal → Peak → Normal
        // VN time: 16:00 → 17:00 → 18:00 → 19:00 → 20:00 → 21:00 → 22:00
        // UTC:     09:00 → 10:00 → 11:00 → 12:00 → 13:00 → 14:00 → 15:00
        // Tier:    Normal → Peak  → Peak  → Peak  → Peak  → Normal → Normal
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc), 0m),   // 16:00 VN Normal
            (new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc), 2m),  // 17:00 VN → midpoint 16:30 Normal
            (new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), 6m),  // 19:00 VN → midpoint 18:00 Peak
            (new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc), 10m), // 21:00 VN → midpoint 20:00 Peak
            (new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), 12m), // 22:00 VN → midpoint 21:30 Normal
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 12000, null, null);

        // Midpoints:
        // 09:00→10:00: midpoint 09:30 UTC = 16:30 VN → Normal, delta = 2 kWh
        // 10:00→12:00: midpoint 11:00 UTC = 18:00 VN → Peak, delta = 4 kWh
        // 12:00→14:00: midpoint 13:00 UTC = 20:00 VN → Peak, delta = 4 kWh
        // 14:00→15:00: midpoint 14:30 UTC = 21:30 VN → Normal, delta = 2 kWh
        breakdown.NormalKwh.ShouldBe(4m);  // 2 + 2
        breakdown.PeakKwh.ShouldBe(8m);    // 4 + 4
        breakdown.OffPeakKwh.ShouldBe(0m);

        var expectedNormalCost = Math.Round(4m * 3500m * 1.10m, 0);  // 15400
        var expectedPeakCost = Math.Round(8m * 5000m * 1.10m, 0);    // 44000
        breakdown.TotalCost.ShouldBe(expectedNormalCost + expectedPeakCost); // 59400
    }

    [Fact]
    public void CalculateTouCost_CrossMidnight()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Session spanning 22:00–02:00 VN (Normal → OffPeak)
        // UTC: 15:00–19:00
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), 0m),   // 22:00 VN Normal
            (new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc), 3m),   // 23:00 VN → midpoint 22:30 Normal
            (new DateTime(2026, 6, 15, 17, 0, 0, DateTimeKind.Utc), 6m),   // 00:00 VN → midpoint 23:30 OffPeak
            (new DateTime(2026, 6, 15, 19, 0, 0, DateTimeKind.Utc), 12m),  // 02:00 VN → midpoint 01:00 OffPeak
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 12000, null, null);

        // Midpoints:
        // 15:00→16:00: midpoint 15:30 UTC = 22:30 VN → Normal (hour 22, not >=23 not <6)
        // 16:00→17:00: midpoint 16:30 UTC = 23:30 VN → OffPeak (hour 23, >=23)
        // 17:00→19:00: midpoint 18:00 UTC = 01:00 VN → OffPeak (hour 1, <6)
        breakdown.NormalKwh.ShouldBe(3m);
        breakdown.OffPeakKwh.ShouldBe(9m); // 3 + 6
    }

    [Fact]
    public void CalculateTouCost_FlatTariff_FallsBack()
    {
        var tariff = CreateFlatTariff(3500m, 10m);

        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (DateTime.UtcNow.AddHours(-2), 0m),
            (DateTime.UtcNow.AddHours(-1), 5m),
            (DateTime.UtcNow, 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        // Flat tariff: uses simple (meterStop - meterStart) / 1000 * flat rate
        breakdown.TotalCost.ShouldBe(Math.Round(10m * 3500m * 1.10m, 0)); // 38500
    }

    [Fact]
    public void CalculateTouCost_SingleMeterValue_FallsToFlat()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        // Only 1 meter value — not enough for TOU calculation
        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (DateTime.UtcNow, 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        // Falls back to flat: (10000 - 0) / 1000 * totalRate
        breakdown.TotalCost.ShouldBe(Math.Round(10m * 3500m * 1.10m, 0)); // Uses base rate for flat fallback
    }

    [Fact]
    public void CalculateTouCost_ZeroEnergyDelta_SkipsSegment()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc), 5m),
            (new DateTime(2026, 6, 15, 5, 30, 0, DateTimeKind.Utc), 5m),  // Zero delta — skipped
            (new DateTime(2026, 6, 15, 6, 0, 0, DateTimeKind.Utc), 10m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 5000, 10000, null, null);

        // Only the 5→10 kWh segment counts
        breakdown.NormalKwh.ShouldBe(5m);
    }

    [Fact]
    public void CalculateTouCost_NegativeDelta_SkipsSegment()
    {
        var tariff = CreateTouTariff(1500m, 3500m, 5000m, 10m);

        var meterValues = new List<(DateTime Timestamp, decimal EnergyKwh)>
        {
            (new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc), 10m),
            (new DateTime(2026, 6, 15, 5, 30, 0, DateTimeKind.Utc), 8m),   // Negative delta — skipped
            (new DateTime(2026, 6, 15, 6, 0, 0, DateTimeKind.Utc), 15m),
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 10000, 15000, null, null);

        // Only the 8→15 kWh segment counts (delta = 7)
        breakdown.NormalKwh.ShouldBe(7m);
    }

    #endregion

    #region TariffPlan Configuration

    [Fact]
    public void SetTouRates_ShouldChange_TariffType()
    {
        var tariff = CreateFlatTariff();
        tariff.TariffType.ShouldBe(TariffType.Flat);

        tariff.SetTouRates(1500m, 3500m, 5000m);
        tariff.TariffType.ShouldBe(TariffType.TimeOfUse);
        tariff.OffPeakRatePerKwh.ShouldBe(1500m);
        tariff.NormalRatePerKwh.ShouldBe(3500m);
        tariff.PeakRatePerKwh.ShouldBe(5000m);
    }

    [Fact]
    public void SetFlatRate_ShouldClear_TouRates()
    {
        var tariff = CreateTouTariff();
        tariff.TariffType.ShouldBe(TariffType.TimeOfUse);

        tariff.SetFlatRate();
        tariff.TariffType.ShouldBe(TariffType.Flat);
        tariff.OffPeakRatePerKwh.ShouldBeNull();
        tariff.NormalRatePerKwh.ShouldBeNull();
        tariff.PeakRatePerKwh.ShouldBeNull();
    }

    [Fact]
    public void SetTouRates_NegativeRate_ShouldThrow()
    {
        var tariff = CreateFlatTariff();

        Should.Throw<Volo.Abp.BusinessException>(() =>
            tariff.SetTouRates(-100m, 3500m, 5000m));
    }

    [Fact]
    public void IsCurrentlyEffective_ActiveAndWithinPeriod_ReturnsTrue()
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(), "Test", 3500m, 10m,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30));

        tariff.IsCurrentlyEffective().ShouldBeTrue();
    }

    [Fact]
    public void IsCurrentlyEffective_Expired_ReturnsFalse()
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(), "Test", 3500m, 10m,
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-1));

        tariff.IsCurrentlyEffective().ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyEffective_Deactivated_ReturnsFalse()
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(), "Test", 3500m, 10m,
            DateTime.UtcNow.AddDays(-30));
        tariff.Deactivate();

        tariff.IsCurrentlyEffective().ShouldBeFalse();
    }

    #endregion
}
