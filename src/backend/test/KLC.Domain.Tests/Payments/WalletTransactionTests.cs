using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Payments;

public class WalletTransactionTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Status_Pending()
    {
        var userId = Guid.NewGuid();

        var tx = CreateTransaction(userId: userId);

        tx.UserId.ShouldBe(userId);
        tx.Type.ShouldBe(WalletTransactionType.TopUp);
        tx.Amount.ShouldBe(100000m);
        tx.BalanceAfter.ShouldBe(100000m);
        tx.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public void Constructor_Should_Generate_ReferenceCode()
    {
        var tx = CreateTransaction();

        tx.ReferenceCode.ShouldNotBeNullOrWhiteSpace();
        tx.ReferenceCode.ShouldStartWith("WTX");
    }

    [Fact]
    public void Constructor_Should_Set_Optional_Fields()
    {
        var sessionId = Guid.NewGuid();

        var tx = new WalletTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WalletTransactionType.SessionPayment,
            -50000m,
            50000m,
            PaymentGateway.Wallet,
            "GW123",
            sessionId,
            "Session payment");

        tx.PaymentGateway.ShouldBe(PaymentGateway.Wallet);
        tx.GatewayTransactionId.ShouldBe("GW123");
        tx.RelatedSessionId.ShouldBe(sessionId);
        tx.Description.ShouldBe("Session payment");
    }

    [Fact]
    public void MarkCompleted_Should_Set_Status_To_Completed()
    {
        var tx = CreateTransaction();

        tx.MarkCompleted();

        tx.Status.ShouldBe(TransactionStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_Should_Set_GatewayTransactionId_When_Provided()
    {
        var tx = CreateTransaction();

        tx.MarkCompleted("GW_TX_456");

        tx.Status.ShouldBe(TransactionStatus.Completed);
        tx.GatewayTransactionId.ShouldBe("GW_TX_456");
    }

    [Fact]
    public void MarkCompleted_Should_Not_Change_GatewayTransactionId_When_Null()
    {
        var tx = new WalletTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WalletTransactionType.TopUp,
            100000m,
            100000m,
            PaymentGateway.ZaloPay,
            "ORIGINAL_GW");

        tx.MarkCompleted();

        tx.GatewayTransactionId.ShouldBe("ORIGINAL_GW");
    }

    [Fact]
    public void MarkFailed_Should_Set_Status_To_Failed()
    {
        var tx = CreateTransaction();

        tx.MarkFailed();

        tx.Status.ShouldBe(TransactionStatus.Failed);
    }

    [Fact]
    public void MarkCancelled_Should_Set_Status_To_Cancelled_From_Pending()
    {
        var tx = CreateTransaction();

        tx.MarkCancelled();

        tx.Status.ShouldBe(TransactionStatus.Cancelled);
    }

    [Fact]
    public void MarkCancelled_Should_Throw_When_Status_Is_Completed()
    {
        var tx = CreateTransaction();
        tx.MarkCompleted();

        var ex = Should.Throw<BusinessException>(() => tx.MarkCancelled());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Wallet.TransactionAlreadyCompleted);
    }

    [Fact]
    public void MarkCancelled_Should_Work_When_Status_Is_Failed()
    {
        var tx = CreateTransaction();
        tx.MarkFailed();

        tx.MarkCancelled();

        tx.Status.ShouldBe(TransactionStatus.Cancelled);
    }

    [Fact]
    public void Each_Transaction_Should_Get_Unique_ReferenceCode()
    {
        var tx1 = CreateTransaction();
        var tx2 = CreateTransaction();

        tx1.ReferenceCode.ShouldNotBe(tx2.ReferenceCode);
    }

    private static WalletTransaction CreateTransaction(Guid? userId = null)
    {
        return new WalletTransaction(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            WalletTransactionType.TopUp,
            100000m,
            100000m,
            PaymentGateway.ZaloPay);
    }
}
