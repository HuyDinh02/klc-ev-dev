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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class WalletBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly WalletBffService _service;

    public WalletBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
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
            _dbContext, _cache, logger, walletDomainService, paymentGateways, callbackValidator, Substitute.For<IPushNotificationService>(), _driverNotifier, configuration, Microsoft.Extensions.Options.Options.Create(new KLC.Configuration.WalletSettings()), redis);
    }

    [Fact]
    public async Task TopUp_Should_Fail_When_Amount_Is_Zero()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(Guid.NewGuid(), new TopUpRequest
            {
                Amount = 0,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("positive");
        });
    }

    [Fact]
    public async Task TopUp_Should_Fail_When_Amount_Below_Minimum()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(Guid.NewGuid(), new TopUpRequest
            {
                Amount = 10_000, // Below 50,000 VND minimum
                Gateway = PaymentGateway.MoMo
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("MinTopUpAmount");
        });
    }

    [Fact]
    public async Task TopUp_Should_Fail_When_Amount_Above_Maximum()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(Guid.NewGuid(), new TopUpRequest
            {
                Amount = 20_000_000, // Above 10,000,000 VND maximum
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("MaxTopUpAmount");
        });
    }

    [Fact]
    public async Task TopUp_Should_Fail_When_User_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(Guid.NewGuid(), new TopUpRequest
            {
                Amount = 100_000,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("User not found");
        });
    }

    [Fact]
    public async Task TopUp_Should_Succeed_And_Create_Pending_Transaction()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(identityUserId, new TopUpRequest
            {
                Amount = 100_000,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeTrue();
            result.TransactionId.ShouldNotBeNull();
            result.ReferenceCode.ShouldNotBeNullOrEmpty();
            result.RedirectUrl.ShouldNotBeNullOrEmpty();
            result.Status.ShouldBe(TransactionStatus.Pending);
        });
    }

    [Fact]
    public async Task ProcessTopUpCallback_Should_Complete_Transaction_And_Credit_Balance()
    {
        var identityUserId = Guid.NewGuid();
        var referenceCode = "";

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);

            var transaction = new WalletTransaction(
                Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                50_000m, 0, PaymentGateway.ZaloPay);
            await _dbContext.WalletTransactions.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            referenceCode = transaction.ReferenceCode;
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessTopUpCallbackAsync(new TopUpCallbackRequest
            {
                ReferenceCode = referenceCode,
                GatewayTransactionId = "GW_TX_001",
                Status = TransactionStatus.Completed
            });

            result.Success.ShouldBeTrue();
            result.NewBalance.ShouldNotBeNull();
            result.NewBalance!.Value.ShouldBeGreaterThan(0);
        });

        // Verify SignalR notification was sent
        await _driverNotifier.Received(1).NotifyWalletBalanceChangedAsync(
            identityUserId, Arg.Any<WalletBalanceChangedMessage>());
    }

    [Fact]
    public async Task ProcessTopUpCallback_Should_Fail_When_Transaction_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessTopUpCallbackAsync(new TopUpCallbackRequest
            {
                ReferenceCode = "NONEXISTENT",
                Status = TransactionStatus.Completed
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("Transaction not found");
        });
    }

    [Fact]
    public async Task ProcessTopUpCallback_Should_Mark_Failed_Transaction()
    {
        var identityUserId = Guid.NewGuid();
        var referenceCode = "";

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);

            var transaction = new WalletTransaction(
                Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                50_000m, 0, PaymentGateway.MoMo);
            await _dbContext.WalletTransactions.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            referenceCode = transaction.ReferenceCode;
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessTopUpCallbackAsync(new TopUpCallbackRequest
            {
                ReferenceCode = referenceCode,
                Status = TransactionStatus.Failed
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("failure");
        });
    }

    [Fact]
    public async Task GetBalance_Should_Return_Zero_For_Unknown_User()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetBalanceAsync(Guid.NewGuid());

            result.ShouldNotBeNull();
            result.Balance.ShouldBe(0);
            result.Currency.ShouldBe("VND");
        });
    }

    [Fact]
    public async Task GetBalance_Should_Return_User_Balance()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Wallet User", "0909876543");
            user.AddToWallet(500_000m);
            await _dbContext.AppUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetBalanceAsync(identityUserId);

            result.Balance.ShouldBe(500_000m);
            result.Currency.ShouldBe("VND");
        });
    }

    [Fact]
    public async Task GetTransactions_Should_Return_Empty_For_New_User()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetTransactionsAsync(Guid.NewGuid(), null, 20, null);

            result.Data.ShouldBeEmpty();
            result.HasMore.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetTopUpStatus_Should_Return_Null_For_Unknown_Transaction()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetTopUpStatusAsync(Guid.NewGuid(), Guid.NewGuid());
            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task TopUp_Should_Fail_When_Monthly_Limit_Exceeded()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Limit User", "0901111111");
            await _dbContext.AppUsers.AddAsync(user);

            // Create completed top-up transactions totaling 99,500,000 VND this month
            for (int i = 0; i < 10; i++)
            {
                var tx = new WalletTransaction(
                    Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                    9_950_000m, 0, PaymentGateway.ZaloPay);
                tx.MarkCompleted($"GW_{i}");
                SetCreationTime(tx, DateTime.UtcNow);
                await _dbContext.WalletTransactions.AddAsync(tx);
            }
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            // Attempt to top up 1,000,000 more → total 100,500,000 > 100,000,000 limit
            var result = await _service.TopUpAsync(identityUserId, new TopUpRequest
            {
                Amount = 1_000_000,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("SBV");
        });
    }

    [Fact]
    public async Task TopUp_Should_Succeed_When_Under_Monthly_Limit()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Under Limit User", "0902222222");
            await _dbContext.AppUsers.AddAsync(user);

            // Create completed top-up totaling 5,000,000 VND this month
            var tx = new WalletTransaction(
                Guid.NewGuid(), identityUserId, WalletTransactionType.TopUp,
                5_000_000m, 0, PaymentGateway.ZaloPay);
            tx.MarkCompleted("GW_UNDER");
            SetCreationTime(tx, DateTime.UtcNow);
            await _dbContext.WalletTransactions.AddAsync(tx);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.TopUpAsync(identityUserId, new TopUpRequest
            {
                Amount = 5_000_000,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeTrue();
            result.TransactionId.ShouldNotBeNull();
        });
    }

    // Note: GetTransactionSummaryAsync uses Math.Abs() which SQLite cannot translate.
    // This path is tested in production against PostgreSQL which supports ABS().

    #region Helpers

    private static WalletDomainService CreateWalletDomainService()
    {
        var service = new WalletDomainService();

        // Inject mock IAbpLazyServiceProvider for GUID generation
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

    private class PassthroughCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) => Task.CompletedTask;
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
            => await factory();
    }

    #endregion
}
