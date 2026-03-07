using System;
using Shouldly;
using Xunit;

namespace KLC.Marketing;

public class UserVoucherTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_IsUsed_False()
    {
        var userId = Guid.NewGuid();
        var voucherId = Guid.NewGuid();

        var userVoucher = CreateUserVoucher(userId: userId, voucherId: voucherId);

        userVoucher.UserId.ShouldBe(userId);
        userVoucher.VoucherId.ShouldBe(voucherId);
        userVoucher.IsUsed.ShouldBeFalse();
        userVoucher.UsedAt.ShouldBeNull();
    }

    [Fact]
    public void MarkUsed_Should_Set_IsUsed_True()
    {
        var userVoucher = CreateUserVoucher();

        userVoucher.MarkUsed();

        userVoucher.IsUsed.ShouldBeTrue();
    }

    [Fact]
    public void MarkUsed_Should_Set_UsedAt()
    {
        var userVoucher = CreateUserVoucher();
        var before = DateTime.UtcNow;

        userVoucher.MarkUsed();

        userVoucher.UsedAt.ShouldNotBeNull();
        userVoucher.UsedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        userVoucher.UsedAt!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void MarkUsed_Twice_Should_Still_Be_Used()
    {
        var userVoucher = CreateUserVoucher();

        userVoucher.MarkUsed();
        var firstUsedAt = userVoucher.UsedAt;

        userVoucher.MarkUsed();

        userVoucher.IsUsed.ShouldBeTrue();
        userVoucher.UsedAt.ShouldNotBeNull();
    }

    private static UserVoucher CreateUserVoucher(
        Guid? userId = null,
        Guid? voucherId = null)
    {
        return new UserVoucher(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            voucherId ?? Guid.NewGuid());
    }
}
