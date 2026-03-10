using System;
using KLC.Enums;
using KLC.Marketing;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Marketing;

/// <summary>
/// Tests for Promotion domain entity behavior.
/// Validates creation, active status checks, and updates.
/// </summary>
public class PromotionTests
{
    private static Promotion CreateTestPromotion(
        string title = "Summer Charging Promotion",
        PromotionType type = PromotionType.Banner,
        int daysFromNowStart = -1,
        int daysFromNowEnd = 30,
        string? description = "Get 20% off all charging sessions",
        string? imageUrl = null)
    {
        return new Promotion(
            Guid.NewGuid(),
            title,
            DateTime.UtcNow.AddDays(daysFromNowStart),
            DateTime.UtcNow.AddDays(daysFromNowEnd),
            type,
            description,
            imageUrl);
    }

    [Fact]
    public void Create_Promotion_Should_Set_Default_Values()
    {
        var promo = CreateTestPromotion();

        promo.Title.ShouldBe("Summer Charging Promotion");
        promo.Type.ShouldBe(PromotionType.Banner);
        promo.Description.ShouldBe("Get 20% off all charging sessions");
        promo.IsActive.ShouldBeTrue();
        promo.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_Promotion_With_Empty_Title_Should_Throw()
    {
        Should.Throw<Exception>(() =>
            CreateTestPromotion(title: ""));
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_True_When_Within_DateRange_And_Active()
    {
        var promo = CreateTestPromotion(
            daysFromNowStart: -5,
            daysFromNowEnd: 10);

        promo.IsCurrentlyActive().ShouldBeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_Not_Yet_Started()
    {
        var promo = CreateTestPromotion(
            daysFromNowStart: 5,
            daysFromNowEnd: 30);

        promo.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_Ended()
    {
        var promo = CreateTestPromotion(
            daysFromNowStart: -30,
            daysFromNowEnd: -1);

        promo.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_Deactivated()
    {
        var promo = CreateTestPromotion();

        promo.Update(
            title: null,
            description: null,
            imageUrl: null,
            startDate: null,
            endDate: null,
            type: null,
            isActive: false);

        promo.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Change_Specified_Fields()
    {
        var promo = CreateTestPromotion();
        var newEnd = DateTime.UtcNow.AddDays(90);

        promo.Update(
            title: "Updated Title",
            description: "New description",
            imageUrl: "https://cdn.example.com/banner.jpg",
            startDate: null,
            endDate: newEnd,
            type: PromotionType.Popup,
            isActive: null);

        promo.Title.ShouldBe("Updated Title");
        promo.Description.ShouldBe("New description");
        promo.ImageUrl.ShouldBe("https://cdn.example.com/banner.jpg");
        promo.Type.ShouldBe(PromotionType.Popup);
        promo.IsActive.ShouldBeTrue(); // Unchanged
    }

    [Fact]
    public void Update_With_Empty_Title_Should_Throw()
    {
        var promo = CreateTestPromotion();

        Should.Throw<Exception>(() =>
            promo.Update(
                title: "",
                description: null,
                imageUrl: null,
                startDate: null,
                endDate: null,
                type: null,
                isActive: null));
    }

    [Fact]
    public void Create_Promotion_With_All_Types()
    {
        foreach (var type in Enum.GetValues<PromotionType>())
        {
            var promo = CreateTestPromotion(type: type);
            promo.Type.ShouldBe(type);
        }
    }

    [Fact]
    public void Update_Deactivate_Should_Set_IsActive_False()
    {
        var promo = CreateTestPromotion();
        promo.IsActive.ShouldBeTrue();

        promo.Update(
            title: null,
            description: null,
            imageUrl: null,
            startDate: null,
            endDate: null,
            type: null,
            isActive: false);

        promo.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Create_Promotion_With_ImageUrl()
    {
        var promo = CreateTestPromotion(imageUrl: "https://storage.googleapis.com/banner.png");

        promo.ImageUrl.ShouldBe("https://storage.googleapis.com/banner.png");
    }
}
