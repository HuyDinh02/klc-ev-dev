using System;
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
