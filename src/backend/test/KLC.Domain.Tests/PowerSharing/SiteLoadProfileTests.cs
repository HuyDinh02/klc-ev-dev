using System;
using Shouldly;
using Xunit;

namespace KLC.PowerSharing;

public class SiteLoadProfileTests
{
    [Fact]
    public void Create_Should_Set_All_Properties()
    {
        var groupId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var profile = new SiteLoadProfile(
            Guid.NewGuid(), groupId, timestamp,
            totalLoadKw: 120m,
            availableCapacityKw: 30m,
            activeSessionCount: 4,
            totalConnectorCount: 6,
            peakLoadKw: 45m);

        profile.PowerSharingGroupId.ShouldBe(groupId);
        profile.Timestamp.ShouldBe(timestamp);
        profile.TotalLoadKw.ShouldBe(120m);
        profile.AvailableCapacityKw.ShouldBe(30m);
        profile.ActiveSessionCount.ShouldBe(4);
        profile.TotalConnectorCount.ShouldBe(6);
        profile.PeakLoadKw.ShouldBe(45m);
    }

    [Fact]
    public void Create_With_Zero_Load_Should_Work()
    {
        var profile = new SiteLoadProfile(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            totalLoadKw: 0,
            availableCapacityKw: 150m,
            activeSessionCount: 0,
            totalConnectorCount: 6,
            peakLoadKw: 0);

        profile.TotalLoadKw.ShouldBe(0);
        profile.ActiveSessionCount.ShouldBe(0);
    }
}
