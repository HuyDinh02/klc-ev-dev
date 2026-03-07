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
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class PaymentBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly PaymentBffService _service;

    public PaymentBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();

        var logger = Substitute.For<ILogger<PaymentBffService>>();
        var walletDomainService = CreateWalletDomainService();
        var paymentGateways = CreateMockPaymentGateways();

        _service = new PaymentBffService(
            _dbContext, _cache, logger, paymentGateways, walletDomainService, _driverNotifier);
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Session_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
            {
                SessionId = Guid.NewGuid(),
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("Session not found");
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Session_Not_Completed()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(
                stationId, "TST-001", "Test Station", "123 Test St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not completed");
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Succeed_Without_Voucher()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(
                stationId, "TST-002", "Test Station 2", "456 Test St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            // Transition to completed: Pending -> Starting -> InProgress -> Stopping -> Completed
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000); // 25 kWh → TotalCost = 25 * 3500 = 87,500
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay
            });

            result.Success.ShouldBeTrue();
            result.PaymentId.ShouldNotBeNull();
            result.VoucherDiscount.ShouldBeNull();
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Apply_Voucher_Discount()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Test User", "0901234567");
            await _dbContext.AppUsers.AddAsync(user);

            var station = new ChargingStation(
                stationId, "TST-003", "Test Station 3", "789 Test St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000); // 25 kWh → TotalCost = 87,500
            await _dbContext.ChargingSessions.AddAsync(session);

            var voucher = new Voucher(
                Guid.NewGuid(), "PAY50K", VoucherType.FixedAmount, 50_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Payment discount");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay,
                VoucherCode = "PAY50K"
            });

            result.Success.ShouldBeTrue();
            result.VoucherDiscount.ShouldNotBeNull();
            result.VoucherDiscount!.Value.ShouldBeGreaterThan(0);
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_With_Invalid_Voucher()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(
                stationId, "TST-004", "Test Station 4", "101 Test St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000); // 25 kWh
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay,
                VoucherCode = "NONEXISTENT"
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("not found");
        });
    }

    [Fact]
    public async Task GetPaymentHistory_Should_Return_Empty_For_New_User()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPaymentHistoryAsync(Guid.NewGuid(), null, 20);

            result.Data.ShouldBeEmpty();
            result.HasMore.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetPaymentDetail_Should_Return_Null_For_Unknown_Payment()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPaymentDetailAsync(Guid.NewGuid(), Guid.NewGuid());
            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetPaymentMethods_Should_Return_Empty_For_New_User()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPaymentMethodsAsync(Guid.NewGuid());
            result.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Apply_FreeCharging_Voucher()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Free Charge User", "0904444444");
            await _dbContext.AppUsers.AddAsync(user);

            var station = new ChargingStation(
                stationId, "TST-FREE", "Free Station", "Free St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000); // 25 kWh → TotalCost = 87,500
            await _dbContext.ChargingSessions.AddAsync(session);

            var voucher = new Voucher(
                Guid.NewGuid(), "FREECHARGE", VoucherType.FreeCharging, 0m,
                DateTime.UtcNow.AddDays(30), 100, description: "Free charging voucher");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay,
                VoucherCode = "FREECHARGE"
            });

            result.Success.ShouldBeTrue();
            result.VoucherDiscount.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Apply_Percentage_Voucher_With_MaxDiscount()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Pct User", "0905555555");
            await _dbContext.AppUsers.AddAsync(user);

            var station = new ChargingStation(
                stationId, "TST-PCT", "Pct Station", "Pct St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000); // 25 kWh → TotalCost = 87,500
            await _dbContext.ChargingSessions.AddAsync(session);

            // 50% discount but max 20,000 VND → discount should be capped at 20,000
            var voucher = new Voucher(
                Guid.NewGuid(), "PCT50CAP", VoucherType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), 100,
                maxDiscountAmount: 20_000m, description: "50% off capped at 20K");
            await _dbContext.Vouchers.AddAsync(voucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay,
                VoucherCode = "PCT50CAP"
            });

            result.Success.ShouldBeTrue();
            result.VoucherDiscount.ShouldNotBeNull();
            // 50% of 87,500 = 43,750 but capped at 20,000
            result.VoucherDiscount!.Value.ShouldBeLessThanOrEqualTo(20_000m);
        });
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Voucher_Already_Used_By_User()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var voucherId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Used Voucher User", "0906666666");
            await _dbContext.AppUsers.AddAsync(user);

            var station = new ChargingStation(
                stationId, "TST-USED", "Used Station", "Used St",
                21.0278, 105.8342, null, null);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(
                sessionId, userId, stationId, 1, ratePerKwh: 3500);
            session.MarkStarting();
            session.RecordStart(1001, 0);
            session.MarkStopping();
            session.RecordStop(25000);
            await _dbContext.ChargingSessions.AddAsync(session);

            var voucher = new Voucher(
                voucherId, "ALREADYUSED", VoucherType.FixedAmount, 10_000m,
                DateTime.UtcNow.AddDays(30), 100, description: "Already used");
            await _dbContext.Vouchers.AddAsync(voucher);

            var userVoucher = new UserVoucher(Guid.NewGuid(), userId, voucherId);
            userVoucher.MarkUsed();
            await _dbContext.UserVouchers.AddAsync(userVoucher);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.ProcessPaymentAsync(userId, new ProcessPaymentRequest
            {
                SessionId = sessionId,
                Gateway = PaymentGateway.ZaloPay,
                VoucherCode = "ALREADYUSED"
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldContain("already");
        });
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
            .Returns(PaymentGatewayResult.Ok("https://zalopay.test/pay/123", "GW_TX_001"));

        return new[] { zaloPayGateway };
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
