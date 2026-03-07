using System;
using System.Collections.Generic;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Tariffs;

public class TariffPlanTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var tariff = new TariffPlan(id, "Standard", 3500, 10, effectiveFrom, description: "Standard plan");

        tariff.Id.ShouldBe(id);
        tariff.Name.ShouldBe("Standard");
        tariff.BaseRatePerKwh.ShouldBe(3500m);
        tariff.TaxRatePercent.ShouldBe(10m);
        tariff.EffectiveFrom.ShouldBe(effectiveFrom);
        tariff.EffectiveTo.ShouldBeNull();
        tariff.Description.ShouldBe("Standard plan");
        tariff.IsActive.ShouldBeTrue();
        tariff.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void SetBaseRate_Should_Throw_On_Negative()
    {
        var tariff = CreateTariff();

        var ex = Should.Throw<BusinessException>(() => tariff.SetBaseRate(-1));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Tariff.InvalidBaseRate);
    }

    [Fact]
    public void SetBaseRate_Should_Accept_Zero()
    {
        var tariff = CreateTariff();

        tariff.SetBaseRate(0);

        tariff.BaseRatePerKwh.ShouldBe(0m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void SetTaxRate_Should_Throw_On_Out_Of_Range(decimal taxRate)
    {
        var tariff = CreateTariff();

        var ex = Should.Throw<BusinessException>(() => tariff.SetTaxRate(taxRate));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Tariff.InvalidTaxRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void SetTaxRate_Should_Accept_Valid_Values(decimal taxRate)
    {
        var tariff = CreateTariff();

        tariff.SetTaxRate(taxRate);

        tariff.TaxRatePercent.ShouldBe(taxRate);
    }

    [Fact]
    public void SetEffectivePeriod_Should_Throw_When_End_Before_Start()
    {
        var tariff = CreateTariff();
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var ex = Should.Throw<BusinessException>(() => tariff.SetEffectivePeriod(start, end));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Tariff.InvalidEffectivePeriod);
    }

    [Fact]
    public void SetEffectivePeriod_Should_Accept_Null_End()
    {
        var tariff = CreateTariff();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        tariff.SetEffectivePeriod(start, null);

        tariff.EffectiveFrom.ShouldBe(start);
        tariff.EffectiveTo.ShouldBeNull();
    }

    [Fact]
    public void GetTotalRatePerKwh_Should_Include_Tax()
    {
        var tariff = CreateTariff(baseRate: 3500, taxRate: 10);

        var total = tariff.GetTotalRatePerKwh();

        total.ShouldBe(3850m); // 3500 * 1.10
    }

    [Fact]
    public void Deactivate_Should_Remove_Default()
    {
        var tariff = CreateTariff();
        tariff.SetAsDefault();
        tariff.IsDefault.ShouldBeTrue();

        tariff.Deactivate();

        tariff.IsActive.ShouldBeFalse();
        tariff.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyEffective_Should_Return_True_When_Active_And_In_Period()
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(),
            "Test",
            3500,
            10,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30));

        tariff.IsCurrentlyEffective().ShouldBeTrue();
    }

    [Fact]
    public void IsCurrentlyEffective_Should_Return_False_When_Deactivated()
    {
        var tariff = new TariffPlan(
            Guid.NewGuid(),
            "Test",
            3500,
            10,
            DateTime.UtcNow.AddDays(-1));
        tariff.Deactivate();

        tariff.IsCurrentlyEffective().ShouldBeFalse();
    }

    [Fact]
    public void SetTouRates_Should_Set_TimeOfUse_Type()
    {
        var tariff = CreateTariff();

        tariff.SetTouRates(2500, 3500, 4500);

        tariff.TariffType.ShouldBe(TariffType.TimeOfUse);
        tariff.OffPeakRatePerKwh.ShouldBe(2500m);
        tariff.NormalRatePerKwh.ShouldBe(3500m);
        tariff.PeakRatePerKwh.ShouldBe(4500m);
    }

    [Fact]
    public void SetFlatRate_Should_Clear_Tou_Fields()
    {
        var tariff = CreateTariff();
        tariff.SetTouRates(2500, 3500, 4500);

        tariff.SetFlatRate();

        tariff.TariffType.ShouldBe(TariffType.Flat);
        tariff.OffPeakRatePerKwh.ShouldBeNull();
        tariff.NormalRatePerKwh.ShouldBeNull();
        tariff.PeakRatePerKwh.ShouldBeNull();
    }

    [Theory]
    [InlineData(16, 0, "OffPeak")]    // 23:00 VN = 16:00 UTC
    [InlineData(0, 0, "Normal")]      // 07:00 VN = 00:00 UTC
    [InlineData(10, 0, "Peak")]       // 17:00 VN = 10:00 UTC
    [InlineData(14, 0, "Normal")]     // 21:00 VN = 14:00 UTC
    [InlineData(22, 30, "OffPeak")]   // 05:30 VN = 22:30 UTC (previous day)
    public void GetTierName_Should_Map_Utc_To_Vietnam_Tiers(int utcHour, int utcMinute, string expectedTier)
    {
        var time = new DateTime(2026, 6, 15, utcHour, utcMinute, 0, DateTimeKind.Utc);

        var tier = TariffPlan.GetTierName(time);

        tier.ShouldBe(expectedTier);
    }

    [Fact]
    public void GetRateForTime_Flat_Should_Return_Flat_Rate()
    {
        var tariff = CreateTariff(baseRate: 3500, taxRate: 10);

        // Should return flat rate regardless of time
        var peakTime = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc); // 17:00 VN = peak
        var offPeakTime = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc); // 23:00 VN = off-peak

        tariff.GetRateForTime(peakTime).ShouldBe(3850m);
        tariff.GetRateForTime(offPeakTime).ShouldBe(3850m);
    }

    [Fact]
    public void GetRateForTime_Tou_Should_Return_Tier_Rate()
    {
        var tariff = CreateTariff(baseRate: 3500, taxRate: 10);
        tariff.SetTouRates(2500, 3500, 4500);

        var offPeakTime = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc); // 23:00 VN
        var normalTime = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);   // 07:00 VN
        var peakTime = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);    // 17:00 VN

        tariff.GetRateForTime(offPeakTime).ShouldBe(2750m); // 2500 * 1.10
        tariff.GetRateForTime(normalTime).ShouldBe(3850m);  // 3500 * 1.10
        tariff.GetRateForTime(peakTime).ShouldBe(4950m);    // 4500 * 1.10
    }

    [Fact]
    public void CalculateTouCost_Should_Apportion_Energy_By_Tier()
    {
        var tariff = CreateTariff(baseRate: 3500, taxRate: 0);
        tariff.SetTouRates(2500, 3500, 4500);

        // Simulate charging from 16:00 UTC (23:00 VN off-peak) to 17:00 UTC (00:00 VN off-peak)
        // All energy in off-peak tier
        var meterValues = new List<(DateTime, decimal)>
        {
            (new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc), 0m),    // 23:00 VN
            (new DateTime(2026, 6, 15, 16, 30, 0, DateTimeKind.Utc), 5m),   // 23:30 VN
            (new DateTime(2026, 6, 15, 17, 0, 0, DateTimeKind.Utc), 10m),   // 00:00 VN
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 10000, null, null);

        breakdown.OffPeakKwh.ShouldBe(10m);
        breakdown.NormalKwh.ShouldBe(0m);
        breakdown.PeakKwh.ShouldBe(0m);
        breakdown.TotalCost.ShouldBe(25000m); // 10kWh * 2500đ
    }

    [Fact]
    public void CalculateTouCost_Mixed_Tiers_Should_Split_Correctly()
    {
        var tariff = CreateTariff(baseRate: 3500, taxRate: 0);
        tariff.SetTouRates(2500, 3500, 4500);

        // Simulate charging across peak and normal hours
        // 10:00 UTC = 17:00 VN (peak), 14:00 UTC = 21:00 VN (normal)
        var meterValues = new List<(DateTime, decimal)>
        {
            (new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc), 0m),    // 17:00 VN peak
            (new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), 10m),   // 19:00 VN peak
            (new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc), 20m),   // 21:00 VN normal
            (new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), 25m),   // 22:00 VN normal
        };

        var breakdown = tariff.CalculateTouCost(meterValues, 0, 25000, null, null);

        // First interval (10-12 UTC): midpoint 11:00 UTC = 18:00 VN = peak, 10 kWh
        // Second interval (12-14 UTC): midpoint 13:00 UTC = 20:00 VN = peak, 10 kWh
        // Third interval (14-15 UTC): midpoint 14:30 UTC = 21:30 VN = normal, 5 kWh
        breakdown.PeakKwh.ShouldBe(20m);
        breakdown.NormalKwh.ShouldBe(5m);
        breakdown.OffPeakKwh.ShouldBe(0m);
        breakdown.PeakCost.ShouldBe(90000m);   // 20 * 4500
        breakdown.NormalCost.ShouldBe(17500m);  // 5 * 3500
        breakdown.TotalCost.ShouldBe(107500m);
    }

    private static TariffPlan CreateTariff(decimal baseRate = 3500, decimal taxRate = 10)
    {
        return new TariffPlan(
            Guid.NewGuid(),
            "Test Tariff",
            baseRate,
            taxRate,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
