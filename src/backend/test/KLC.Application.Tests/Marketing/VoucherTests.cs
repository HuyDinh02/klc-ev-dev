using System;
using KLC.Enums;
using KLC.Marketing;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Marketing;

/// <summary>
/// Tests for Voucher domain entity behavior.
/// Validates creation, validity checks, usage limits, and discount logic.
/// </summary>
public class VoucherTests
{
    private static Voucher CreateTestVoucher(
        string code = "SUMMER50",
        VoucherType type = VoucherType.FixedAmount,
        decimal value = 50000m,
        int totalQuantity = 100,
        int daysUntilExpiry = 30,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null)
    {
        return new Voucher(
            Guid.NewGuid(),
            code,
            type,
            value,
            DateTime.UtcNow.AddDays(daysUntilExpiry),
            totalQuantity,
            minOrderAmount,
            maxDiscountAmount);
    }

    [Fact]
    public void Create_Voucher_Should_Set_Default_Values()
    {
        var voucher = CreateTestVoucher();

        voucher.Code.ShouldBe("SUMMER50");
        voucher.Type.ShouldBe(VoucherType.FixedAmount);
        voucher.Value.ShouldBe(50000m);
        voucher.TotalQuantity.ShouldBe(100);
        voucher.UsedQuantity.ShouldBe(0);
        voucher.IsActive.ShouldBeTrue();
        voucher.MinOrderAmount.ShouldBeNull();
        voucher.MaxDiscountAmount.ShouldBeNull();
    }

    [Fact]
    public void Create_Voucher_With_MinOrder_And_MaxDiscount()
    {
        var voucher = CreateTestVoucher(
            code: "PERCENT20",
            type: VoucherType.Percentage,
            value: 20m,
            minOrderAmount: 100000m,
            maxDiscountAmount: 50000m);

        voucher.Type.ShouldBe(VoucherType.Percentage);
        voucher.Value.ShouldBe(20m);
        voucher.MinOrderAmount.ShouldBe(100000m);
        voucher.MaxDiscountAmount.ShouldBe(50000m);
    }

    [Fact]
    public void Create_Voucher_With_Empty_Code_Should_Throw()
    {
        Should.Throw<Exception>(() =>
            CreateTestVoucher(code: ""));
    }

    [Fact]
    public void IsValid_Should_Return_True_For_Active_NonExpired_WithCapacity()
    {
        var voucher = CreateTestVoucher(daysUntilExpiry: 30);

        voucher.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Deactivated()
    {
        var voucher = CreateTestVoucher();

        voucher.Deactivate();

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Expired()
    {
        var voucher = CreateTestVoucher(daysUntilExpiry: -1);

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Fully_Used()
    {
        var voucher = CreateTestVoucher(totalQuantity: 1);

        voucher.IncrementUsage();

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IncrementUsage_Should_Increase_UsedQuantity()
    {
        var voucher = CreateTestVoucher(totalQuantity: 10);

        voucher.IncrementUsage();
        voucher.UsedQuantity.ShouldBe(1);

        voucher.IncrementUsage();
        voucher.UsedQuantity.ShouldBe(2);
    }

    [Fact]
    public void IncrementUsage_Should_Throw_When_Not_Valid()
    {
        var voucher = CreateTestVoucher(totalQuantity: 1);
        voucher.IncrementUsage(); // Uses the single available

        var ex = Should.Throw<BusinessException>(() => voucher.IncrementUsage());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotValid);
    }

    [Fact]
    public void IncrementUsage_Should_Throw_When_Deactivated()
    {
        var voucher = CreateTestVoucher();
        voucher.Deactivate();

        var ex = Should.Throw<BusinessException>(() => voucher.IncrementUsage());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotValid);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var voucher = CreateTestVoucher();
        voucher.IsActive.ShouldBeTrue();

        voucher.Deactivate();

        voucher.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Activate_Should_Set_IsActive_True()
    {
        var voucher = CreateTestVoucher();
        voucher.Deactivate();
        voucher.IsActive.ShouldBeFalse();

        voucher.Activate();

        voucher.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Update_Should_Change_Specified_Fields()
    {
        var voucher = CreateTestVoucher();
        var newExpiry = DateTime.UtcNow.AddDays(90);

        voucher.Update(
            description: "Updated description",
            expiryDate: newExpiry,
            totalQuantity: 500,
            isActive: null);

        voucher.Description.ShouldBe("Updated description");
        voucher.TotalQuantity.ShouldBe(500);
        voucher.IsActive.ShouldBeTrue(); // Unchanged when null
    }

    [Fact]
    public void Update_With_IsActive_False_Should_Deactivate()
    {
        var voucher = CreateTestVoucher();

        voucher.Update(
            description: null,
            expiryDate: null,
            totalQuantity: null,
            isActive: false);

        voucher.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void FreeCharging_Voucher_Should_Accept_Zero_Value()
    {
        var voucher = CreateTestVoucher(
            code: "FREE01",
            type: VoucherType.FreeCharging,
            value: 0m);

        voucher.Type.ShouldBe(VoucherType.FreeCharging);
        voucher.Value.ShouldBe(0m);
        voucher.IsValid().ShouldBeTrue();
    }
}
