using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Marketing;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Marketing;

public abstract class PromotionAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPromotionAppService _promotionAppService;

    protected PromotionAppServiceTests()
    {
        _promotionAppService = GetRequiredService<IPromotionAppService>();
    }

    [Fact]
    public async Task Should_Create_Promotion()
    {
        var result = await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "Summer Sale",
            Description = "50% off all charging",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Type = PromotionType.Banner
        });

        result.Id.ShouldNotBe(Guid.Empty);
        result.Title.ShouldBe("Summer Sale");
    }

    [Fact]
    public async Task Should_Get_Promotion_By_Id()
    {
        var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "Test Promo",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Type = PromotionType.InApp
        });

        var promo = await _promotionAppService.GetAsync(created.Id);

        promo.Title.ShouldBe("Test Promo");
        promo.Type.ShouldBe(PromotionType.InApp);
        promo.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_Getting_NonExistent_Promotion()
    {
        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _promotionAppService.GetAsync(Guid.NewGuid());
        });
    }

    [Fact]
    public async Task Should_List_Promotions()
    {
        await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "Promo 1",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Type = PromotionType.Banner
        });
        await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "Promo 2",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14),
            Type = PromotionType.Popup
        });

        var result = await _promotionAppService.GetListAsync(new GetPromotionListDto { PageSize = 10 });

        result.Data.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Pagination.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Update_Promotion()
    {
        var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "Original",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Type = PromotionType.Banner
        });

        await _promotionAppService.UpdateAsync(created.Id, new UpdatePromotionDto
        {
            Title = "Updated Title",
            IsActive = false
        });

        var updated = await _promotionAppService.GetAsync(created.Id);
        updated.Title.ShouldBe("Updated Title");
        updated.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Delete_Promotion()
    {
        var created = await _promotionAppService.CreateAsync(new CreatePromotionDto
        {
            Title = "To Delete",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Type = PromotionType.Banner
        });

        await _promotionAppService.DeleteAsync(created.Id);

        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _promotionAppService.GetAsync(created.Id);
        });
    }

}
