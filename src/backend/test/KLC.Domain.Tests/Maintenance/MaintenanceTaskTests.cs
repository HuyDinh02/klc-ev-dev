using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Maintenance;

public class MaintenanceTaskTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        var task = new MaintenanceTask(
            id, stationId, MaintenanceTaskType.Scheduled,
            "Quarterly check", "John", DateTime.UtcNow.AddDays(7),
            connectorNumber: 1, description: "Check connectors");

        task.Id.ShouldBe(id);
        task.StationId.ShouldBe(stationId);
        task.Type.ShouldBe(MaintenanceTaskType.Scheduled);
        task.Status.ShouldBe(MaintenanceTaskStatus.Planned);
        task.Title.ShouldBe("Quarterly check");
        task.AssignedTo.ShouldBe("John");
        task.ConnectorNumber.ShouldBe(1);
        task.Description.ShouldBe("Check connectors");
        task.StartedAt.ShouldBeNull();
        task.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void Start_From_Planned_Should_Set_InProgress()
    {
        var task = CreateTask();

        task.Start();

        task.Status.ShouldBe(MaintenanceTaskStatus.InProgress);
        task.StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Start_From_InProgress_Should_Throw()
    {
        var task = CreateTask();
        task.Start();

        var ex = Should.Throw<BusinessException>(() => task.Start());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Maintenance.InvalidStateTransition);
    }

    [Fact]
    public void Complete_From_InProgress_Should_Set_Completed()
    {
        var task = CreateTask();
        task.Start();

        task.Complete("All good");

        task.Status.ShouldBe(MaintenanceTaskStatus.Completed);
        task.CompletedAt.ShouldNotBeNull();
        task.Notes.ShouldBe("All good");
    }

    [Fact]
    public void Complete_From_Planned_Should_Throw()
    {
        var task = CreateTask();

        var ex = Should.Throw<BusinessException>(() => task.Complete());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Maintenance.InvalidStateTransition);
    }

    [Fact]
    public void Cancel_From_Planned_Should_Succeed()
    {
        var task = CreateTask();

        task.Cancel("No longer needed");

        task.Status.ShouldBe(MaintenanceTaskStatus.Cancelled);
        task.Notes.ShouldBe("No longer needed");
    }

    [Fact]
    public void Cancel_From_Completed_Should_Throw()
    {
        var task = CreateTask();
        task.Start();
        task.Complete();

        var ex = Should.Throw<BusinessException>(() => task.Cancel());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Maintenance.InvalidStateTransition);
    }

    [Fact]
    public void IsOverdue_Should_Return_True_When_Past_Due_And_Planned()
    {
        var task = new MaintenanceTask(
            Guid.NewGuid(), Guid.NewGuid(),
            MaintenanceTaskType.Scheduled,
            "Overdue task", "Tech",
            DateTime.UtcNow.AddDays(-1));

        task.IsOverdue().ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_Should_Return_False_When_InProgress()
    {
        var task = new MaintenanceTask(
            Guid.NewGuid(), Guid.NewGuid(),
            MaintenanceTaskType.Scheduled,
            "Started task", "Tech",
            DateTime.UtcNow.AddDays(-1));
        task.Start();

        task.IsOverdue().ShouldBeFalse();
    }

    [Fact]
    public void Update_Should_Change_Fields()
    {
        var task = CreateTask();

        task.Update("New Title", "New desc", "Jane", DateTime.UtcNow.AddDays(14));

        task.Title.ShouldBe("New Title");
        task.Description.ShouldBe("New desc");
        task.AssignedTo.ShouldBe("Jane");
    }

    private static MaintenanceTask CreateTask()
    {
        return new MaintenanceTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            MaintenanceTaskType.Scheduled,
            "Test maintenance",
            "Technician",
            DateTime.UtcNow.AddDays(7));
    }
}
