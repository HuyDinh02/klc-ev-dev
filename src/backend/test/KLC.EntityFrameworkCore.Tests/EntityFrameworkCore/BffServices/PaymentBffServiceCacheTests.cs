using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Payments;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using PaymentMethodDto = KLC.Driver.Services.PaymentMethodDto;

namespace KLC.BffServices;

/// <summary>
/// Tests for PaymentBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class PaymentBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly IPaymentProcessingAppService _paymentProcessingAppService;
    private readonly PaymentBffService _service;

    public PaymentBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();
        _paymentProcessingAppService = Substitute.For<IPaymentProcessingAppService>();

        var logger = Substitute.For<ILogger<PaymentBffService>>();

        _service = new PaymentBffService(
            _dbContext, _cache, logger, _paymentProcessingAppService, _driverNotifier);
    }

    [Fact]
    public async Task GetPaymentMethods_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedMethods = new List<PaymentMethodDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Gateway = PaymentGateway.MoMo,
                DisplayName = "MoMo Wallet",
                LastFourDigits = "1234",
                IsDefault = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Gateway = PaymentGateway.ZaloPay,
                DisplayName = "ZaloPay",
                LastFourDigits = "5678",
                IsDefault = false
            }
        };

        var cacheKey = $"user:{userId}:payment-methods";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<PaymentMethodDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedMethods);

        // Act
        var result = await _service.GetPaymentMethodsAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Gateway.ShouldBe(PaymentGateway.MoMo);
        result[0].IsDefault.ShouldBeTrue();
        result[1].Gateway.ShouldBe(PaymentGateway.ZaloPay);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<PaymentMethodDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetPaymentMethods_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var method = new UserPaymentMethod(
                Guid.NewGuid(), userId, PaymentGateway.MoMo, "MoMo Wallet", "token_123", "9999");
            method.SetAsDefault();
            await _dbContext.UserPaymentMethods.AddAsync(method);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:payment-methods";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<PaymentMethodDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<PaymentMethodDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPaymentMethodsAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Gateway.ShouldBe(PaymentGateway.MoMo);
            result[0].DisplayName.ShouldBe("MoMo Wallet");
            result[0].IsDefault.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task AddPaymentMethod_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddPaymentMethodAsync(userId, new AddPaymentMethodRequest
            {
                Gateway = PaymentGateway.ZaloPay,
                DisplayName = "ZaloPay Wallet",
                TokenReference = "zp_token_001",
                LastFourDigits = "4567"
            });

            // Assert
            result.ShouldNotBeNull();
            result.Gateway.ShouldBe(PaymentGateway.ZaloPay);
            result.DisplayName.ShouldBe("ZaloPay Wallet");
        });

        // Verify cache invalidation
        await _cache.Received(1).RemoveAsync($"user:{userId}:payment-methods");
    }

    [Fact]
    public async Task DeletePaymentMethod_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var methodId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var method = new UserPaymentMethod(
                methodId, userId, PaymentGateway.MoMo, "MoMo Wallet", "token_del", "1111");
            await _dbContext.UserPaymentMethods.AddAsync(method);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.DeletePaymentMethodAsync(userId, methodId);
        });

        // Assert
        await _cache.Received(1).RemoveAsync($"user:{userId}:payment-methods");
    }

    [Fact]
    public async Task SetDefaultPaymentMethod_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var methodId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var method = new UserPaymentMethod(
                methodId, userId, PaymentGateway.VnPay, "VnPay", "token_def", "2222");
            await _dbContext.UserPaymentMethods.AddAsync(method);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.SetDefaultPaymentMethodAsync(userId, methodId);
        });

        // Assert
        await _cache.Received(1).RemoveAsync($"user:{userId}:payment-methods");
    }

    [Fact]
    public async Task GetPaymentHistory_Should_Bypass_Cache()
    {
        // Arrange - GetPaymentHistory uses cursor-based pagination, no cache
        var userId = Guid.NewGuid();

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPaymentHistoryAsync(userId, null, 10);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        });

        // Verify cache was NOT called for paginated history
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<PagedResult<PaymentHistoryDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetPaymentMethods_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:payment-methods";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<PaymentMethodDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<PaymentMethodDto>());

        // Act
        await _service.GetPaymentMethodsAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<List<PaymentMethodDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

}
