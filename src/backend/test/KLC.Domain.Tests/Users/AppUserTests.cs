using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Users;

public class AppUserTests
{
    [Fact]
    public void Constructor_Should_Initialize_Correctly()
    {
        var id = Guid.NewGuid();
        var identityUserId = Guid.NewGuid();

        var user = new AppUser(id, identityUserId, "Nguyen Van A", "0901234567", "a@test.com");

        user.Id.ShouldBe(id);
        user.IdentityUserId.ShouldBe(identityUserId);
        user.FullName.ShouldBe("Nguyen Van A");
        user.PhoneNumber.ShouldBe("0901234567");
        user.Email.ShouldBe("a@test.com");
        user.IsPhoneVerified.ShouldBeFalse();
        user.IsEmailVerified.ShouldBeFalse();
        user.IsNotificationsEnabled.ShouldBeTrue();
        user.IsActive.ShouldBeTrue();
        user.WalletBalance.ShouldBe(0m);
        user.PreferredLanguage.ShouldBe("vi");
        user.MembershipTier.ShouldBe(MembershipTier.Standard);
    }

    [Fact]
    public void Constructor_Should_Set_MembershipTier_To_Standard()
    {
        var user = CreateUser();

        user.MembershipTier.ShouldBe(MembershipTier.Standard);
    }

    [Fact]
    public void SetMembershipTier_Should_Update_Tier()
    {
        var user = CreateUser();

        user.SetMembershipTier(MembershipTier.Gold);

        user.MembershipTier.ShouldBe(MembershipTier.Gold);
    }

    [Fact]
    public void SetMembershipTier_Should_Update_To_Platinum()
    {
        var user = CreateUser();

        user.SetMembershipTier(MembershipTier.Platinum);

        user.MembershipTier.ShouldBe(MembershipTier.Platinum);
    }

    [Fact]
    public void SetMembershipTier_Should_Allow_Downgrade()
    {
        var user = CreateUser();
        user.SetMembershipTier(MembershipTier.Gold);

        user.SetMembershipTier(MembershipTier.Standard);

        user.MembershipTier.ShouldBe(MembershipTier.Standard);
    }

    [Fact]
    public void RecordTopUp_Should_Set_LastTopUpAt()
    {
        var user = CreateUser();
        var before = DateTime.UtcNow;

        user.RecordTopUp();

        user.LastTopUpAt.ShouldNotBeNull();
        user.LastTopUpAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        user.LastTopUpAt!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void SetDateOfBirth_Should_Set_Date()
    {
        var user = CreateUser();
        var dob = new DateTime(1990, 5, 15);

        user.SetDateOfBirth(dob);

        user.DateOfBirth.ShouldBe(dob);
    }

    [Fact]
    public void SetDateOfBirth_Should_Allow_Null()
    {
        var user = CreateUser();
        user.SetDateOfBirth(new DateTime(1990, 5, 15));

        user.SetDateOfBirth(null);

        user.DateOfBirth.ShouldBeNull();
    }

    [Fact]
    public void SetGender_Should_Set_Male()
    {
        var user = CreateUser();

        user.SetGender("M");

        user.Gender.ShouldBe("M");
    }

    [Fact]
    public void SetGender_Should_Set_Female()
    {
        var user = CreateUser();

        user.SetGender("F");

        user.Gender.ShouldBe("F");
    }

    [Fact]
    public void SetGender_Should_Allow_Null()
    {
        var user = CreateUser();
        user.SetGender("M");

        user.SetGender(null);

        user.Gender.ShouldBeNull();
    }

    [Fact]
    public void SetGender_Should_Throw_For_Invalid_Value()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.SetGender("X"));
    }

    [Fact]
    public void SetGender_Should_Throw_For_Lowercase()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.SetGender("m"));
    }

    [Fact]
    public void SetGender_Should_Throw_For_Other_String()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.SetGender("Male"));
    }

    [Fact]
    public void SetAvatarUrl_Should_Set_Url()
    {
        var user = CreateUser();

        user.SetAvatarUrl("https://example.com/avatar.png");

        user.AvatarUrl.ShouldBe("https://example.com/avatar.png");
    }

    [Fact]
    public void SetAvatarUrl_Should_Allow_Null()
    {
        var user = CreateUser();
        user.SetAvatarUrl("https://example.com/avatar.png");

        user.SetAvatarUrl(null);

        user.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public void AddToWallet_Should_Increase_Balance()
    {
        var user = CreateUser();

        user.AddToWallet(100000m);

        user.WalletBalance.ShouldBe(100000m);
    }

    [Fact]
    public void AddToWallet_Should_Throw_For_Zero()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.AddToWallet(0));
    }

    [Fact]
    public void AddToWallet_Should_Throw_For_Negative()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.AddToWallet(-100m));
    }

    [Fact]
    public void DeductFromWallet_Should_Decrease_Balance()
    {
        var user = CreateUser();
        user.AddToWallet(200000m);

        user.DeductFromWallet(50000m);

        user.WalletBalance.ShouldBe(150000m);
    }

    [Fact]
    public void DeductFromWallet_Should_Throw_For_Insufficient_Balance()
    {
        var user = CreateUser();
        user.AddToWallet(50000m);

        var ex = Should.Throw<BusinessException>(() => user.DeductFromWallet(100000m));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalance);
    }

    [Fact]
    public void DeductFromWallet_Should_Throw_For_Zero()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.DeductFromWallet(0));
    }

    [Fact]
    public void SetPreferredLanguage_Should_Accept_Vi()
    {
        var user = CreateUser();

        user.SetPreferredLanguage("vi");

        user.PreferredLanguage.ShouldBe("vi");
    }

    [Fact]
    public void SetPreferredLanguage_Should_Accept_En()
    {
        var user = CreateUser();

        user.SetPreferredLanguage("en");

        user.PreferredLanguage.ShouldBe("en");
    }

    [Fact]
    public void SetPreferredLanguage_Should_Throw_For_Invalid()
    {
        var user = CreateUser();

        Should.Throw<ArgumentException>(() => user.SetPreferredLanguage("fr"));
    }

    [Fact]
    public void Deactivate_And_Reactivate_Should_Toggle_IsActive()
    {
        var user = CreateUser();

        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        user.Reactivate();
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void RecordLogin_Should_Set_LastLoginAt()
    {
        var user = CreateUser();
        var before = DateTime.UtcNow;

        user.RecordLogin();

        user.LastLoginAt.ShouldNotBeNull();
        user.LastLoginAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void UpdateProfile_Should_Update_FullName_And_Avatar()
    {
        var user = CreateUser();

        user.UpdateProfile("New Name", "https://example.com/new.png");

        user.FullName.ShouldBe("New Name");
        user.AvatarUrl.ShouldBe("https://example.com/new.png");
    }

    [Fact]
    public void VerifyPhone_Should_Set_IsPhoneVerified_True()
    {
        var user = CreateUser();

        user.VerifyPhone();

        user.IsPhoneVerified.ShouldBeTrue();
    }

    [Fact]
    public void VerifyEmail_Should_Set_IsEmailVerified_True()
    {
        var user = CreateUser();

        user.VerifyEmail();

        user.IsEmailVerified.ShouldBeTrue();
    }

    private static AppUser CreateUser()
    {
        return new AppUser(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test User",
            "0901234567");
    }
}
