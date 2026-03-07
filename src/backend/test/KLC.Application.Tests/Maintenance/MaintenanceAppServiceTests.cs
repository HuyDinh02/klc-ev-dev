using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Stations;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Maintenance;

public abstract class MaintenanceAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IMaintenanceAppService _maintenanceAppService;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    protected MaintenanceAppServiceTests()
    {
        _maintenanceAppService = GetRequiredService<IMaintenanceAppService>();
        _stationRepository = GetRequiredService<IRepository<ChargingStation, Guid>>();
    }

    private async Task<Guid> CreateTestStationAsync()
    {
        var station = new ChargingStation(
            Guid.NewGuid(), $"MNT-{Guid.NewGuid():N}"[..12], "Maintenance Test Station",
            "123 Maintenance St", 21.03, 105.85);
        await _stationRepository.InsertAsync(station);
        return station.Id;
    }

    [Fact]
    public async Task Should_Create_Maintenance_Task()
    {
        var stationId = await CreateTestStationAsync();

        var result = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Quarterly inspection",
            AssignedTo = "Technician A",
            ScheduledDate = DateTime.UtcNow.AddDays(7),
            Description = "Check all connectors"
        });

        result.Id.ShouldNotBe(Guid.Empty);
        result.Title.ShouldBe("Quarterly inspection");
        result.Status.ShouldBe(MaintenanceTaskStatus.Planned);
        result.StationName.ShouldBe("Maintenance Test Station");
        result.AssignedTo.ShouldBe("Technician A");
        result.Description.ShouldBe("Check all connectors");
    }

    [Fact]
    public async Task Should_Throw_When_Creating_With_Invalid_Station()
    {
        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
            {
                StationId = Guid.NewGuid(), // Non-existent station
                Type = MaintenanceTaskType.Emergency,
                Title = "Emergency fix",
                AssignedTo = "Tech B",
                ScheduledDate = DateTime.UtcNow
            });
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.NotFound);
    }

    [Fact]
    public async Task Should_Get_Task_By_Id()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Inspection,
            Title = "Get by ID test",
            AssignedTo = "Tech C",
            ScheduledDate = DateTime.UtcNow.AddDays(3)
        });

        var task = await _maintenanceAppService.GetAsync(created.Id);

        task.Id.ShouldBe(created.Id);
        task.Title.ShouldBe("Get by ID test");
        task.Type.ShouldBe(MaintenanceTaskType.Inspection);
    }

    [Fact]
    public async Task Should_List_Tasks_With_Filtering()
    {
        var stationId = await CreateTestStationAsync();
        await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Scheduled task",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(7)
        });
        await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Emergency,
            Title = "Emergency task",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow
        });

        var allTasks = await _maintenanceAppService.GetListAsync(new GetMaintenanceTaskListDto
        {
            StationId = stationId
        });
        allTasks.TotalCount.ShouldBeGreaterThanOrEqualTo(2);

        var emergencyOnly = await _maintenanceAppService.GetListAsync(new GetMaintenanceTaskListDto
        {
            Type = MaintenanceTaskType.Emergency,
            StationId = stationId
        });
        emergencyOnly.Items.ShouldAllBe(t => t.Type == MaintenanceTaskType.Emergency);
    }

    [Fact]
    public async Task Should_Start_Planned_Task()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Task to start",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        });

        var started = await _maintenanceAppService.StartAsync(created.Id);

        started.Status.ShouldBe(MaintenanceTaskStatus.InProgress);
        started.StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Complete_InProgress_Task()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Task to complete",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        });
        await _maintenanceAppService.StartAsync(created.Id);

        var completed = await _maintenanceAppService.CompleteAsync(created.Id,
            new CompleteMaintenanceTaskDto { Notes = "All connectors checked" });

        completed.Status.ShouldBe(MaintenanceTaskStatus.Completed);
        completed.CompletedAt.ShouldNotBeNull();
        completed.Notes.ShouldBe("All connectors checked");
    }

    [Fact]
    public async Task Should_Cancel_Planned_Task()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Task to cancel",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        });

        var cancelled = await _maintenanceAppService.CancelAsync(created.Id,
            new CancelMaintenanceTaskDto { Notes = "No longer needed" });

        cancelled.Status.ShouldBe(MaintenanceTaskStatus.Cancelled);
        cancelled.Notes.ShouldBe("No longer needed");
    }

    [Fact]
    public async Task Should_Throw_When_Starting_Already_Started_Task()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Double start test",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(1)
        });
        await _maintenanceAppService.StartAsync(created.Id);

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _maintenanceAppService.StartAsync(created.Id);
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Maintenance.InvalidStateTransition);
    }

    [Fact]
    public async Task Should_Update_Task_Fields()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Original title",
            AssignedTo = "Tech A",
            ScheduledDate = DateTime.UtcNow.AddDays(7)
        });

        var updated = await _maintenanceAppService.UpdateAsync(created.Id, new UpdateMaintenanceTaskDto
        {
            Title = "Updated title",
            AssignedTo = "Tech B",
            Description = "New description"
        });

        updated.Title.ShouldBe("Updated title");
        updated.AssignedTo.ShouldBe("Tech B");
        updated.Description.ShouldBe("New description");
    }

    [Fact]
    public async Task Should_Get_Stats()
    {
        var stationId = await CreateTestStationAsync();

        // Create tasks in different states
        var task1 = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Planned task",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(7)
        });

        var task2 = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Inspection,
            Title = "In progress task",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(3)
        });
        await _maintenanceAppService.StartAsync(task2.Id);

        var stats = await _maintenanceAppService.GetStatsAsync();

        stats.PlannedCount.ShouldBeGreaterThanOrEqualTo(1);
        stats.InProgressCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Should_Delete_Task()
    {
        var stationId = await CreateTestStationAsync();
        var created = await _maintenanceAppService.CreateAsync(new CreateMaintenanceTaskDto
        {
            StationId = stationId,
            Type = MaintenanceTaskType.Scheduled,
            Title = "Task to delete",
            AssignedTo = "Tech",
            ScheduledDate = DateTime.UtcNow.AddDays(7)
        });

        await _maintenanceAppService.DeleteAsync(created.Id);

        // Should throw when trying to get deleted task
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _maintenanceAppService.GetAsync(created.Id);
        });
    }
}
