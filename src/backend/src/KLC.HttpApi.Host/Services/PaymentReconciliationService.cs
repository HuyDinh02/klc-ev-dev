using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Services;

/// <summary>
/// Background service that reconciles pending VnPay transactions by querying
/// VnPay's querydr API. Runs every 15 minutes. Also expires transactions
/// older than 24 hours that were never completed.
/// </summary>
public class PaymentReconciliationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReconciliationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _pendingThreshold = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _expiryThreshold = TimeSpan.FromHours(24);

    public PaymentReconciliationService(
        IServiceProvider serviceProvider,
        ILogger<PaymentReconciliationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentReconciliationService started (interval: {Interval})", _checkInterval);

        // Wait 2 minutes after startup to let services initialize
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcilePendingTransactionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentReconciliationService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PaymentReconciliationService stopped");
    }

    private async Task ReconcilePendingTransactionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var walletTxnRepo = scope.ServiceProvider.GetRequiredService<IRepository<WalletTransaction, Guid>>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<AppUser, Guid>>();
        var walletDomainService = scope.ServiceProvider.GetRequiredService<WalletDomainService>();
        var gateways = scope.ServiceProvider.GetServices<IPaymentGatewayService>();
        var vnpayGateway = gateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);

        var now = DateTime.UtcNow;
        var pendingCutoff = now - _pendingThreshold;
        var expiryCutoff = now - _expiryThreshold;

        // Step 1: Expire very old pending transactions (> 24h)
        using (var uow = uowManager.Begin(requiresNew: true))
        {
            var expired = await (await walletTxnRepo.GetQueryableAsync())
                .Where(t => t.Status == TransactionStatus.Pending
                    && t.CreationTime < expiryCutoff)
                .ToListAsync();

            foreach (var txn in expired)
            {
                txn.MarkFailed();
                await walletTxnRepo.UpdateAsync(txn);
                _logger.LogInformation(
                    "Expired pending transaction: {RefCode} (age > 24h)",
                    txn.ReferenceCode);
            }

            if (expired.Count > 0)
            {
                await uow.CompleteAsync();
                _logger.LogInformation("Expired {Count} stale transactions", expired.Count);
            }
        }

        // Step 2: Query VnPay for pending transactions between 30 min and 24h old
        if (vnpayGateway == null)
        {
            _logger.LogDebug("VnPay gateway not available, skipping reconciliation query");
            return;
        }

        var pendingVnPay = await (await walletTxnRepo.GetQueryableAsync())
            .Where(t => t.Status == TransactionStatus.Pending
                && t.PaymentGateway == PaymentGateway.VnPay
                && t.CreationTime < pendingCutoff
                && t.CreationTime >= expiryCutoff)
            .OrderBy(t => t.CreationTime)
            .Take(50) // Batch limit
            .ToListAsync();

        if (pendingVnPay.Count == 0) return;

        _logger.LogInformation("Reconciling {Count} pending VnPay transactions", pendingVnPay.Count);

        foreach (var txn in pendingVnPay)
        {
            try
            {
                using var uow = uowManager.Begin(requiresNew: true);

                var queryResult = await vnpayGateway.QueryTransactionAsync(new QueryTransactionRequest
                {
                    TxnRef = txn.ReferenceCode,
                    TransactionDate = txn.CreationTime.AddHours(7).ToString("yyyyMMddHHmmss")
                });

                if (queryResult.IsValid && queryResult.IsSuccess)
                {
                    // VnPay confirms payment was successful — credit wallet.
                    // Look up by IdentityUserId (WalletTransaction.UserId stores IdentityUserId,
                    // not AppUser.Id — using the wrong field was a bug that silently skipped credits).
                    var user = await userRepo.FirstOrDefaultAsync(u => u.IdentityUserId == txn.UserId);
                    if (user != null)
                    {
                        // Go through WalletDomainService to create an audit WalletTransaction
                        // and properly update LastTopUpAt — direct AddToWallet() was missing both.
                        var (_, walletTx) = walletDomainService.TopUp(
                            user,
                            txn.Amount,
                            txn.PaymentGateway ?? PaymentGateway.VnPay,
                            queryResult.GatewayTransactionId);

                        txn.MarkCompleted(queryResult.GatewayTransactionId);
                        await userRepo.UpdateAsync(user);
                        await walletTxnRepo.UpdateAsync(txn);
                        await walletTxnRepo.InsertAsync(walletTx);
                        await uow.CompleteAsync();

                        _logger.LogInformation(
                            "Reconciled VnPay top-up: {RefCode}, amount={Amount}, user={UserId}",
                            txn.ReferenceCode, txn.Amount, txn.UserId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Reconciliation: AppUser not found for IdentityUserId={UserId}, skipping {RefCode}",
                            txn.UserId, txn.ReferenceCode);
                    }
                }
                else if (queryResult.IsValid && !queryResult.IsSuccess)
                {
                    // VnPay confirms payment failed
                    txn.MarkFailed();
                    await walletTxnRepo.UpdateAsync(txn);
                    await uow.CompleteAsync();
                    _logger.LogInformation(
                        "VnPay confirms failure for {RefCode}: {Error}",
                        txn.ReferenceCode, queryResult.ErrorMessage);
                }
                // If query itself failed (network error), skip — retry next cycle
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reconcile transaction {RefCode}", txn.ReferenceCode);
            }
        }
    }
}
