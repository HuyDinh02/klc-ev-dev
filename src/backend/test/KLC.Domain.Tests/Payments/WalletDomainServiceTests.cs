using System;
using System.Reflection;
using KLC.Enums;
using KLC.Users;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Xunit;

namespace KLC.Payments;

public class WalletDomainServiceTests
{
    private readonly WalletDomainService _service;

    public WalletDomainServiceTests()
    {
        _service = new WalletDomainService();

        // DomainService uses IAbpLazyServiceProvider to resolve IGuidGenerator.
        // Set up a mock lazy service provider that returns SimpleGuidGenerator.
        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        lazyServiceProvider
            .LazyGetRequiredService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);
        lazyServiceProvider
            .LazyGetService<IGuidGenerator>()
            .Returns(SimpleGuidGenerator.Instance);

        // Find and set the LazyServiceProvider property on the service base type hierarchy
        var prop = _service.GetType().BaseType?
            .GetProperty("LazyServiceProvider",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        // Walk up the type hierarchy if not found directly
        var type = _service.GetType();
        while (prop == null && type != null)
        {
            prop = type.GetProperty("LazyServiceProvider",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            type = type.BaseType;
        }

        prop?.SetValue(_service, lazyServiceProvider);
    }

    [Fact]
    public void TopUp_Should_Increase_Balance()
    {
        var user = CreateUser();
        var initialBalance = user.WalletBalance;

        var (newBalance, transaction) = _service.TopUp(user, 100000m, PaymentGateway.ZaloPay);

        newBalance.ShouldBe(initialBalance + 100000m);
        user.WalletBalance.ShouldBe(newBalance);
    }

    [Fact]
    public void TopUp_Should_Return_Correct_Transaction()
    {
        var user = CreateUser();

        var (newBalance, transaction) = _service.TopUp(user, 100000m, PaymentGateway.MoMo, "GW_123");

        transaction.ShouldNotBeNull();
        transaction.Type.ShouldBe(WalletTransactionType.TopUp);
        transaction.Amount.ShouldBe(100000m);
        transaction.BalanceAfter.ShouldBe(newBalance);
        transaction.PaymentGateway.ShouldBe(PaymentGateway.MoMo);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void TopUp_Should_Record_LastTopUpAt()
    {
        var user = CreateUser();
        var before = DateTime.UtcNow;

        _service.TopUp(user, 50000m, PaymentGateway.ZaloPay);

        user.LastTopUpAt.ShouldNotBeNull();
        user.LastTopUpAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void TopUp_Should_Throw_When_Amount_Is_Zero()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.TopUp(user, 0, PaymentGateway.ZaloPay));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void TopUp_Should_Throw_When_Amount_Is_Negative()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.TopUp(user, -10000m, PaymentGateway.ZaloPay));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void DeductForSession_Should_Decrease_Balance()
    {
        var user = CreateUser(initialBalance: 200000m);
        var sessionId = Guid.NewGuid();

        var (newBalance, transaction) = _service.DeductForSession(user, 75000m, sessionId);

        newBalance.ShouldBe(125000m);
        user.WalletBalance.ShouldBe(125000m);
    }

    [Fact]
    public void DeductForSession_Should_Return_Correct_Transaction()
    {
        var user = CreateUser(initialBalance: 200000m);
        var sessionId = Guid.NewGuid();

        var (newBalance, transaction) = _service.DeductForSession(user, 75000m, sessionId);

        transaction.ShouldNotBeNull();
        transaction.Type.ShouldBe(WalletTransactionType.SessionPayment);
        transaction.Amount.ShouldBe(-75000m);
        transaction.BalanceAfter.ShouldBe(125000m);
        transaction.RelatedSessionId.ShouldBe(sessionId);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void DeductForSession_Should_Throw_When_Insufficient_Balance()
    {
        var user = CreateUser(initialBalance: 50000m);
        var sessionId = Guid.NewGuid();

        var ex = Should.Throw<BusinessException>(() =>
            _service.DeductForSession(user, 100000m, sessionId));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalance);
    }

    [Fact]
    public void DeductForSession_Should_Throw_When_Amount_Is_Zero()
    {
        var user = CreateUser(initialBalance: 100000m);

        var ex = Should.Throw<BusinessException>(() =>
            _service.DeductForSession(user, 0, Guid.NewGuid()));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void Refund_Should_Increase_Balance()
    {
        var user = CreateUser(initialBalance: 50000m);
        var sessionId = Guid.NewGuid();

        var (newBalance, transaction) = _service.Refund(user, 25000m, sessionId, "Overcharge refund");

        newBalance.ShouldBe(75000m);
        user.WalletBalance.ShouldBe(75000m);
    }

    [Fact]
    public void Refund_Should_Return_Correct_Transaction()
    {
        var user = CreateUser(initialBalance: 50000m);
        var sessionId = Guid.NewGuid();

        var (newBalance, transaction) = _service.Refund(user, 25000m, sessionId, "Test refund");

        transaction.ShouldNotBeNull();
        transaction.Type.ShouldBe(WalletTransactionType.Refund);
        transaction.Amount.ShouldBe(25000m);
        transaction.BalanceAfter.ShouldBe(75000m);
        transaction.RelatedSessionId.ShouldBe(sessionId);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void Refund_Should_Throw_When_Amount_Is_Zero()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.Refund(user, 0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void Adjust_Positive_Should_Increase_Balance()
    {
        var user = CreateUser(initialBalance: 100000m);

        var (newBalance, transaction) = _service.Adjust(user, 50000m, "Bonus credit");

        newBalance.ShouldBe(150000m);
        user.WalletBalance.ShouldBe(150000m);
        transaction.Type.ShouldBe(WalletTransactionType.Adjustment);
        transaction.Amount.ShouldBe(50000m);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void Adjust_Negative_Should_Decrease_Balance()
    {
        var user = CreateUser(initialBalance: 100000m);

        var (newBalance, transaction) = _service.Adjust(user, -30000m, "Penalty");

        newBalance.ShouldBe(70000m);
        user.WalletBalance.ShouldBe(70000m);
        transaction.Type.ShouldBe(WalletTransactionType.Adjustment);
        transaction.Amount.ShouldBe(-30000m);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void Adjust_Should_Throw_When_Amount_Is_Zero()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.Adjust(user, 0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void Adjust_Negative_Should_Throw_When_Insufficient_Balance()
    {
        var user = CreateUser(initialBalance: 10000m);

        var ex = Should.Throw<BusinessException>(() =>
            _service.Adjust(user, -50000m));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InsufficientBalance);
    }

    [Fact]
    public void ApplyVoucher_Should_Increase_Balance()
    {
        var user = CreateUser(initialBalance: 50000m);

        var (newBalance, transaction) = _service.ApplyVoucher(user, 20000m, "SUMMER2026 voucher");

        newBalance.ShouldBe(70000m);
        user.WalletBalance.ShouldBe(70000m);
    }

    [Fact]
    public void ApplyVoucher_Should_Return_Correct_Transaction()
    {
        var user = CreateUser(initialBalance: 50000m);

        var (newBalance, transaction) = _service.ApplyVoucher(user, 20000m, "Voucher applied");

        transaction.ShouldNotBeNull();
        transaction.Type.ShouldBe(WalletTransactionType.VoucherCredit);
        transaction.Amount.ShouldBe(20000m);
        transaction.BalanceAfter.ShouldBe(70000m);
        transaction.PaymentGateway.ShouldBe(PaymentGateway.Voucher);
        transaction.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void ApplyVoucher_Should_Throw_When_Amount_Is_Zero()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.ApplyVoucher(user, 0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    [Fact]
    public void ApplyVoucher_Should_Throw_When_Amount_Is_Negative()
    {
        var user = CreateUser();

        var ex = Should.Throw<BusinessException>(() =>
            _service.ApplyVoucher(user, -5000m));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.InvalidAmount);
    }

    private static AppUser CreateUser(decimal initialBalance = 0)
    {
        var user = new AppUser(Guid.NewGuid(), Guid.NewGuid(), "Test User", "0901234567");
        if (initialBalance > 0)
        {
            user.AddToWallet(initialBalance);
        }
        return user;
    }
}
