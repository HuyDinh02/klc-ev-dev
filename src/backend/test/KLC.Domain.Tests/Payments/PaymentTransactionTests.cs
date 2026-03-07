using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Payments;

public class PaymentTransactionTests
{
    [Fact]
    public void Constructor_Should_Initialize_As_Pending()
    {
        var payment = CreatePayment();

        payment.Status.ShouldBe(PaymentStatus.Pending);
        payment.Amount.ShouldBe(50_000m);
        payment.Gateway.ShouldBe(PaymentGateway.ZaloPay);
        payment.ReferenceCode.ShouldNotBeNullOrEmpty();
        payment.ReferenceCode!.ShouldStartWith("KLC");
        payment.CompletedAt.ShouldBeNull();
        payment.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void MarkProcessing_Should_Set_Processing_Status()
    {
        var payment = CreatePayment();

        payment.MarkProcessing("GW_TX_001");

        payment.Status.ShouldBe(PaymentStatus.Processing);
        payment.GatewayTransactionId.ShouldBe("GW_TX_001");
    }

    [Fact]
    public void MarkProcessing_Without_TransactionId_Should_Set_Processing()
    {
        var payment = CreatePayment();

        payment.MarkProcessing();

        payment.Status.ShouldBe(PaymentStatus.Processing);
        payment.GatewayTransactionId.ShouldBeNull();
    }

    [Fact]
    public void MarkCompleted_Should_Set_Completed_Status_And_Timestamp()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        payment.MarkCompleted("GW_TX_FINAL");

        payment.Status.ShouldBe(PaymentStatus.Completed);
        payment.GatewayTransactionId.ShouldBe("GW_TX_FINAL");
        payment.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkFailed_Should_Set_Failed_Status_And_ErrorMessage()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        payment.MarkFailed("Insufficient funds");

        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.ErrorMessage.ShouldBe("Insufficient funds");
    }

    [Fact]
    public void MarkRefunded_Should_Succeed_When_Completed()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();
        payment.MarkCompleted("GW_TX_001");

        payment.MarkRefunded();

        payment.Status.ShouldBe(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_Should_Throw_When_Not_Completed()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        var ex = Should.Throw<BusinessException>(() => payment.MarkRefunded());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Payment.InvalidRefund);
    }

    [Fact]
    public void MarkRefunded_Should_Throw_When_Pending()
    {
        var payment = CreatePayment();

        Should.Throw<BusinessException>(() => payment.MarkRefunded());
    }

    [Fact]
    public void MarkCancelled_Should_Succeed_When_Pending()
    {
        var payment = CreatePayment();

        payment.MarkCancelled();

        payment.Status.ShouldBe(PaymentStatus.Cancelled);
    }

    [Fact]
    public void MarkCancelled_Should_Succeed_When_Processing()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        payment.MarkCancelled();

        payment.Status.ShouldBe(PaymentStatus.Cancelled);
    }

    [Fact]
    public void MarkCancelled_Should_Throw_When_Completed()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();
        payment.MarkCompleted("GW_TX_001");

        var ex = Should.Throw<BusinessException>(() => payment.MarkCancelled());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Payment.CannotCancel);
    }

    [Fact]
    public void UpdateAmount_Should_Succeed_When_Pending()
    {
        var payment = CreatePayment();

        payment.UpdateAmount(100_000m);

        payment.Amount.ShouldBe(100_000m);
    }

    [Fact]
    public void UpdateAmount_Should_Throw_When_Not_Pending()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        Should.Throw<BusinessException>(() => payment.UpdateAmount(100_000m));
    }

    [Fact]
    public void Full_Lifecycle_Pending_To_Completed()
    {
        var payment = CreatePayment();

        payment.Status.ShouldBe(PaymentStatus.Pending);

        payment.MarkProcessing("GW_INIT");
        payment.Status.ShouldBe(PaymentStatus.Processing);

        payment.MarkCompleted("GW_FINAL");
        payment.Status.ShouldBe(PaymentStatus.Completed);
        payment.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Full_Lifecycle_Pending_To_Failed()
    {
        var payment = CreatePayment();

        payment.MarkProcessing();
        payment.MarkFailed("Gateway timeout");

        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.ErrorMessage.ShouldBe("Gateway timeout");
    }

    private static PaymentTransaction CreatePayment(
        decimal amount = 50_000m,
        PaymentGateway gateway = PaymentGateway.ZaloPay)
    {
        return new PaymentTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            gateway,
            amount);
    }
}
