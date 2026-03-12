using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Operators;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Operators;

public abstract class OperatorAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IOperatorAppService _operatorAppService;

    protected OperatorAppServiceTests()
    {
        _operatorAppService = GetRequiredService<IOperatorAppService>();
    }

    [Fact]
    public async Task Should_Create_Operator()
    {
        var result = await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Test Operator",
            ContactEmail = "test@operator.com",
            Description = "Test description",
            RateLimitPerMinute = 500
        });

        result.Operator.ShouldNotBeNull();
        result.Operator.Id.ShouldNotBe(Guid.Empty);
        result.Operator.Name.ShouldBe("Test Operator");
        result.Operator.ContactEmail.ShouldBe("test@operator.com");
        result.Operator.IsActive.ShouldBeTrue();
        result.Operator.RateLimitPerMinute.ShouldBe(500);
        result.ApiKey.ShouldNotBeNullOrWhiteSpace();
        result.ApiKey.Length.ShouldBe(64); // 32 bytes hex
    }

    [Fact]
    public async Task Should_Get_Operator_By_Id()
    {
        var created = await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Get Test Op",
            ContactEmail = "get@test.com"
        });

        var op = await _operatorAppService.GetAsync(created.Operator.Id);

        op.Name.ShouldBe("Get Test Op");
        op.ContactEmail.ShouldBe("get@test.com");
        op.AllowedStations.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Name()
    {
        await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Unique Op",
            ContactEmail = "first@test.com"
        });

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _operatorAppService.CreateAsync(new CreateOperatorDto
            {
                Name = "Unique Op",
                ContactEmail = "second@test.com"
            });
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Operators.DuplicateName);
    }

    [Fact]
    public async Task Should_Update_Operator()
    {
        var created = await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Update Test",
            ContactEmail = "update@test.com"
        });

        var updated = await _operatorAppService.UpdateAsync(created.Operator.Id, new UpdateOperatorDto
        {
            Name = "Updated Name",
            ContactEmail = "updated@test.com",
            WebhookUrl = "https://webhook.example.com",
            RateLimitPerMinute = 2000
        });

        updated.Name.ShouldBe("Updated Name");
        updated.ContactEmail.ShouldBe("updated@test.com");
        updated.WebhookUrl.ShouldBe("https://webhook.example.com");
        updated.RateLimitPerMinute.ShouldBe(2000);
    }

    [Fact]
    public async Task Should_Delete_Operator()
    {
        var created = await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Delete Test",
            ContactEmail = "delete@test.com"
        });

        await _operatorAppService.DeleteAsync(created.Operator.Id);

        // Soft-deleted entity throws EntityNotFoundException from ABP repository
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _operatorAppService.GetAsync(created.Operator.Id);
        });
    }

    [Fact]
    public async Task Should_Regenerate_Api_Key()
    {
        var created = await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "Regen Key Op",
            ContactEmail = "regen@test.com"
        });

        var newKey = await _operatorAppService.RegenerateApiKeyAsync(created.Operator.Id);

        newKey.ApiKey.ShouldNotBeNullOrWhiteSpace();
        newKey.ApiKey.Length.ShouldBe(64);
        newKey.ApiKey.ShouldNotBe(created.ApiKey);
    }

    [Fact]
    public async Task Should_List_Operators()
    {
        await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "List Op A",
            ContactEmail = "a@test.com"
        });
        await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "List Op B",
            ContactEmail = "b@test.com"
        });

        var result = await _operatorAppService.GetListAsync(new GetOperatorListDto { PageSize = 50 });

        result.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Search_Operators()
    {
        await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "SearchOp Alpha",
            ContactEmail = "alpha@test.com"
        });
        await _operatorAppService.CreateAsync(new CreateOperatorDto
        {
            Name = "SearchOp Beta",
            ContactEmail = "beta@test.com"
        });

        var result = await _operatorAppService.GetListAsync(new GetOperatorListDto
        {
            Search = "Alpha"
        });

        result.ShouldContain(o => o.Name.Contains("Alpha"));
        result.ShouldNotContain(o => o.Name.Contains("Beta"));
    }
}
