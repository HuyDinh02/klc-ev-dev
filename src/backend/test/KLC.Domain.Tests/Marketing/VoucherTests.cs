using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Marketing;

public class VoucherTests
{
    [Fact]
    public void Constructor_Should_Initialize_Correctly()
    {
        var id = Guid.NewGuid();
        var expiryDate = DateTime.UtcNow.AddDays(30);

        var voucher = CreateVoucher(id: id, expiryDate: expiryDate);

        voucher.Id.ShouldBe(id);
        voucher.Code.ShouldBe("SUMMER2026");
        voucher.Type.ShouldBe(VoucherType.FixedAmount);
        voucher.Value.ShouldBe(50000m);
        voucher.ExpiryDate.ShouldBe(expiryDate);
        voucher.TotalQuantity.ShouldBe(100);
        voucher.UsedQuantity.ShouldBe(0);
        voucher.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Should_Set_Optional_Fields()
    {
        var voucher = new Voucher(
            Guid.NewGuid(),
            "PERCENT10",
            VoucherType.Percentage,
            10m,
            DateTime.UtcNow.AddDays(30),
            50,
            minOrderAmount: 100000m,
            maxDiscountAmount: 20000m,
            description: "10% off");

        voucher.MinOrderAmount.ShouldBe(100000m);
        voucher.MaxDiscountAmount.ShouldBe(20000m);
        voucher.Description.ShouldBe("10% off");
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_Code()
    {
        Should.Throw<ArgumentException>(() =>
            new Voucher(
                Guid.NewGuid(),
                "",
                VoucherType.FixedAmount,
                50000m,
                DateTime.UtcNow.AddDays(30),
                100));
    }

    [Fact]
    public void IsValid_Should_Return_True_When_Active_Not_Expired_Has_Quantity()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Inactive()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));
        voucher.Deactivate();

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Expired()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(-1));

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_No_Quantity_Left()
    {
        var voucher = CreateVoucher(
            expiryDate: DateTime.UtcNow.AddDays(30),
            totalQuantity: 1);

        voucher.IncrementUsage();

        voucher.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IncrementUsage_Should_Increment_UsedQuantity()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.IncrementUsage();

        voucher.UsedQuantity.ShouldBe(1);
    }

    [Fact]
    public void IncrementUsage_Should_Throw_When_Not_Valid_Inactive()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));
        voucher.Deactivate();

        var ex = Should.Throw<BusinessException>(() => voucher.IncrementUsage());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotValid);
    }

    [Fact]
    public void IncrementUsage_Should_Throw_When_Not_Valid_Expired()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(-1));

        var ex = Should.Throw<BusinessException>(() => voucher.IncrementUsage());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotValid);
    }

    [Fact]
    public void IncrementUsage_Should_Throw_When_Not_Valid_No_Quantity()
    {
        var voucher = CreateVoucher(
            expiryDate: DateTime.UtcNow.AddDays(30),
            totalQuantity: 1);
        voucher.IncrementUsage(); // Use the only available one

        var ex = Should.Throw<BusinessException>(() => voucher.IncrementUsage());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotValid);
    }

    [Fact]
    public void Activate_Should_Set_IsActive_True()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));
        voucher.Deactivate();

        voucher.Activate();

        voucher.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.Deactivate();

        voucher.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Update_Description_When_Provided()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.Update(description: "New description", expiryDate: null, totalQuantity: null, isActive: null);

        voucher.Description.ShouldBe("New description");
    }

    [Fact]
    public void Update_Should_Update_ExpiryDate_When_Provided()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));
        var newExpiry = DateTime.UtcNow.AddDays(60);

        voucher.Update(description: null, expiryDate: newExpiry, totalQuantity: null, isActive: null);

        voucher.ExpiryDate.ShouldBe(newExpiry);
    }

    [Fact]
    public void Update_Should_Update_TotalQuantity_When_Provided()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.Update(description: null, expiryDate: null, totalQuantity: 500, isActive: null);

        voucher.TotalQuantity.ShouldBe(500);
    }

    [Fact]
    public void Update_Should_Update_IsActive_When_Provided()
    {
        var voucher = CreateVoucher(expiryDate: DateTime.UtcNow.AddDays(30));

        voucher.Update(description: null, expiryDate: null, totalQuantity: null, isActive: false);

        voucher.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Not_Change_Fields_When_Null()
    {
        var expiryDate = DateTime.UtcNow.AddDays(30);
        var voucher = CreateVoucher(expiryDate: expiryDate);

        voucher.Update(description: null, expiryDate: null, totalQuantity: null, isActive: null);

        voucher.Description.ShouldBeNull();
        voucher.ExpiryDate.ShouldBe(expiryDate);
        voucher.TotalQuantity.ShouldBe(100);
        voucher.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Multiple_IncrementUsage_Should_Track_Count()
    {
        var voucher = CreateVoucher(
            expiryDate: DateTime.UtcNow.AddDays(30),
            totalQuantity: 5);

        voucher.IncrementUsage();
        voucher.IncrementUsage();
        voucher.IncrementUsage();

        voucher.UsedQuantity.ShouldBe(3);
        voucher.IsValid().ShouldBeTrue();
    }

    private static Voucher CreateVoucher(
        Guid? id = null,
        DateTime? expiryDate = null,
        int totalQuantity = 100)
    {
        return new Voucher(
            id ?? Guid.NewGuid(),
            "SUMMER2026",
            VoucherType.FixedAmount,
            50000m,
            expiryDate ?? DateTime.UtcNow.AddDays(30),
            totalQuantity);
    }
}
