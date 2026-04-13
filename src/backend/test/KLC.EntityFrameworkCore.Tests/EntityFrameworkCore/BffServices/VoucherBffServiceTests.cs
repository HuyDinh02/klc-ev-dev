using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using KLC.Driver;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Marketing;
using KLC.Payments;
using KLC.TestDoubles;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class VoucherBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly VoucherBffService _service;

    public VoucherBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();

        var logger = Substitute.For<ILogger<VoucherBffService>>();
        var voucherRedemptionAppService = CreateVoucherRedemptionAppService();

        _service = new VoucherBffService(
            _dbContext, _cache, logger, voucherRedemptionAppService, _driverNotifier);
    }

    [Fact]
    public async Task ApplyVoucher_Should_Fail_When_Code_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(Guid.NewGuid(), "NONEXISTENT");

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task ApplyVoucher_Should_Fail_When_Voucher_Expired()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var voucher = new Voucher(
                Guid.NewGuid(), "EXPIRED01", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(-1), 100, description: "Expired voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(userId, "EXPIRED01");

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not valid");
        });
    }

    [Fact]
    public async Task ApplyVoucher_Should_Fail_When_Already_Used()
    {
        var userId = Guid.NewGuid();
        var voucherId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var voucher = new Voucher(
                voucherId, "USED01", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Test voucher");
            await _dbContext.Vouchers.AddAsync(voucher);

            var userVoucher = new UserVoucher(Guid.NewGuid(), userId, voucherId);
            userVoucher.MarkUsed();
            await _dbContext.UserVouchers.AddAsync(userVoucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(userId, "USED01");

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("already used");
        });
    }

    [Fact]
    public async Task ApplyVoucher_Should_Succeed_With_FixedAmount()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);

            var voucher = new Voucher(
                Guid.NewGuid(), "FIXED50K", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Fixed 50K voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(identityUserId, "FIXED50K");

            result.Success.ShouldBeTrue();
            result.CreditAmount.ShouldBe(50_000m);
            result.NewBalance.ShouldNotBeNull();
        });

        // Verify SignalR notification was sent
        await _driverNotifier.Received(1).NotifyWalletBalanceChangedAsync(
            identityUserId, Arg.Any<WalletBalanceChangedMessage>());
    }

    [Fact]
    public async Task ApplyVoucher_Should_Succeed_With_Percentage()
    {
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), identityUserId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);

            var voucher = new Voucher(
                Guid.NewGuid(), "PCT20", VoucherType.Percentage, 20m,
                DateTime.UtcNow.AddDays(30), 100, maxDiscountAmount: 200_000m, description: "20% voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(identityUserId, "PCT20");

            result.Success.ShouldBeTrue();
            result.CreditAmount.ShouldNotBeNull();
            result.CreditAmount!.Value.ShouldBeGreaterThan(0);
        });
    }

    [Fact]
    public async Task ValidateVoucher_Should_Return_Valid_For_Active_Voucher()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var voucher = new Voucher(
                Guid.NewGuid(), "VALID01", VoucherType.FixedAmount, 30_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Valid voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ValidateVoucherAsync("VALID01");

            result.IsValid.ShouldBeTrue();
            result.Voucher.ShouldNotBeNull();
            result.Voucher!.Code.ShouldBe("VALID01");
        });
    }

    [Fact]
    public async Task ValidateVoucher_Should_Return_Invalid_For_Unknown_Code()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ValidateVoucherAsync("UNKNOWN");

            result.IsValid.ShouldBeFalse();
        });
    }

    #region Helpers

    private VoucherRedemptionAppService CreateVoucherRedemptionAppService()
    {
        var voucherRepository = GetRequiredService<IRepository<Voucher, Guid>>();
        var userVoucherRepository = GetRequiredService<IRepository<UserVoucher, Guid>>();
        var walletTransactionRepository = GetRequiredService<IRepository<WalletTransaction, Guid>>();
        var appUserRepository = GetRequiredService<IRepository<AppUser, Guid>>();
        var walletDomainService = CreateWalletDomainService();

        var service = new VoucherRedemptionAppService(
            voucherRepository,
            userVoucherRepository,
            walletTransactionRepository,
            appUserRepository,
            walletDomainService,
            Substitute.For<ILogger<VoucherRedemptionAppService>>());

        SetupAbpServiceProvider(service);
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

    private static void SetupAbpServiceProvider(VoucherRedemptionAppService service)
    {
        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider
            .LazyGetRequiredService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetService<Microsoft.Extensions.Logging.ILoggerFactory>()
            .Returns(Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>());

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
    }

    #endregion
}
