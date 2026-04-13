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
using KLC.TestDoubles;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for VnPay IPN processing — verifies correct RspCode for each scenario.
/// Bug fix: validation must run BEFORE idempotency lock so invalid signature/amount
/// always returns the correct error code regardless of lock state.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class VnPayIpnTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly IPaymentCallbackValidator _callbackValidator;
    private readonly WalletBffService _bffService;
    private readonly WalletAppService _walletAppService;

    public VnPayIpnTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _callbackValidator = Substitute.For<IPaymentCallbackValidator>();

        var walletDomainService = CreateWalletDomainService();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        // Create the WalletAppService (business logic)
        _walletAppService = CreateWalletAppService(
            walletDomainService, new List<IPaymentGatewayService>(), _callbackValidator, configuration);

        // Create the thin BFF service that delegates to WalletAppService
        _bffService = new WalletBffService(
            _dbContext,
            new PassthroughCacheService(),
            Substitute.For<ILogger<WalletBffService>>(),
            _walletAppService,
            Substitute.For<IPushNotificationService>(),
            Substitute.For<IDriverHubNotifier>());
    }

    [Fact]
    public async Task IPN_Should_Return_OrderNotFound_When_TxnRef_Missing()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string>());
            result.RspCode.ShouldBe("01");
        });
    }

    [Fact]
    public async Task IPN_Should_Return_OrderNotFound_When_Transaction_Not_Exists()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string>
            {
                { "vnp_TxnRef", "NONEXISTENT" }
            });
            result.RspCode.ShouldBe("01");
        });
    }

    [Fact]
    public async Task IPN_Should_Return_InvalidAmount_When_Amount_Mismatch()
    {
        var txnRef = $"WTX{Guid.NewGuid():N}".Substring(0, 20);
        await SeedTransaction(txnRef, 75_000m);

        _callbackValidator.ValidateVnPayIpnAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<decimal?>())
            .Returns(VnPayCallbackValidation.Invalid("Amount mismatch"));

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string>
            {
                { "vnp_TxnRef", txnRef }
            });
            result.RspCode.ShouldBe("04");
        });
    }

    [Fact]
    public async Task IPN_Should_Return_InvalidSignature_When_Checksum_Wrong()
    {
        var txnRef = $"WTX{Guid.NewGuid():N}".Substring(0, 20);
        await SeedTransaction(txnRef, 75_000m);

        _callbackValidator.ValidateVnPayIpnAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<decimal?>())
            .Returns(VnPayCallbackValidation.Invalid("Invalid signature"));

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string>
            {
                { "vnp_TxnRef", txnRef }
            });
            result.RspCode.ShouldBe("97");
        });
    }

    [Fact]
    public async Task IPN_Should_Return_Correct_Codes_For_Sequential_Tests_On_Same_TxnRef()
    {
        // VnPay sandbox scenario: test invalid checksum then invalid amount on same TxnRef
        var txnRef = $"WTX{Guid.NewGuid():N}".Substring(0, 20);
        await SeedTransaction(txnRef, 75_000m);

        // Test 1: Invalid checksum
        _callbackValidator.ValidateVnPayIpnAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<decimal?>())
            .Returns(VnPayCallbackValidation.Invalid("Invalid signature"));

        await WithUnitOfWorkAsync(async () =>
        {
            var r1 = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string> { { "vnp_TxnRef", txnRef } });
            r1.RspCode.ShouldBe("97");
        });

        // Test 2: Invalid amount (same TxnRef -- was returning 02 before fix)
        _callbackValidator.ValidateVnPayIpnAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<decimal?>())
            .Returns(VnPayCallbackValidation.Invalid("Amount mismatch"));

        await WithUnitOfWorkAsync(async () =>
        {
            var r2 = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string> { { "vnp_TxnRef", txnRef } });
            r2.RspCode.ShouldBe("04");
        });
    }

    [Fact]
    public async Task IPN_Should_Return_AlreadyConfirmed_When_Transaction_Completed()
    {
        var txnRef = $"WTX{Guid.NewGuid():N}".Substring(0, 20);
        await SeedTransaction(txnRef, 50_000m, completed: true);

        _callbackValidator.ValidateVnPayIpnAsync(Arg.Any<Dictionary<string, string>>(), Arg.Any<decimal?>())
            .Returns(VnPayCallbackValidation.Valid(txnRef, "00", 50_000m, "GW123"));

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _bffService.ProcessVnPayIpnAsync(new Dictionary<string, string> { { "vnp_TxnRef", txnRef } });
            result.RspCode.ShouldBe("02");
        });
    }

    private async Task SeedTransaction(string txnRef, decimal amount, bool completed = false)
    {
        var userId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "IPN Test", $"09{Random.Shared.Next(10000000, 99999999)}");
            user.AddToWallet(200_000m);
            await _dbContext.AppUsers.AddAsync(user);

            var txn = new WalletTransaction(Guid.NewGuid(), userId, WalletTransactionType.TopUp, amount, 200_000m, PaymentGateway.VnPay);
            // Set ReferenceCode via reflection (private setter)
            typeof(WalletTransaction).GetProperty("ReferenceCode")!
                .SetValue(txn, txnRef);
            if (completed) txn.MarkCompleted("GW_DONE");
            await _dbContext.WalletTransactions.AddAsync(txn);
            await _dbContext.SaveChangesAsync();
        });
    }

    #region Helpers

    private WalletAppService CreateWalletAppService(
        WalletDomainService walletDomainService,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        IPaymentCallbackValidator callbackValidator,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var walletTransactionRepo = GetRequiredService<IRepository<WalletTransaction, Guid>>();
        var appUserRepo = GetRequiredService<IRepository<AppUser, Guid>>();
        var notificationRepo = GetRequiredService<IRepository<Notification, Guid>>();

        var distributedLock = Substitute.For<IAbpDistributedLock>();
        distributedLock.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Substitute.For<IAbpDistributedLockHandle>());

        var service = new WalletAppService(
            walletTransactionRepo,
            appUserRepo,
            notificationRepo,
            walletDomainService,
            paymentGateways,
            callbackValidator,
            configuration,
            Microsoft.Extensions.Options.Options.Create(new KLC.Configuration.WalletSettings()),
            distributedLock,
            Substitute.For<ILogger<WalletAppService>>());

        // Inject ABP lazy service provider
        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider
            .LazyGetRequiredService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetRequiredService<Volo.Abp.Linq.IAsyncQueryableExecuter>()
            .Returns(GetRequiredService<Volo.Abp.Linq.IAsyncQueryableExecuter>());
        lazyServiceProvider
            .LazyGetService<Volo.Abp.Linq.IAsyncQueryableExecuter>()
            .Returns(GetRequiredService<Volo.Abp.Linq.IAsyncQueryableExecuter>());

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

    #endregion
}
