using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Fleets;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Fleets;

public abstract class FleetAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IFleetAppService _fleetAppService;

    protected FleetAppServiceTests()
    {
        _fleetAppService = GetRequiredService<IFleetAppService>();
    }

    [Fact]
    public async Task Should_Create_Fleet_With_Defaults()
    {
        var result = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Test Fleet",
            Description = "Test description",
            MaxMonthlyBudgetVnd = 5_000_000m
        });

        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Test Fleet");
        result.Description.ShouldBe("Test description");
        result.MaxMonthlyBudgetVnd.ShouldBe(5_000_000m);
        result.ChargingPolicy.ShouldBe(ChargingPolicyType.AnytimeAnywhere);
        result.BudgetAlertThresholdPercent.ShouldBe(80);
        result.IsActive.ShouldBeTrue();
        result.VehicleCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Create_Fleet_With_Custom_Policy()
    {
        var result = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Scheduled Fleet",
            MaxMonthlyBudgetVnd = 10_000_000m,
            ChargingPolicy = ChargingPolicyType.ScheduledOnly,
            BudgetAlertThresholdPercent = 90
        });

        result.ChargingPolicy.ShouldBe(ChargingPolicyType.ScheduledOnly);
        result.BudgetAlertThresholdPercent.ShouldBe(90);
    }

    [Fact]
    public async Task Should_Get_Fleet_By_Id()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Get Fleet Test",
            MaxMonthlyBudgetVnd = 1_000_000m
        });

        var fleet = await _fleetAppService.GetAsync(created.Id);

        fleet.Name.ShouldBe("Get Fleet Test");
        fleet.Vehicles.ShouldNotBeNull();
        fleet.Vehicles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Update_Fleet()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Update Fleet",
            MaxMonthlyBudgetVnd = 1_000_000m
        });

        var updated = await _fleetAppService.UpdateAsync(created.Id, new UpdateFleetDto
        {
            Name = "Updated Fleet Name",
            Description = "New description",
            MaxMonthlyBudgetVnd = 8_000_000m,
            ChargingPolicy = ChargingPolicyType.ApprovedStationsOnly,
            BudgetAlertThresholdPercent = 95
        });

        updated.Name.ShouldBe("Updated Fleet Name");
        updated.MaxMonthlyBudgetVnd.ShouldBe(8_000_000m);
        updated.ChargingPolicy.ShouldBe(ChargingPolicyType.ApprovedStationsOnly);
        updated.BudgetAlertThresholdPercent.ShouldBe(95);
    }

    [Fact]
    public async Task Should_Delete_Fleet()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Delete Fleet",
            MaxMonthlyBudgetVnd = 0
        });

        await _fleetAppService.DeleteAsync(created.Id);

        // ABP soft delete — entity is gone from queries
        var list = await _fleetAppService.GetListAsync(new GetFleetListDto { PageSize = 100 });
        list.Items.ShouldNotContain(f => f.Id == created.Id);
    }

    [Fact]
    public async Task Should_List_Fleets()
    {
        await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "List Fleet A",
            MaxMonthlyBudgetVnd = 1_000_000m
        });
        await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "List Fleet B",
            MaxMonthlyBudgetVnd = 2_000_000m
        });

        var result = await _fleetAppService.GetListAsync(new GetFleetListDto { PageSize = 50 });

        result.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Search_Fleets_By_Name()
    {
        await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "FleetSearch Alpha",
            MaxMonthlyBudgetVnd = 0
        });
        await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "FleetSearch Beta",
            MaxMonthlyBudgetVnd = 0
        });

        var result = await _fleetAppService.GetListAsync(new GetFleetListDto
        {
            Search = "Alpha"
        });

        result.Items.ShouldContain(f => f.Name.Contains("Alpha"));
        result.Items.ShouldNotContain(f => f.Name.Contains("Beta"));
    }

    [Fact]
    public async Task Should_Add_Schedule()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Schedule Fleet",
            MaxMonthlyBudgetVnd = 0
        });

        var schedule = await _fleetAppService.AddScheduleAsync(created.Id, new CreateFleetScheduleDto
        {
            DayOfWeek = 1, // Monday
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(17)
        });

        schedule.DayOfWeek.ShouldBe(1);
        schedule.StartTimeUtc.ShouldBe(TimeSpan.FromHours(8));
        schedule.EndTimeUtc.ShouldBe(TimeSpan.FromHours(17));
    }

    [Fact]
    public async Task Should_Get_Schedules()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Get Schedules Fleet",
            MaxMonthlyBudgetVnd = 0
        });

        await _fleetAppService.AddScheduleAsync(created.Id, new CreateFleetScheduleDto
        {
            DayOfWeek = 1,
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(17)
        });
        await _fleetAppService.AddScheduleAsync(created.Id, new CreateFleetScheduleDto
        {
            DayOfWeek = 3,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(18)
        });

        var schedules = await _fleetAppService.GetSchedulesAsync(created.Id);

        schedules.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Remove_Schedule()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Remove Schedule Fleet",
            MaxMonthlyBudgetVnd = 0
        });

        var schedule = await _fleetAppService.AddScheduleAsync(created.Id, new CreateFleetScheduleDto
        {
            DayOfWeek = 5,
            StartTimeUtc = TimeSpan.FromHours(6),
            EndTimeUtc = TimeSpan.FromHours(22)
        });

        await _fleetAppService.RemoveScheduleAsync(created.Id, schedule.Id);

        var schedules = await _fleetAppService.GetSchedulesAsync(created.Id);
        schedules.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Get_Analytics_Empty()
    {
        var created = await _fleetAppService.CreateAsync(new CreateFleetDto
        {
            Name = "Analytics Fleet",
            MaxMonthlyBudgetVnd = 0
        });

        var analytics = await _fleetAppService.GetAnalyticsAsync(created.Id);

        analytics.TotalEnergyKwh.ShouldBe(0);
        analytics.TotalCostVnd.ShouldBe(0);
        analytics.SessionCount.ShouldBe(0);
        analytics.TopVehicles.ShouldBeEmpty();
        analytics.TopStations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Reject_Empty_Fleet_Name()
    {
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _fleetAppService.CreateAsync(new CreateFleetDto
            {
                Name = "",
                MaxMonthlyBudgetVnd = 0
            });
        });
    }
}
