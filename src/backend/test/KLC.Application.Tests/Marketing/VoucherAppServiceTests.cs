using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Marketing;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Marketing;

public abstract class VoucherAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IVoucherAppService _voucherAppService;

    protected VoucherAppServiceTests()
    {
        _voucherAppService = GetRequiredService<IVoucherAppService>();
    }

    [Fact]
    public async Task Should_Create_Voucher()
    {
        var result = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "TEST50",
            Type = VoucherType.FixedAmount,
            Value = 50000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 100,
            Description = "Test voucher"
        });

        result.Id.ShouldNotBe(Guid.Empty);
        result.Code.ShouldBe("TEST50");
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Voucher_Code()
    {
        await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "DUPE01",
            Type = VoucherType.FixedAmount,
            Value = 10000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _voucherAppService.CreateAsync(new CreateVoucherDto
            {
                Code = "DUPE01",
                Type = VoucherType.Percentage,
                Value = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                TotalQuantity = 5
            });
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.DuplicateCode);
    }

    [Fact]
    public async Task Should_Get_Voucher_By_Id()
    {
        var created = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "GET01",
            Type = VoucherType.Percentage,
            Value = 20,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 50,
            MinOrderAmount = 100000,
            MaxDiscountAmount = 50000
        });

        var voucher = await _voucherAppService.GetAsync(created.Id);

        voucher.Code.ShouldBe("GET01");
        voucher.Type.ShouldBe(VoucherType.Percentage);
        voucher.Value.ShouldBe(20);
        voucher.TotalQuantity.ShouldBe(50);
        voucher.UsedQuantity.ShouldBe(0);
        voucher.IsActive.ShouldBeTrue();
        voucher.MinOrderAmount.ShouldBe(100000);
        voucher.MaxDiscountAmount.ShouldBe(50000);
    }

    [Fact]
    public async Task Should_Throw_When_Getting_NonExistent_Voucher()
    {
        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _voucherAppService.GetAsync(Guid.NewGuid());
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Voucher.NotFound);
    }

    [Fact]
    public async Task Should_List_Vouchers()
    {
        await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "LIST01",
            Type = VoucherType.FixedAmount,
            Value = 10000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });
        await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "LIST02",
            Type = VoucherType.FreeCharging,
            Value = 0,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 5
        });

        var result = await _voucherAppService.GetListAsync(new GetVoucherListDto { PageSize = 10 });

        result.Data.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Filter_By_IsActive()
    {
        var created = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "FILTER01",
            Type = VoucherType.FixedAmount,
            Value = 5000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });

        // Deactivate via soft delete
        await _voucherAppService.DeleteAsync(created.Id);

        var activeOnly = await _voucherAppService.GetListAsync(new GetVoucherListDto { IsActive = true });
        activeOnly.Data.ShouldNotContain(v => v.Code == "FILTER01");
    }

    [Fact]
    public async Task Should_Update_Voucher()
    {
        var created = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "UPD01",
            Type = VoucherType.FixedAmount,
            Value = 10000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });

        var newExpiry = DateTime.UtcNow.AddDays(60);
        await _voucherAppService.UpdateAsync(created.Id, new UpdateVoucherDto
        {
            Description = "Updated description",
            ExpiryDate = newExpiry,
            TotalQuantity = 200
        });

        var updated = await _voucherAppService.GetAsync(created.Id);
        updated.Description.ShouldBe("Updated description");
        updated.TotalQuantity.ShouldBe(200);
    }

    [Fact]
    public async Task Should_Soft_Delete_Voucher()
    {
        var created = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "DEL01",
            Type = VoucherType.FixedAmount,
            Value = 10000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });

        await _voucherAppService.DeleteAsync(created.Id);

        // Voucher should still be accessible (soft deleted = deactivated)
        var voucher = await _voucherAppService.GetAsync(created.Id);
        voucher.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Get_Empty_Usage_For_New_Voucher()
    {
        var created = await _voucherAppService.CreateAsync(new CreateVoucherDto
        {
            Code = "USAGE01",
            Type = VoucherType.FixedAmount,
            Value = 10000,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            TotalQuantity = 10
        });

        var usage = await _voucherAppService.GetUsageAsync(created.Id);

        usage.TotalQuantity.ShouldBe(10);
        usage.UsedQuantity.ShouldBe(0);
        usage.Usages.ShouldBeEmpty();
    }
}
