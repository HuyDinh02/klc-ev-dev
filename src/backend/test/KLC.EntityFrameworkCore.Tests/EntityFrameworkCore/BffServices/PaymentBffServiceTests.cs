using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Payments;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class PaymentBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly IPaymentProcessingAppService _paymentProcessingAppService;
    private readonly PaymentBffService _service;

    public PaymentBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = new PassthroughCacheService();
        _driverNotifier = Substitute.For<IDriverHubNotifier>();
        _paymentProcessingAppService = Substitute.For<IPaymentProcessingAppService>();

        var logger = Substitute.For<ILogger<PaymentBffService>>();
        _service = new PaymentBffService(
            _dbContext, _cache, logger, _paymentProcessingAppService, _driverNotifier);
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Session_Not_Found()
    {
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto { Success = false, Error = "Session not found" });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("Session not found");
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Session_Not_Completed()
    {
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto { Success = false, Error = "Session not completed" });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not completed");
    }

    [Fact]
    public async Task ProcessPayment_Should_Succeed_Without_Voucher()
    {
        var paymentId = Guid.NewGuid();

        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = true,
                PaymentId = paymentId,
                Status = PaymentStatus.Completed,
                VoucherDiscount = null
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay
        });

        result.Success.ShouldBeTrue();
        result.PaymentId.ShouldNotBeNull();
        result.VoucherDiscount.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessPayment_Should_Apply_Voucher_Discount()
    {
        var paymentId = Guid.NewGuid();

        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = true,
                PaymentId = paymentId,
                Status = PaymentStatus.Completed,
                VoucherDiscount = 50_000m
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay,
            VoucherCode = "PAY50K"
        });

        result.Success.ShouldBeTrue();
        result.VoucherDiscount.ShouldNotBeNull();
        result.VoucherDiscount!.Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_With_Invalid_Voucher()
    {
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = false,
                Error = "Voucher not found"
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay,
            VoucherCode = "NONEXISTENT"
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("not found");
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
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = true,
                PaymentId = Guid.NewGuid(),
                Status = PaymentStatus.Completed,
                VoucherDiscount = 87_500m
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay,
            VoucherCode = "FREECHARGE"
        });

        result.Success.ShouldBeTrue();
        result.VoucherDiscount.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProcessPayment_Should_Apply_Percentage_Voucher_With_MaxDiscount()
    {
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = true,
                PaymentId = Guid.NewGuid(),
                Status = PaymentStatus.Completed,
                VoucherDiscount = 20_000m // Capped at max discount
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay,
            VoucherCode = "PCT50CAP"
        });

        result.Success.ShouldBeTrue();
        result.VoucherDiscount.ShouldNotBeNull();
        // 50% of 87,500 = 43,750 but capped at 20,000
        result.VoucherDiscount!.Value.ShouldBeLessThanOrEqualTo(20_000m);
    }

    [Fact]
    public async Task ProcessPayment_Should_Fail_When_Voucher_Already_Used_By_User()
    {
        _paymentProcessingAppService.ProcessSessionPaymentAsync(Arg.Any<ProcessSessionPaymentInput>())
            .Returns(new SessionPaymentResultDto
            {
                Success = false,
                Error = "You have already used this voucher"
            });

        var result = await _service.ProcessPaymentAsync(Guid.NewGuid(), new ProcessPaymentRequest
        {
            SessionId = Guid.NewGuid(),
            Gateway = PaymentGateway.ZaloPay,
            VoucherCode = "ALREADYUSED"
        });

        result.Success.ShouldBeFalse();
        result.Error.ShouldContain("already");
    }

    #region Helpers

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
