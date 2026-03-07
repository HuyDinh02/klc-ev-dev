using System;
using KLC.Enums;
using Shouldly;
using Xunit;

namespace KLC.Support;

public class UserFeedbackTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Status_Open()
    {
        var userId = Guid.NewGuid();

        var feedback = CreateFeedback(userId: userId);

        feedback.UserId.ShouldBe(userId);
        feedback.Type.ShouldBe(FeedbackType.ChargingIssue);
        feedback.Subject.ShouldBe("Charger not working");
        feedback.Message.ShouldBe("The charger at station A stopped mid-charge.");
        feedback.Status.ShouldBe(FeedbackStatus.Open);
        feedback.AdminResponse.ShouldBeNull();
        feedback.RespondedAt.ShouldBeNull();
        feedback.RespondedBy.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_Subject()
    {
        Should.Throw<ArgumentException>(() =>
            new UserFeedback(
                Guid.NewGuid(),
                Guid.NewGuid(),
                FeedbackType.Bug,
                "",
                "Some message"));
    }

    [Fact]
    public void Constructor_Should_Throw_For_Empty_Message()
    {
        Should.Throw<ArgumentException>(() =>
            new UserFeedback(
                Guid.NewGuid(),
                Guid.NewGuid(),
                FeedbackType.Bug,
                "Subject",
                ""));
    }

    [Fact]
    public void SetInReview_Should_Change_Status_To_InReview()
    {
        var feedback = CreateFeedback();

        feedback.SetInReview();

        feedback.Status.ShouldBe(FeedbackStatus.InReview);
    }

    [Fact]
    public void Resolve_Should_Set_AdminResponse_And_Status()
    {
        var feedback = CreateFeedback();
        var adminId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        feedback.Resolve("We have fixed the charger.", adminId);

        feedback.Status.ShouldBe(FeedbackStatus.Resolved);
        feedback.AdminResponse.ShouldBe("We have fixed the charger.");
        feedback.RespondedBy.ShouldBe(adminId);
        feedback.RespondedAt.ShouldNotBeNull();
        feedback.RespondedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        feedback.RespondedAt!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void Resolve_Should_Throw_For_Empty_AdminResponse()
    {
        var feedback = CreateFeedback();

        Should.Throw<ArgumentException>(() =>
            feedback.Resolve("", Guid.NewGuid()));
    }

    [Fact]
    public void Close_Should_Set_Status_To_Closed()
    {
        var feedback = CreateFeedback();

        feedback.Close();

        feedback.Status.ShouldBe(FeedbackStatus.Closed);
    }

    [Fact]
    public void Full_Feedback_Lifecycle_Should_Work()
    {
        var feedback = CreateFeedback();
        var adminId = Guid.NewGuid();

        feedback.Status.ShouldBe(FeedbackStatus.Open);

        feedback.SetInReview();
        feedback.Status.ShouldBe(FeedbackStatus.InReview);

        feedback.Resolve("Issue has been resolved.", adminId);
        feedback.Status.ShouldBe(FeedbackStatus.Resolved);
        feedback.AdminResponse.ShouldBe("Issue has been resolved.");

        feedback.Close();
        feedback.Status.ShouldBe(FeedbackStatus.Closed);
    }

    private static UserFeedback CreateFeedback(Guid? userId = null)
    {
        return new UserFeedback(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            FeedbackType.ChargingIssue,
            "Charger not working",
            "The charger at station A stopped mid-charge.");
    }
}
