using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for FeedbackBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class FeedbackBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly FeedbackBffService _service;

    public FeedbackBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<FeedbackBffService>>();
        _service = new FeedbackBffService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task GetFaqs_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var cachedFaqs = new List<FaqDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Question = "Cached FAQ?",
                Answer = "Cached answer.",
                Category = "General",
                Order = 1
            }
        };

        var cacheKey = "support:faq";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FaqDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedFaqs);

        // Act
        var result = await _service.GetFaqsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Question.ShouldBe("Cached FAQ?");

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FaqDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetFaqs_Should_Return_Static_Data_On_Cache_Miss()
    {
        // Arrange
        var cacheKey = "support:faq";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FaqDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<FaqDto>>>>(1);
                return factory();
            });

        // Act
        var result = await _service.GetFaqsAsync();

        // Assert - static FAQ data returned
        result.ShouldNotBeNull();
        result.Count.ShouldBe(6);
        result[0].Question.ShouldBe("How do I start a charging session?");
        result[0].Category.ShouldBe("Charging");
    }

    [Fact]
    public async Task GetAboutInfo_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var cachedAbout = new AboutInfoDto
        {
            AppName = "Cached App",
            Version = "2.0.0",
            CompanyName = "Cached Co",
            Website = "https://cached.test",
            SupportEmail = "cached@test.com",
            SupportPhone = "1900-0000"
        };

        var cacheKey = "support:about";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<AboutInfoDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedAbout);

        // Act
        var result = await _service.GetAboutInfoAsync();

        // Assert
        result.ShouldNotBeNull();
        result.AppName.ShouldBe("Cached App");
        result.Version.ShouldBe("2.0.0");

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<AboutInfoDto>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetAboutInfo_Should_Return_Static_Data_On_Cache_Miss()
    {
        // Arrange
        var cacheKey = "support:about";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<AboutInfoDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<AboutInfoDto>>>(1);
                return factory();
            });

        // Act
        var result = await _service.GetAboutInfoAsync();

        // Assert - static about data returned
        result.ShouldNotBeNull();
        result.AppName.ShouldBe("KLC EV Charging");
        result.Version.ShouldBe("1.0.0");
        result.CompanyName.ShouldBe("KLC Vietnam");
    }

    [Fact]
    public async Task SubmitFeedback_Should_Not_Use_Cache()
    {
        // Arrange - SubmitFeedback writes directly to DB, no cache involved
        var userId = Guid.NewGuid();

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.SubmitFeedbackAsync(userId, new SubmitFeedbackRequest
            {
                Type = FeedbackType.ChargingIssue,
                Subject = "Charger not working",
                Message = "The charger at station X is not responding"
            });

            // Assert
            result.Success.ShouldBeTrue();
            result.FeedbackId.ShouldNotBeNull();
            result.Status.ShouldBe(FeedbackStatus.Open);
        });

        // Verify cache was NOT called for submit
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>());
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<FeedbackResultDto>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetUserFeedback_Should_Bypass_Cache()
    {
        // Arrange - GetUserFeedback uses cursor-based pagination, no cache
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var feedback = new UserFeedback(
                Guid.NewGuid(), userId, FeedbackType.General,
                "Test Inquiry", "This is a test feedback");
            await _dbContext.UserFeedbacks.AddAsync(feedback);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetUserFeedbackAsync(userId, null, 10);

            // Assert
            result.ShouldNotBeNull();
            result.Data.Count.ShouldBe(1);
            result.Data[0].Subject.ShouldBe("Test Inquiry");
        });

        // Verify cache was NOT called for paginated feedback
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<PagedResult<FeedbackDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetFaqs_Should_Use_Correct_Cache_Key()
    {
        // Arrange
        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<FaqDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<FaqDto>());

        // Act
        await _service.GetFaqsAsync();

        // Assert - verify global cache key (not user-specific)
        await _cache.Received(1).GetOrSetAsync(
            "support:faq",
            Arg.Any<Func<Task<List<FaqDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetAboutInfo_Should_Use_Correct_Cache_Key()
    {
        // Arrange
        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<AboutInfoDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(new AboutInfoDto());

        // Act
        await _service.GetAboutInfoAsync();

        // Assert - verify global cache key (not user-specific)
        await _cache.Received(1).GetOrSetAsync(
            "support:about",
            Arg.Any<Func<Task<AboutInfoDto>>>(),
            Arg.Any<TimeSpan?>());
    }
}
