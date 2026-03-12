using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.PowerSharing;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.PowerSharing;

public abstract class PowerSharingAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPowerSharingAppService _powerSharingAppService;

    protected PowerSharingAppServiceTests()
    {
        _powerSharingAppService = GetRequiredService<IPowerSharingAppService>();
    }

    [Fact]
    public async Task Should_Create_Group()
    {
        var result = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Test Group",
            MaxCapacityKw = 100m,
            Mode = PowerSharingMode.Link,
            DistributionStrategy = PowerDistributionStrategy.Average,
            MinPowerPerConnectorKw = 2m
        });

        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Test Group");
        result.MaxCapacityKw.ShouldBe(100m);
        result.Mode.ShouldBe(PowerSharingMode.Link);
        result.DistributionStrategy.ShouldBe(PowerDistributionStrategy.Average);
        result.MinPowerPerConnectorKw.ShouldBe(2m);
        result.IsActive.ShouldBeTrue();
        result.Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Get_Group_By_Id()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Get Group",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Loop
        });

        var group = await _powerSharingAppService.GetAsync(created.Id);

        group.Name.ShouldBe("Get Group");
        group.Mode.ShouldBe(PowerSharingMode.Loop);
        group.Members.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Update_Group()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Update Group",
            MaxCapacityKw = 80m,
            Mode = PowerSharingMode.Link
        });

        var updated = await _powerSharingAppService.UpdateAsync(created.Id, new UpdatePowerSharingGroupDto
        {
            Name = "Updated Group Name",
            MaxCapacityKw = 120m,
            DistributionStrategy = PowerDistributionStrategy.Proportional,
            MinPowerPerConnectorKw = 3m
        });

        updated.Name.ShouldBe("Updated Group Name");
        updated.MaxCapacityKw.ShouldBe(120m);
        updated.DistributionStrategy.ShouldBe(PowerDistributionStrategy.Proportional);
    }

    [Fact]
    public async Task Should_Delete_Group()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Delete Group",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Link
        });

        await _powerSharingAppService.DeleteAsync(created.Id);

        // Soft-deleted entity throws from ABP repository or domain service
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _powerSharingAppService.GetAsync(created.Id);
        });
    }

    [Fact]
    public async Task Should_Activate_And_Deactivate()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Toggle Group",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Link
        });

        await _powerSharingAppService.DeactivateAsync(created.Id);
        var deactivated = await _powerSharingAppService.GetAsync(created.Id);
        deactivated.IsActive.ShouldBeFalse();

        await _powerSharingAppService.ActivateAsync(created.Id);
        var activated = await _powerSharingAppService.GetAsync(created.Id);
        activated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_List_Groups()
    {
        await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "List Group A",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Link
        });
        await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "List Group B",
            MaxCapacityKw = 75m,
            Mode = PowerSharingMode.Loop
        });

        var result = await _powerSharingAppService.GetListAsync(new GetPowerSharingGroupListDto
        {
            PageSize = 50
        });

        result.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Filter_By_Mode()
    {
        await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Filter Link",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Link
        });
        await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Filter Loop",
            MaxCapacityKw = 75m,
            Mode = PowerSharingMode.Loop
        });

        var linkOnly = await _powerSharingAppService.GetListAsync(new GetPowerSharingGroupListDto
        {
            Mode = PowerSharingMode.Link
        });

        linkOnly.ShouldAllBe(g => g.Name.Contains("Link") || g.Mode == PowerSharingMode.Link);
    }

    [Fact]
    public async Task Should_Get_Load_Profiles_Empty()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "LoadProfile Group",
            MaxCapacityKw = 50m,
            Mode = PowerSharingMode.Link
        });

        var profiles = await _powerSharingAppService.GetLoadProfilesAsync(
            created.Id, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        profiles.ShouldNotBeNull();
        profiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Recalculate_Empty_Group()
    {
        var created = await _powerSharingAppService.CreateAsync(new CreatePowerSharingGroupDto
        {
            Name = "Recalc Group",
            MaxCapacityKw = 100m,
            Mode = PowerSharingMode.Link,
            DistributionStrategy = PowerDistributionStrategy.Average
        });

        var allocations = await _powerSharingAppService.RecalculateAsync(created.Id);

        allocations.ShouldBeEmpty();
    }
}
