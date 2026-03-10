using System;
using KLC.Enums;
using KLC.Users;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.UserManagement;

/// <summary>
/// Tests for AppUser and DeviceToken domain entity behavior.
/// Validates profile management, wallet operations, and device token lifecycle.
/// </summary>
public class UserManagementTests
{
    private static AppUser CreateTestUser(
        string fullName = "Nguyen Van A",
        string? phoneNumber = "0912345678",
        string? email = "test@example.com")
    {
        return new AppUser(
            Guid.NewGuid(),
            Guid.NewGuid(),
            fullName,
            phoneNumber,
            email);
    }

    [Fact]
    public void Create_AppUser_Should_Set_Default_Values()
    {
        var identityUserId = Guid.NewGuid();
        var user = new AppUser(
            Guid.NewGuid(),
            identityUserId,
            "Tran Thi B",
            "0987654321",
            "b@example.com");

        user.IdentityUserId.ShouldBe(identityUserId);
        user.FullName.ShouldBe("Tran Thi B");
        user.PhoneNumber.ShouldBe("0987654321");
        user.Email.ShouldBe("b@example.com");
        user.IsPhoneVerified.ShouldBeFalse();
        user.IsEmailVerified.ShouldBeFalse();
        user.IsNotificationsEnabled.ShouldBeTrue();
        user.IsActive.ShouldBeTrue();
        user.WalletBalance.ShouldBe(0m);
        user.PreferredLanguage.ShouldBe("vi");
        user.MembershipTier.ShouldBe(MembershipTier.Standard);
        user.AvatarUrl.ShouldBeNull();
        user.FcmToken.ShouldBeNull();
        user.LastLoginAt.ShouldBeNull();
        user.LastTopUpAt.ShouldBeNull();
    }

    [Fact]
    public void UpdateProfile_Should_Change_Name_And_Avatar()
    {
        var user = CreateTestUser();

        user.UpdateProfile("Le Van C", "https://cdn.example.com/avatar.jpg");

        user.FullName.ShouldBe("Le Van C");
        user.AvatarUrl.ShouldBe("https://cdn.example.com/avatar.jpg");
    }

    [Fact]
    public void SetPhoneNumber_Should_Update_And_Reset_Verification()
    {
        var user = CreateTestUser();
        user.VerifyPhone();
        user.IsPhoneVerified.ShouldBeTrue();

        user.SetPhoneNumber("0911111111");

        user.PhoneNumber.ShouldBe("0911111111");
        user.IsPhoneVerified.ShouldBeFalse();
    }

    [Fact]
    public void VerifyPhone_Should_Set_IsPhoneVerified_True()
    {
        var user = CreateTestUser();
        user.IsPhoneVerified.ShouldBeFalse();

        user.VerifyPhone();

        user.IsPhoneVerified.ShouldBeTrue();
    }

    [Fact]
    public void SetEmail_And_VerifyEmail_Should_Work()
    {
        var user = CreateTestUser();

        user.SetEmail("new@example.com");
        user.Email.ShouldBe("new@example.com");
        user.IsEmailVerified.ShouldBeFalse();

        user.VerifyEmail();
        user.IsEmailVerified.ShouldBeTrue();
    }

    [Fact]
    public void SetPreferredLanguage_Should_Accept_Valid_Languages()
    {
        var user = CreateTestUser();
        user.PreferredLanguage.ShouldBe("vi");

        user.SetPreferredLanguage("en");
        user.PreferredLanguage.ShouldBe("en");

        user.SetPreferredLanguage("vi");
        user.PreferredLanguage.ShouldBe("vi");
    }

    [Fact]
    public void SetPreferredLanguage_Should_Throw_For_Invalid_Language()
    {
        var user = CreateTestUser();

        Should.Throw<ArgumentException>(() => user.SetPreferredLanguage("fr"));
        Should.Throw<ArgumentException>(() => user.SetPreferredLanguage(""));
    }

    [Fact]
    public void AddToWallet_Should_Increase_Balance()
    {
        var user = CreateTestUser();
        user.WalletBalance.ShouldBe(0m);

        user.AddToWallet(100000m);
        user.WalletBalance.ShouldBe(100000m);

        user.AddToWallet(50000m);
        user.WalletBalance.ShouldBe(150000m);
    }

    [Fact]
    public void AddToWallet_Should_Throw_For_NonPositive_Amount()
    {
        var user = CreateTestUser();

        Should.Throw<ArgumentException>(() => user.AddToWallet(0m));
        Should.Throw<ArgumentException>(() => user.AddToWallet(-10000m));
    }

    [Fact]
    public void DeductFromWallet_Should_Decrease_Balance()
    {
        var user = CreateTestUser();
        user.AddToWallet(200000m);

        user.DeductFromWallet(50000m);

        user.WalletBalance.ShouldBe(150000m);
    }

