using System;
using System.Linq;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.PowerSharing;

public class PowerSharingGroupTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var group = CreateGroup();

        group.Name.ShouldBe("Site A");
        group.MaxCapacityKw.ShouldBe(150m);
        group.Mode.ShouldBe(PowerSharingMode.Link);
        group.DistributionStrategy.ShouldBe(PowerDistributionStrategy.Average);
        group.IsActive.ShouldBeTrue();
        group.MinPowerPerConnectorKw.ShouldBe(1.4m);
    }

    [Fact]
    public void Create_With_Loop_Mode_Should_Work()
    {
        var group = new PowerSharingGroup(
            Guid.NewGuid(), "Regional Loop", 500m, PowerSharingMode.Loop,
            PowerDistributionStrategy.Dynamic, 2.0m);

        group.Mode.ShouldBe(PowerSharingMode.Loop);
        group.DistributionStrategy.ShouldBe(PowerDistributionStrategy.Dynamic);
        group.MinPowerPerConnectorKw.ShouldBe(2.0m);
    }

    [Fact]
    public void SetMaxCapacity_Should_Reject_Zero()
    {
        var group = CreateGroup();

        Should.Throw<BusinessException>(() => group.SetMaxCapacity(0))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.InvalidCapacity);
    }

    [Fact]
    public void SetMaxCapacity_Should_Reject_Negative()
    {
        var group = CreateGroup();

        Should.Throw<BusinessException>(() => group.SetMaxCapacity(-100))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.InvalidCapacity);
    }

    [Fact]
    public void SetMaxCapacity_Should_Accept_Valid_Value()
    {
        var group = CreateGroup();

        group.SetMaxCapacity(200m);

        group.MaxCapacityKw.ShouldBe(200m);
    }

    [Fact]
    public void SetMinPowerPerConnector_Should_Reject_Negative()
    {
        var group = CreateGroup();

        Should.Throw<BusinessException>(() => group.SetMinPowerPerConnector(-1))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.InvalidMinPower);
    }

    [Fact]
    public void SetMinPowerPerConnector_Should_Accept_Zero()
    {
        var group = CreateGroup();

        group.SetMinPowerPerConnector(0);

        group.MinPowerPerConnectorKw.ShouldBe(0);
    }

    [Fact]
    public void SetName_Should_Reject_Empty()
    {
        var group = CreateGroup();

        Should.Throw<ArgumentException>(() => group.SetName(""));
    }

    [Fact]
    public void SetDistributionStrategy_Should_Change_Strategy()
    {
        var group = CreateGroup();

        group.SetDistributionStrategy(PowerDistributionStrategy.Proportional);

        group.DistributionStrategy.ShouldBe(PowerDistributionStrategy.Proportional);
    }

    [Fact]
    public void SetMode_Should_Change_Mode()
    {
        var group = CreateGroup();

        group.SetMode(PowerSharingMode.Loop);

        group.Mode.ShouldBe(PowerSharingMode.Loop);
    }

    [Fact]
    public void Activate_Deactivate_Should_Toggle()
    {
        var group = CreateGroup();

        group.Deactivate();
        group.IsActive.ShouldBeFalse();

        group.Activate();
        group.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void AddMember_Should_Add_Connector()
    {
        var group = CreateGroup();
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();

        var member = group.AddMember(Guid.NewGuid(), stationId, connectorId, 1);

        member.ShouldNotBeNull();
        member.StationId.ShouldBe(stationId);
        member.ConnectorId.ShouldBe(connectorId);
        member.Priority.ShouldBe(1);
        member.AllocatedPowerKw.ShouldBe(0);
        group.Members.Count.ShouldBe(1);
    }

    [Fact]
    public void AddMember_Should_Reject_Duplicate_Connector()
    {
        var group = CreateGroup();
        var connectorId = Guid.NewGuid();
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), connectorId);

        Should.Throw<BusinessException>(() =>
            group.AddMember(Guid.NewGuid(), Guid.NewGuid(), connectorId))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.ConnectorAlreadyInGroup);
    }

    [Fact]
    public void AddMember_Link_Mode_Should_Enforce_Max_10()
    {
        var group = CreateGroup();

        for (int i = 0; i < 10; i++)
            group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Should.Throw<BusinessException>(() =>
            group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.MaxMembersExceeded);
    }

    [Fact]
    public void AddMember_Loop_Mode_Should_Allow_More_Than_10()
    {
        var group = new PowerSharingGroup(
            Guid.NewGuid(), "Loop", 1000m, PowerSharingMode.Loop);

        for (int i = 0; i < 15; i++)
            group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        group.Members.Count.ShouldBe(15);
    }

    [Fact]
    public void RemoveMember_Should_Soft_Delete()
    {
        var group = CreateGroup();
        var connectorId = Guid.NewGuid();
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), connectorId);

        group.RemoveMember(connectorId);

        group.Members.Count.ShouldBe(1); // Still in collection
        group.GetActiveMembers().Count.ShouldBe(0); // But not active
    }

    [Fact]
    public void RemoveMember_Should_Throw_If_Not_Found()
    {
        var group = CreateGroup();

        Should.Throw<BusinessException>(() =>
            group.RemoveMember(Guid.NewGuid()))
            .Code.ShouldBe(KLCDomainErrorCodes.PowerSharing.ConnectorNotInGroup);
    }

    [Fact]
    public void RemoveMember_Should_Allow_Readd_After_Removal()
    {
        var group = CreateGroup();
        var connectorId = Guid.NewGuid();
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), connectorId);
        group.RemoveMember(connectorId);

        // Should be able to re-add since previous was soft-deleted
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), connectorId);

        group.GetActiveMembers().Count.ShouldBe(1);
    }

    [Fact]
    public void GetActiveMembers_Should_Exclude_Deleted()
    {
        var group = CreateGroup();
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var c3 = Guid.NewGuid();
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), c1);
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), c2);
        group.AddMember(Guid.NewGuid(), Guid.NewGuid(), c3);

        group.RemoveMember(c2);

        group.GetActiveMembers().Count.ShouldBe(2);
        group.GetActiveMembers().ShouldAllBe(m => m.ConnectorId != c2);
    }

    [Fact]
    public void Member_SetPriority_Should_Update()
    {
        var group = CreateGroup();
        var member = group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        member.SetPriority(5);

        member.Priority.ShouldBe(5);
    }

    [Fact]
    public void Member_UpdateAllocatedPower_Should_Clamp_At_Zero()
    {
        var group = CreateGroup();
        var member = group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        member.UpdateAllocatedPower(-10);

        member.AllocatedPowerKw.ShouldBe(0);
    }

    [Fact]
    public void Member_UpdateAllocatedPower_Should_Accept_Positive()
    {
        var group = CreateGroup();
        var member = group.AddMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        member.UpdateAllocatedPower(22.5m);

        member.AllocatedPowerKw.ShouldBe(22.5m);
    }

    private static PowerSharingGroup CreateGroup(
        PowerSharingMode mode = PowerSharingMode.Link,
        decimal maxCapacity = 150m)
    {
        return new PowerSharingGroup(
            Guid.NewGuid(), "Site A", maxCapacity, mode);
    }
}
