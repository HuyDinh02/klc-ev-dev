using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Operators;

public class OperatorTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var op = new Operator(id, "Test Operator", "hash123", "test@example.com", "A description", 500);

        op.Id.ShouldBe(id);
        op.Name.ShouldBe("Test Operator");
        op.ApiKeyHash.ShouldBe("hash123");
        op.ContactEmail.ShouldBe("test@example.com");
        op.Description.ShouldBe("A description");
        op.RateLimitPerMinute.ShouldBe(500);
        op.IsActive.ShouldBeTrue();
        op.WebhookUrl.ShouldBeNull();
        op.AllowedStations.ShouldBeEmpty();
    }

    [Fact]
    public void Create_Should_Default_RateLimit_To_1000()
    {
        var op = new Operator(Guid.NewGuid(), "Op", "hash", "e@e.com");

        op.RateLimitPerMinute.ShouldBe(1000);
    }

    [Fact]
    public void Create_Should_Reject_Empty_Name()
    {
        Should.Throw<ArgumentException>(() =>
            new Operator(Guid.NewGuid(), "", "hash", "e@e.com"));
    }

    [Fact]
    public void Create_Should_Reject_Whitespace_Name()
    {
        Should.Throw<ArgumentException>(() =>
            new Operator(Guid.NewGuid(), "   ", "hash", "e@e.com"));
    }

    [Fact]
    public void SetName_Should_Update()
    {
        var op = CreateOperator();
        op.SetName("New Name");

        op.Name.ShouldBe("New Name");
    }

    [Fact]
    public void SetName_Should_Reject_Empty()
    {
        var op = CreateOperator();

        Should.Throw<ArgumentException>(() => op.SetName(""));
    }

    [Fact]
    public void SetWebhookUrl_Should_Update()
    {
        var op = CreateOperator();
        op.SetWebhookUrl("https://webhook.example.com/events");

        op.WebhookUrl.ShouldBe("https://webhook.example.com/events");
    }

    [Fact]
    public void SetWebhookUrl_Should_Allow_Null()
    {
        var op = CreateOperator();
        op.SetWebhookUrl("https://webhook.example.com");
        op.SetWebhookUrl(null);

        op.WebhookUrl.ShouldBeNull();
    }

    [Fact]
    public void SetRateLimit_Should_Update()
    {
        var op = CreateOperator();
        op.SetRateLimit(200);

        op.RateLimitPerMinute.ShouldBe(200);
    }

    [Fact]
    public void SetRateLimit_Should_Default_To_1000_When_Zero()
    {
        var op = CreateOperator();
        op.SetRateLimit(0);

        op.RateLimitPerMinute.ShouldBe(1000);
    }

    [Fact]
    public void SetRateLimit_Should_Default_To_1000_When_Negative()
    {
        var op = CreateOperator();
        op.SetRateLimit(-5);

        op.RateLimitPerMinute.ShouldBe(1000);
    }

    [Fact]
    public void Activate_Deactivate_Should_Toggle()
    {
        var op = CreateOperator();
        op.IsActive.ShouldBeTrue();

        op.Deactivate();
        op.IsActive.ShouldBeFalse();

        op.Activate();
        op.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void AddStation_Should_Add_To_Collection()
    {
        var op = CreateOperator();
        var stationId = Guid.NewGuid();
        var osId = Guid.NewGuid();

        var result = op.AddStation(osId, stationId);

        op.AllowedStations.Count.ShouldBe(1);
        result.OperatorId.ShouldBe(op.Id);
        result.StationId.ShouldBe(stationId);
    }

    [Fact]
    public void AddStation_Should_Reject_Duplicate()
    {
        var op = CreateOperator();
        var stationId = Guid.NewGuid();
        op.AddStation(Guid.NewGuid(), stationId);

        var ex = Should.Throw<BusinessException>(() =>
            op.AddStation(Guid.NewGuid(), stationId));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Operators.StationAlreadyAssigned);
    }

    [Fact]
    public void RemoveStation_Should_Soft_Delete()
    {
        var op = CreateOperator();
        var stationId = Guid.NewGuid();
        op.AddStation(Guid.NewGuid(), stationId);

        op.RemoveStation(stationId);

        // Station is soft deleted, so HasStationAccess should return false
        op.HasStationAccess(stationId).ShouldBeFalse();
    }

    [Fact]
    public void RemoveStation_Should_Throw_If_Not_Found()
    {
        var op = CreateOperator();

        var ex = Should.Throw<BusinessException>(() =>
            op.RemoveStation(Guid.NewGuid()));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Operators.StationNotAssigned);
    }

    [Fact]
    public void HasStationAccess_Should_Return_True_For_Assigned()
    {
        var op = CreateOperator();
        var stationId = Guid.NewGuid();
        op.AddStation(Guid.NewGuid(), stationId);

        op.HasStationAccess(stationId).ShouldBeTrue();
    }

    [Fact]
    public void HasStationAccess_Should_Return_False_For_Unassigned()
    {
        var op = CreateOperator();

        op.HasStationAccess(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void SetApiKeyHash_Should_Update()
    {
        var op = CreateOperator();
        op.SetApiKeyHash("newhash456");

        op.ApiKeyHash.ShouldBe("newhash456");
    }

    [Fact]
    public void SetApiKeyHash_Should_Reject_Empty()
    {
        var op = CreateOperator();

        Should.Throw<ArgumentException>(() => op.SetApiKeyHash(""));
    }

    [Fact]
    public void AddStation_Should_Allow_Re_Adding_After_Soft_Delete()
    {
        var op = CreateOperator();
        var stationId = Guid.NewGuid();

        op.AddStation(Guid.NewGuid(), stationId);
        op.RemoveStation(stationId);

        // Should be able to re-add after soft delete
        var result = op.AddStation(Guid.NewGuid(), stationId);
        result.StationId.ShouldBe(stationId);
        op.HasStationAccess(stationId).ShouldBeTrue();
    }

    private static Operator CreateOperator()
    {
        return new Operator(
            Guid.NewGuid(),
            "Test Operator",
            "testhash",
            "contact@example.com",
            "Test description",
            1000);
    }
}
