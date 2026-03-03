using System;
using KLC.Enums;
using Shouldly;
using Xunit;

namespace KLC.Users;

public class UserIdTagTests
{
    [Fact]
    public void Constructor_Should_Initialize_Active()
    {
        var tag = CreateTag();

        tag.IsActive.ShouldBeTrue();
        tag.TagType.ShouldBe(IdTagType.Rfid);
        tag.IdTag.ShouldBe("B4A63CDF");
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_UserId()
    {
        Should.Throw<ArgumentException>(() =>
            new UserIdTag(Guid.NewGuid(), Guid.Empty, "TAG123", IdTagType.Rfid));
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_IdTag()
    {
        Should.Throw<ArgumentException>(() =>
            new UserIdTag(Guid.NewGuid(), Guid.NewGuid(), "", IdTagType.Rfid));
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var tag = CreateTag();

        tag.Deactivate();

        tag.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Activate_Should_Set_IsActive_True()
    {
        var tag = CreateTag();
        tag.Deactivate();

        tag.Activate();

        tag.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_True_When_Active_No_Expiry()
    {
        var tag = CreateTag();

        tag.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Inactive()
    {
        var tag = CreateTag();
        tag.Deactivate();

        tag.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Expired()
    {
        var tag = new UserIdTag(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EXPIRED01",
            IdTagType.Rfid,
            expiryDate: DateTime.UtcNow.AddDays(-1));

        tag.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_True_When_Not_Yet_Expired()
    {
        var tag = new UserIdTag(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "FUTURE01",
            IdTagType.Rfid,
            expiryDate: DateTime.UtcNow.AddDays(30));

        tag.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void SetFriendlyName_Should_Update()
    {
        var tag = CreateTag();

        tag.SetFriendlyName("My Blue Card");

        tag.FriendlyName.ShouldBe("My Blue Card");
    }

    [Fact]
    public void SetExpiryDate_Should_Update()
    {
        var tag = CreateTag();
        var expiry = DateTime.UtcNow.AddMonths(6);

        tag.SetExpiryDate(expiry);

        tag.ExpiryDate.ShouldBe(expiry);
    }

    private static UserIdTag CreateTag()
    {
        return new UserIdTag(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "B4A63CDF",
            IdTagType.Rfid,
            "Test Card");
    }
}
