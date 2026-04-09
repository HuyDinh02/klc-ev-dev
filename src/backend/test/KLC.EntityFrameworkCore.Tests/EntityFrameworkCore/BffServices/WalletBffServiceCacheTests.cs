using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using KLC.Driver;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using KLC.Payments;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for WalletBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class WalletBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly WalletBffService _service;

    public WalletBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();

        var logger = Substitute.For<ILogger<WalletBffService>>();
        var walletDomainService = CreateWalletDomainService();
        var paymentGateways = CreateMockPaymentGateways();

        var callbackValidator = Substitute.For<IPaymentCallbackValidator>();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var redisDb = Substitute.For<IDatabase>();
        redisDb.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>(), Arg.Any<CommandFlags>()).Returns(true);
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(redisDb);
        _service = new WalletBffService(
            _dbContext, _cache, logger, walletDomainService, paymentGateways, callbackValidator, Substitute.For<IPushNotificationService>(), _driverNotifier, configuration, redis);
    }

    [Fact]
    public async Task GetBalance_Should_Return_Cached_Balance_On_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedBalance = new WalletBalanceDto
        {
            Balance = 500_000m,
            Currency = "VND",
            LastTransactionType = WalletTransactionType.TopUp,
            LastTransactionAmount = 100_000m,
            LastTransactionAt = DateTime.UtcNow.AddHours(-1)
        };

        var cacheKey = $"user:{userId}:wallet-balance";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<WalletBalanceDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedBalance);

        // Act
        var result = await _service.GetBalanceAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Balance.ShouldBe(500_000m);
        result.Currency.ShouldBe("VND");
        result.LastTransactionType.ShouldBe(WalletTransactionType.TopUp);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<WalletBalanceDto>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetBalance_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Cache Miss User", "0901234567");
            user.AddToWallet(750_000m);
            await _dbContext.AppUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{identityUserId}:wallet-balance";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<WalletBalanceDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<WalletBalanceDto>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetBalanceAsync(identityUserId);

            // Assert
            result.ShouldNotBeNull();
            result.Balance.ShouldBe(750_000m);
            result.Currency.ShouldBe("VND");
        });
    }

    [Fact]
    public async Task TopUp_Should_Not_Invalidate_Cache_Before_Callback()
    {
        // Arrange - TopUp creates a pending transaction; cache is only invalidated on callback
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "TopUp User", "0902345678");
            await _dbContext.AppUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(identityUserId, new TopUpRequest
            {
                Amount = 100_000,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeTrue();
        });

        // Assert - cache NOT invalidated yet (only on callback completion)
        await _cache.DidNotReceive().RemoveAsync($"user:{identityUserId}:wallet-balance");
    }

    [Fact]
    public async Task ProcessTopUpCallback_Should_Invalidate_Balance_And_Profile_Cache()
    {
        // Arrange
        var identityUserId = Guid.NewGuid();
        var referenceCode = "";

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Callback User", "0903456789");
            await _dbContext.AppUsers.AddAsync(user);

            var transaction = new WalletTransaction(
                Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                50_000m, 0, PaymentGateway.ZaloPay);
            await _dbContext.WalletTransactions.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            referenceCode = transaction.ReferenceCode;
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessTopUpCallbackAsync(new TopUpCallbackRequest
            {
                ReferenceCode = referenceCode,
                GatewayTransactionId = "GW_TX_CACHE_001",
                Status = TransactionStatus.Completed
            });

            result.Success.ShouldBeTrue();
            result.NewBalance.ShouldNotBeNull();
        });

        // Assert - both balance and profile cache invalidated
        await _cache.Received(1).RemoveAsync($"user:{identityUserId}:wallet-balance");
        await _cache.Received(1).RemoveAsync($"user:{identityUserId}:profile");
    }

    [Fact]
    public async Task GetTransactions_Should_Use_Cursor_Based_Pagination()
    {
        // Arrange
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Paged User", "0904567890");
            await _dbContext.AppUsers.AddAsync(user);

            // Create 5 transactions
            for (int i = 0; i < 5; i++)
            {
                var tx = new WalletTransaction(
                    Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                    100_000m * (i + 1), i * 100_000m, PaymentGateway.ZaloPay);
                tx.MarkCompleted($"GW_TX_{i}");
                SetCreationTime(tx, DateTime.UtcNow.AddMinutes(-i));
                await _dbContext.WalletTransactions.AddAsync(tx);
            }
            await _dbContext.SaveChangesAsync();
        });

        // Act - first page
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetTransactionsAsync(identityUserId, null, 3, null);

            // Assert
            result.ShouldNotBeNull();
            result.Data.Count.ShouldBe(3);
            result.HasMore.ShouldBeTrue();
            result.NextCursor.ShouldNotBeNull();
            result.PageSize.ShouldBe(3);
        });

        // GetTransactions does NOT use cache
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<PagedResult<WalletTransactionDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetBalance_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:wallet-balance";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<WalletBalanceDto>>>(), Arg.Any<TimeSpan?>())
            .Returns(new WalletBalanceDto { Balance = 0, Currency = "VND" });

        // Act
        await _service.GetBalanceAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<WalletBalanceDto>>>(),
            Arg.Any<TimeSpan?>());
    }

    #region Helpers

    private static WalletDomainService CreateWalletDomainService()
    {
        var service = new WalletDomainService();

        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider
            .LazyGetRequiredService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);

        var type = service.GetType();
        while (type != null)
        {
            var prop = type.GetProperty("LazyServiceProvider",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop != null)
            {
                prop.SetValue(service, lazyServiceProvider);
                break;
            }
            type = type.BaseType;
        }

        return service;
    }

    private static IEnumerable<IPaymentGatewayService> CreateMockPaymentGateways()
    {
        var zaloPayGateway = Substitute.For<IPaymentGatewayService>();
        zaloPayGateway.Gateway.Returns(PaymentGateway.ZaloPay);
        zaloPayGateway.CreateTopUpAsync(Arg.Any<CreateTopUpRequest>())
            .Returns(PaymentGatewayResult.Ok("https://zalopay.test/pay/123"));

        var momoGateway = Substitute.For<IPaymentGatewayService>();
        momoGateway.Gateway.Returns(PaymentGateway.MoMo);
        momoGateway.CreateTopUpAsync(Arg.Any<CreateTopUpRequest>())
            .Returns(PaymentGatewayResult.Ok("https://momo.test/pay/456"));

        return new[] { zaloPayGateway, momoGateway };
    }

    private static void SetCreationTime(object entity, DateTime time)
    {
        var prop = entity.GetType().GetProperty("CreationTime",
            BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(entity, time);
    }

    #endregion
}
