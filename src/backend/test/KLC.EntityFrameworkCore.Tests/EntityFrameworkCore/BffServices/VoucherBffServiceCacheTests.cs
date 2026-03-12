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
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for VoucherBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class VoucherBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly VoucherBffService _service;

    public VoucherBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();

        var logger = Substitute.For<ILogger<VoucherBffService>>();
        var walletDomainService = CreateWalletDomainService();

        _service = new VoucherBffService(
            _dbContext, _cache, logger, walletDomainService, _driverNotifier);
    }

    [Fact]
    public async Task GetAvailableVouchers_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedVouchers = new List<VoucherDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "CACHED50K",
                Type = VoucherType.FixedAmount,
                Value = 50_000m,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                Description = "50K discount",
                RemainingQuantity = 10
            }
        };

        var cacheKey = $"user:{userId}:available-vouchers";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VoucherDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedVouchers);

        // Act
        var result = await _service.GetAvailableVouchersAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Code.ShouldBe("CACHED50K");
        result[0].Type.ShouldBe(VoucherType.FixedAmount);
        result[0].Value.ShouldBe(50_000m);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VoucherDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetAvailableVouchers_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var voucher = new Voucher(
                Guid.NewGuid(), "DBVOUCHER", VoucherType.FixedAmount, 30_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "DB voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:available-vouchers";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VoucherDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<VoucherDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetAvailableVouchersAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThanOrEqualTo(1);
            result.ShouldContain(v => v.Code == "DBVOUCHER");
        });
    }

    [Fact]
    public async Task ApplyVoucher_Should_Invalidate_Caches()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Voucher User", "0901111111");
            await _dbContext.AppUsers.AddAsync(user);

            var voucher = new Voucher(
                Guid.NewGuid(), "APPLY50K", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Apply voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(userId, "APPLY50K");

            // Assert
            result.Success.ShouldBeTrue();
            result.CreditAmount.ShouldBe(50_000m);
        });

        // Verify multiple cache keys invalidated
        await _cache.Received(1).RemoveAsync($"user:{userId}:wallet-balance");
        await _cache.Received(1).RemoveAsync($"user:{userId}:available-vouchers");
        await _cache.Received(1).RemoveAsync($"user:{userId}:wallet-summary");
    }

    [Fact]
    public async Task ValidateVoucher_Should_Bypass_Cache()
    {
        // Arrange - ValidateVoucher does NOT use cache
        await WithUnitOfWorkAsync(async () =>
        {
            var voucher = new Voucher(
                Guid.NewGuid(), "VALIDATE01", VoucherType.FixedAmount, 20_000m,
                DateTime.UtcNow.AddDays(30), 50, description: "Validate test");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ValidateVoucherAsync("VALIDATE01");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.Voucher.ShouldNotBeNull();
            result.Voucher!.Code.ShouldBe("VALIDATE01");
        });

        // Verify cache was NOT called for validation
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<VoucherValidationResultDto>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetAvailableVouchers_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:available-vouchers";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<VoucherDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<VoucherDto>());

        // Act
        await _service.GetAvailableVouchersAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<List<VoucherDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task ApplyVoucher_Should_Notify_Via_SignalR()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "SignalR User", "0902222222");
            await _dbContext.AppUsers.AddAsync(user);

            var voucher = new Voucher(
                Guid.NewGuid(), "SIGNAL50K", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "SignalR voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ApplyVoucherAsync(userId, "SIGNAL50K");
            result.Success.ShouldBeTrue();
        });

        // Assert - SignalR notification sent
        await _driverNotifier.Received(1).NotifyWalletBalanceChangedAsync(
            userId, Arg.Any<WalletBalanceChangedMessage>());
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

    #endregion
}
