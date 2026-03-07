using System;
using KLC.Enums;
using Shouldly;
using Xunit;

namespace KLC.Marketing;

public class PromotionTests
{
    [Fact]
    public void Constructor_Should_Initialize_Correctly()
    {
        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(30);

        var promotion = CreatePromotion(id: id, startDate: startDate, endDate: endDate);

        promotion.Id.ShouldBe(id);
        promotion.Title.ShouldBe("Summer Sale");
        promotion.StartDate.ShouldBe(startDate);
        promotion.EndDate.ShouldBe(endDate);
        promotion.Type.ShouldBe(PromotionType.Banner);
        promotion.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Should_Set_Optional_Fields()
    {
        var promotion = new Promotion(
            Guid.NewGuid(),
            "Flash Sale",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            PromotionType.Popup,
            description: "24h flash sale",
            imageUrl: "https://example.com/banner.png");

        promotion.Description.ShouldBe("24h flash sale");
        promotion.ImageUrl.ShouldBe("https://example.com/banner.png");
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_Title()
    {
        Should.Throw<ArgumentException>(() =>
            new Promotion(
                Guid.NewGuid(),
                "",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                PromotionType.Banner));
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_True_When_Active_And_Within_Date_Range()
    {
        var promotion = CreatePromotion(
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow.AddDays(30));

        promotion.IsCurrentlyActive().ShouldBeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_Inactive()
    {
        var promotion = CreatePromotion(
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow.AddDays(30));

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: null, endDate: null, type: null, isActive: false);

        promotion.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_Before_Start_Date()
    {
        var promotion = CreatePromotion(
            startDate: DateTime.UtcNow.AddDays(5),
            endDate: DateTime.UtcNow.AddDays(30));

        promotion.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_Should_Return_False_When_After_End_Date()
    {
        var promotion = CreatePromotion(
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(-1));

        promotion.IsCurrentlyActive().ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Update_Title_When_Provided()
    {
        var promotion = CreatePromotion();

        promotion.Update(title: "Updated Title", description: null, imageUrl: null,
            startDate: null, endDate: null, type: null, isActive: null);

        promotion.Title.ShouldBe("Updated Title");
    }

    [Fact]
    public void Update_Should_Update_Description_When_Provided()
    {
        var promotion = CreatePromotion();

        promotion.Update(title: null, description: "New desc", imageUrl: null,
            startDate: null, endDate: null, type: null, isActive: null);

        promotion.Description.ShouldBe("New desc");
    }

    [Fact]
    public void Update_Should_Update_ImageUrl_When_Provided()
    {
        var promotion = CreatePromotion();

        promotion.Update(title: null, description: null, imageUrl: "https://new.url/img.png",
            startDate: null, endDate: null, type: null, isActive: null);

        promotion.ImageUrl.ShouldBe("https://new.url/img.png");
    }

    [Fact]
    public void Update_Should_Update_StartDate_When_Provided()
    {
        var promotion = CreatePromotion();
        var newStart = DateTime.UtcNow.AddDays(10);

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: newStart, endDate: null, type: null, isActive: null);

        promotion.StartDate.ShouldBe(newStart);
    }

    [Fact]
    public void Update_Should_Update_EndDate_When_Provided()
    {
        var promotion = CreatePromotion();
        var newEnd = DateTime.UtcNow.AddDays(60);

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: null, endDate: newEnd, type: null, isActive: null);

        promotion.EndDate.ShouldBe(newEnd);
    }

    [Fact]
    public void Update_Should_Update_Type_When_Provided()
    {
        var promotion = CreatePromotion();

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: null, endDate: null, type: PromotionType.PushNotification, isActive: null);

        promotion.Type.ShouldBe(PromotionType.PushNotification);
    }

    [Fact]
    public void Update_Should_Update_IsActive_When_Provided()
    {
        var promotion = CreatePromotion();

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: null, endDate: null, type: null, isActive: false);

        promotion.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Not_Change_Fields_When_Null()
    {
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(30);
        var promotion = CreatePromotion(startDate: startDate, endDate: endDate);

        promotion.Update(title: null, description: null, imageUrl: null,
            startDate: null, endDate: null, type: null, isActive: null);

        promotion.Title.ShouldBe("Summer Sale");
        promotion.StartDate.ShouldBe(startDate);
        promotion.EndDate.ShouldBe(endDate);
        promotion.Type.ShouldBe(PromotionType.Banner);
        promotion.IsActive.ShouldBeTrue();
    }

    private static Promotion CreatePromotion(
        Guid? id = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return new Promotion(
            id ?? Guid.NewGuid(),
            "Summer Sale",
            startDate ?? DateTime.UtcNow.AddDays(-1),
            endDate ?? DateTime.UtcNow.AddDays(30),
            PromotionType.Banner);
    }
}
