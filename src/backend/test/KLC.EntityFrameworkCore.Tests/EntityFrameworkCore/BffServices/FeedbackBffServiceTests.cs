using System;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Support;
using KLC.TestDoubles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class FeedbackBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly FeedbackBffService _service;

    public FeedbackBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        var cache = new PassthroughCacheService();
        var logger = Substitute.For<ILogger<FeedbackBffService>>();
        _service = new FeedbackBffService(_dbContext, cache, logger);
    }

    [Fact]
    public async Task SubmitFeedback_Should_Create_Feedback()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.SubmitFeedbackAsync(userId, new SubmitFeedbackRequest
            {
                Type = FeedbackType.ChargingIssue,
                Subject = "Charger broken",
                Message = "The connector at station X is damaged"
            });

            result.Success.ShouldBeTrue();
            result.FeedbackId.ShouldNotBeNull();
            result.Status.ShouldBe(FeedbackStatus.Open);
        });
    }

    [Fact]
    public async Task GetUserFeedback_Should_Return_User_Submissions()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.UserFeedbacks.AddRangeAsync(
                new UserFeedback(Guid.NewGuid(), userId, FeedbackType.ChargingIssue, "Issue 1", "Detail 1"),
                new UserFeedback(Guid.NewGuid(), userId, FeedbackType.PaymentIssue, "Issue 2", "Detail 2"),
                new UserFeedback(Guid.NewGuid(), Guid.NewGuid(), FeedbackType.General, "Other user", "Not mine"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetUserFeedbackAsync(userId, null, 10);

            result.Data.Count.ShouldBe(2);
            result.HasMore.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetUserFeedback_Should_Return_Empty_When_No_Feedback()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetUserFeedbackAsync(Guid.NewGuid(), null, 10);

            result.Data.ShouldBeEmpty();
            result.HasMore.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetUserFeedback_Should_Paginate()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await _dbContext.UserFeedbacks.AddAsync(
                    new UserFeedback(Guid.NewGuid(), userId, FeedbackType.General, $"Issue {i}", $"Detail {i}"));
            }
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetUserFeedbackAsync(userId, null, 3);

            result.Data.Count.ShouldBe(3);
            result.HasMore.ShouldBeTrue();
            result.NextCursor.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task GetFaqs_Should_Return_Static_Data()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFaqsAsync();

            result.ShouldNotBeEmpty();
            result.Count.ShouldBeGreaterThan(0);
            result[0].Question.ShouldNotBeNullOrEmpty();
            result[0].Answer.ShouldNotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetAboutInfo_Should_Return_App_Info()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetAboutInfoAsync();

            result.AppName.ShouldBe("KLC EV Charging");
            result.CompanyName.ShouldNotBeNullOrEmpty();
            result.SupportEmail.ShouldNotBeNullOrEmpty();
        });
    }

}
