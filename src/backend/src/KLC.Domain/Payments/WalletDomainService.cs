using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Users;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace KLC.Payments;

/// <summary>
/// Domain service coordinating wallet balance changes with transaction audit trail.
/// All wallet operations must go through this service to ensure atomicity.
/// </summary>
public class WalletDomainService : DomainService
{
    public (decimal NewBalance, WalletTransaction Transaction) TopUp(
        AppUser user,
        decimal amount,
        PaymentGateway gateway,
        string? gatewayTransactionId = null)
    {
        if (amount <= 0)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InvalidAmount);

        user.AddToWallet(amount);
        user.RecordTopUp();

        var transaction = new WalletTransaction(
            GuidGenerator.Create(),
            user.IdentityUserId,
            WalletTransactionType.TopUp,
            amount,
            user.WalletBalance,
            gateway,
            gatewayTransactionId,
            description: $"Top-up via {gateway}");

        transaction.MarkCompleted(gatewayTransactionId);

        return (user.WalletBalance, transaction);
    }

    public (decimal NewBalance, WalletTransaction Transaction) DeductForSession(
        AppUser user,
        decimal amount,
        Guid sessionId)
    {
        if (amount <= 0)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InvalidAmount);

        user.DeductFromWallet(amount); // Throws if insufficient

        var transaction = new WalletTransaction(
            GuidGenerator.Create(),
            user.IdentityUserId,
            WalletTransactionType.SessionPayment,
            -amount,
            user.WalletBalance,
            PaymentGateway.Wallet,
            relatedSessionId: sessionId,
            description: "Charging session payment");

        transaction.MarkCompleted();

        return (user.WalletBalance, transaction);
    }

    public (decimal NewBalance, WalletTransaction Transaction) Refund(
        AppUser user,
        decimal amount,
        Guid? sessionId = null,
        string? description = null)
    {
        if (amount <= 0)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InvalidAmount);

        user.AddToWallet(amount);

        var transaction = new WalletTransaction(
            GuidGenerator.Create(),
            user.IdentityUserId,
            WalletTransactionType.Refund,
            amount,
            user.WalletBalance,
            relatedSessionId: sessionId,
            description: description ?? "Refund");

        transaction.MarkCompleted();

        return (user.WalletBalance, transaction);
    }

    public (decimal NewBalance, WalletTransaction Transaction) Adjust(
        AppUser user,
        decimal amount,
        string? description = null)
    {
        if (amount > 0)
            user.AddToWallet(amount);
        else if (amount < 0)
            user.DeductFromWallet(Math.Abs(amount));
        else
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InvalidAmount);

        var transaction = new WalletTransaction(
            GuidGenerator.Create(),
            user.IdentityUserId,
            WalletTransactionType.Adjustment,
            amount,
            user.WalletBalance,
            description: description ?? "Admin adjustment");

        transaction.MarkCompleted();

        return (user.WalletBalance, transaction);
    }

    public (decimal NewBalance, WalletTransaction Transaction) ApplyVoucher(
        AppUser user,
        decimal creditAmount,
        string? description = null)
    {
        if (creditAmount <= 0)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InvalidAmount);

        user.AddToWallet(creditAmount);

        var transaction = new WalletTransaction(
            GuidGenerator.Create(),
            user.IdentityUserId,
            WalletTransactionType.VoucherCredit,
            creditAmount,
            user.WalletBalance,
            PaymentGateway.Voucher,
            description: description ?? "Voucher credit");

        transaction.MarkCompleted();

        return (user.WalletBalance, transaction);
    }
}