    [Fact]
    public void DeductFromWallet_Should_Throw_For_Insufficient_Balance()
    {
        var user = CreateTestUser();
        user.AddToWallet(10000m);

        var ex = Should.Throw<BusinessException>(() => user.DeductFromWallet(50000m));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalance);
    }

    [Fact]
    public void DeductFromWallet_Should_Throw_For_NonPositive_Amount()
    {
        var user = CreateTestUser();
        user.AddToWallet(100000m);

        Should.Throw<ArgumentException>(() => user.DeductFromWallet(0m));
        Should.Throw<ArgumentException>(() => user.DeductFromWallet(-5000m));
    }

    [Fact]
    public void SetMembershipTier_Should_Update_Tier()
    {
        var user = CreateTestUser();
        user.MembershipTier.ShouldBe(MembershipTier.Standard);

        user.SetMembershipTier(MembershipTier.Gold);
        user.MembershipTier.ShouldBe(MembershipTier.Gold);

        user.SetMembershipTier(MembershipTier.Platinum);
        user.MembershipTier.ShouldBe(MembershipTier.Platinum);
    }

    [Fact]
    public void Deactivate_And_Reactivate_Should_Toggle_IsActive()
    {
        var user = CreateTestUser();
        user.IsActive.ShouldBeTrue();

        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        user.Reactivate();
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void RecordLogin_Should_Set_LastLoginAt()
    {
        var user = CreateTestUser();
        user.LastLoginAt.ShouldBeNull();

        var before = DateTime.UtcNow;
        user.RecordLogin();

        user.LastLoginAt.ShouldNotBeNull();
        user.LastLoginAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Notifications_Enable_Disable_Should_Toggle()
    {
        var user = CreateTestUser();
        user.IsNotificationsEnabled.ShouldBeTrue();

        user.DisableNotifications();
        user.IsNotificationsEnabled.ShouldBeFalse();

        user.EnableNotifications();
        user.IsNotificationsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void SetGender_Should_Accept_Valid_Values()
    {
        var user = CreateTestUser();

        user.SetGender("M");
        user.Gender.ShouldBe("M");

        user.SetGender("F");
        user.Gender.ShouldBe("F");

        user.SetGender(null);
        user.Gender.ShouldBeNull();
    }

    [Fact]
    public void SetGender_Should_Throw_For_Invalid_Value()
    {
        var user = CreateTestUser();

        Should.Throw<ArgumentException>(() => user.SetGender("X"));
        Should.Throw<ArgumentException>(() => user.SetGender("Other"));
    }

    [Fact]
    public void UpdateFcmToken_Should_Set_Token()
    {
        var user = CreateTestUser();

        user.UpdateFcmToken("fcm-token-abc123");
        user.FcmToken.ShouldBe("fcm-token-abc123");

        user.UpdateFcmToken(null);
        user.FcmToken.ShouldBeNull();
    }

    // --- DeviceToken tests ---

    [Fact]
    public void Create_DeviceToken_Should_Set_Default_Values()
    {
        var userId = Guid.NewGuid();
        var token = new DeviceToken(
            Guid.NewGuid(),
            userId,
            "fcm-device-token-xyz",
            DevicePlatform.Android);

        token.UserId.ShouldBe(userId);
        token.Token.ShouldBe("fcm-device-token-xyz");
        token.Platform.ShouldBe(DevicePlatform.Android);
        token.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void DeviceToken_Deactivate_Should_Set_IsActive_False()
    {
        var token = new DeviceToken(
            Guid.NewGuid(), Guid.NewGuid(), "token-123", DevicePlatform.iOS);

        token.Deactivate();

        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void DeviceToken_UpdateToken_Should_Change_Token_And_Reactivate()
    {
        var token = new DeviceToken(
            Guid.NewGuid(), Guid.NewGuid(), "old-token", DevicePlatform.Android);
        token.Deactivate();
        token.IsActive.ShouldBeFalse();

        token.UpdateToken("new-token-456");

        token.Token.ShouldBe("new-token-456");
        token.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void DeviceToken_Should_Throw_For_Empty_Token()
    {
        Should.Throw<Exception>(() =>
            new DeviceToken(Guid.NewGuid(), Guid.NewGuid(), "", DevicePlatform.iOS));
    }

    [Fact]
    public void DeviceToken_UpdateToken_Should_Throw_For_Empty_Token()
    {
        var token = new DeviceToken(
            Guid.NewGuid(), Guid.NewGuid(), "valid-token", DevicePlatform.Android);

        Should.Throw<Exception>(() => token.UpdateToken(""));
    }
}
