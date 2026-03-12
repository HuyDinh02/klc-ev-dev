using System;
using KLC.Enums;
using Shouldly;
using Xunit;

namespace KLC.Operators;

public class OperatorWebhookLogTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var log = new OperatorWebhookLog(
            id,
            operatorId,
            WebhookEventType.SessionCompleted,
            "{\"sessionId\":\"abc\"}",
            httpStatusCode: 200,
            success: true,
            attemptCount: 1);

        log.Id.ShouldBe(id);
        log.OperatorId.ShouldBe(operatorId);
        log.EventType.ShouldBe(WebhookEventType.SessionCompleted);
        log.PayloadJson.ShouldBe("{\"sessionId\":\"abc\"}");
        log.HttpStatusCode.ShouldBe(200);
        log.Success.ShouldBeTrue();
        log.ErrorMessage.ShouldBeNull();
        log.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void Create_With_Error_Should_Set_Failure()
    {
        var id = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var log = new OperatorWebhookLog(
            id,
            operatorId,
            WebhookEventType.FaultDetected,
            "{\"fault\":\"overcurrent\"}",
            httpStatusCode: 500,
            success: false,
            errorMessage: "Internal Server Error",
            attemptCount: 3);

        log.Success.ShouldBeFalse();
        log.HttpStatusCode.ShouldBe(500);
        log.ErrorMessage.ShouldBe("Internal Server Error");
        log.AttemptCount.ShouldBe(3);
    }

    [Fact]
    public void Create_With_No_Response_Should_Have_Null_StatusCode()
    {
        var log = new OperatorWebhookLog(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WebhookEventType.StationOffline,
            "{\"stationId\":\"123\"}",
            httpStatusCode: null,
            success: false,
            errorMessage: "Connection timeout");

        log.HttpStatusCode.ShouldBeNull();
        log.Success.ShouldBeFalse();
        log.ErrorMessage.ShouldBe("Connection timeout");
    }
}
