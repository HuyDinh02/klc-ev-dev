using System.Collections.Generic;
using System.Linq;
using KLC.Configuration;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace KLC.Driver.Services;

public interface IWalletBffService
{
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);
    Task<TopUpResultDto> TopUpAsync(Guid userId, TopUpRequest request);
    Task<TopUpStatusDto?> GetTopUpStatusAsync(Guid userId, Guid transactionId);
    Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid userId, Guid? cursor, int pageSize, WalletTransactionType? type);
    Task<WalletTransactionDetailDto?> GetTransactionDetailAsync(Guid userId, Guid transactionId);
    Task<TransactionSummaryDto> GetTransactionSummaryAsync(Guid userId);
    Task<TopUpCallbackResultDto> ProcessTopUpCallbackAsync(TopUpCallbackRequest request);
    Task<VnPayIpnResponse> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams);
}

public class WalletBffService : IWalletBffService
{
    /// <summary>
    /// SBV Circular 41/2025: Monthly e-wallet top-up cap.
    /// </summary>
    private const decimal MonthlyTopUpLimit = 100_000_000m;

    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<WalletBffService> _logger;
    private readonly WalletDomainService _walletDomainService;
    private readonly IEnumerable<IPaymentGatewayService> _paymentGateways;
    private readonly IPaymentCallbackValidator _callbackValidator;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IDriverHubNotifier _driverNotifier;
    private readonly IConfiguration _configuration;
    private readonly WalletSettings _walletSettings;
    private readonly IDatabase _redis;

    public WalletBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<WalletBffService> logger,
        WalletDomainService walletDomainService,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        IPaymentCallbackValidator callbackValidator,
        IPushNotificationService pushNotificationService,
        IDriverHubNotifier driverNotifier,
        IConfiguration configuration,
        IOptions<WalletSettings> walletSettings,
        IConnectionMultiplexer redis)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _walletDomainService = walletDomainService;
        _paymentGateways = paymentGateways;
        _callbackValidator = callbackValidator;
        _pushNotificationService = pushNotificationService;
        _driverNotifier = driverNotifier;
        _configuration = configuration;
        _walletSettings = walletSettings.Value;
        _redis = redis.GetDatabase();
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserWalletBalance(userId);

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await _dbContext.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            if (user == null)
            {
                return new WalletBalanceDto { Balance = 0, Currency = "VND" };
            }

            // Get last transaction for context
            var lastTransaction = await _dbContext.WalletTransactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed)
                .OrderByDescending(t => t.CreationTime)
                .Select(t => new { t.Type, t.Amount, t.CreationTime })
                .FirstOrDefaultAsync();

            return new WalletBalanceDto
            {
                Balance = user.WalletBalance,
                Currency = "VND",
                LastTransactionType = lastTransaction?.Type,
                LastTransactionAmount = lastTransaction?.Amount,
                LastTransactionAt = lastTransaction?.CreationTime
            };
        }, TimeSpan.FromSeconds(30));
    }

    public async Task<TopUpResultDto> TopUpAsync(Guid userId, TopUpRequest request)
    {
        if (request.Amount <= 0)
        {
            return new TopUpResultDto { Success = false, Error = "Amount must be positive" };
        }

        if (request.Amount < _walletSettings.MinTopUpAmount)
        {
            return new TopUpResultDto { Success = false, Error = KLCDomainErrorCodes.Wallet.MinTopUpAmount };
        }

        if (request.Amount > _walletSettings.MaxTopUpAmount)
        {
            return new TopUpResultDto { Success = false, Error = KLCDomainErrorCodes.Wallet.MaxTopUpAmount };
        }

        // SBV Circular 41/2025: Monthly top-up limit of 100,000,000 VND
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyTotal = await _dbContext.WalletTransactions
            .Where(t => t.UserId == userId
                && t.Type == WalletTransactionType.TopUp
                && t.Status == TransactionStatus.Completed
                && t.CreationTime >= monthStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var remaining = MonthlyTopUpLimit - monthlyTotal;
        if (monthlyTotal + request.Amount > MonthlyTopUpLimit)
        {
            return new TopUpResultDto
            {
                Success = false,
                Error = $"Vượt quá hạn mức nạp {remaining:N0}đ/tháng theo quy định SBV"
            };
        }

        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

        if (user == null)
        {
            return new TopUpResultDto { Success = false, Error = "User not found" };
        }

        try
        {
            // Create a pending transaction first (gateway will confirm later)
            var transaction = new WalletTransaction(
                Guid.NewGuid(),
                userId,
                WalletTransactionType.TopUp,
                request.Amount,
                user.WalletBalance, // Balance not yet updated
                request.Gateway,
                description: $"Top-up via {request.Gateway}");

            await _dbContext.WalletTransactions.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            // Call payment gateway to get redirect URL
            var gateway = _paymentGateways.FirstOrDefault(g => g.Gateway == request.Gateway);
            var gatewayResult = gateway != null
                ? await gateway.CreateTopUpAsync(new CreateTopUpRequest
                {
                    ReferenceCode = transaction.ReferenceCode,
                    Amount = request.Amount,
                    Description = $"Top-up via {request.Gateway}",
                    ReturnUrl = _configuration["Payment:VnPay:ReturnUrl"] ?? "klc://wallet/topup/callback",
                    NotifyUrl = _configuration["Payment:VnPay:IpnUrl"]
                        ?? $"{_configuration["App:SelfUrl"] ?? "https://bff.ev.odcall.com"}/api/v1/wallet/topup/vnpay-ipn",
                    ClientIpAddress = request.ClientIpAddress,
                    BankCode = request.BankCode
                })
                : PaymentGatewayResult.Fail($"Gateway {request.Gateway} not supported");

            if (!gatewayResult.Success)
            {
                return new TopUpResultDto { Success = false, Error = gatewayResult.ErrorMessage };
            }

            var redirectUrl = gatewayResult.RedirectUrl!;

            _logger.LogInformation(
                "Top-up initiated: UserId={UserId}, Amount={Amount}, Gateway={Gateway}, TransactionId={TransactionId}",
                userId, request.Amount, request.Gateway, transaction.Id);

            return new TopUpResultDto
            {
                Success = true,
                TransactionId = transaction.Id,
                ReferenceCode = transaction.ReferenceCode,
                RedirectUrl = redirectUrl,
                Status = transaction.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate top-up for user {UserId}", userId);
            return new TopUpResultDto { Success = false, Error = "Failed to initiate top-up" };
        }
    }

    public async Task<TopUpCallbackResultDto> ProcessTopUpCallbackAsync(TopUpCallbackRequest request)
    {
        var transaction = await _dbContext.WalletTransactions
            .FirstOrDefaultAsync(t => t.ReferenceCode == request.ReferenceCode &&
                                      t.Status == TransactionStatus.Pending);

        if (transaction == null)
        {
            _logger.LogWarning("Top-up callback for unknown reference: {ReferenceCode}", request.ReferenceCode);
            return new TopUpCallbackResultDto { Success = false, Error = "Transaction not found" };
        }

        if (request.Status == TransactionStatus.Completed)
        {
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == transaction.UserId);

            if (user == null)
            {
                return new TopUpCallbackResultDto { Success = false, Error = "User not found" };
            }

            // Use domain service for the actual wallet credit
            var (newBalance, _) = _walletDomainService.TopUp(
                user,
                transaction.Amount,
                transaction.PaymentGateway ?? PaymentGateway.Wallet,
                request.GatewayTransactionId);

            // Mark original pending transaction as completed
            transaction.MarkCompleted(request.GatewayTransactionId);
            await _dbContext.SaveChangesAsync();

            // Invalidate cache
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(transaction.UserId));
            await _cache.RemoveAsync(CacheKeys.UserProfile(transaction.UserId));

            _logger.LogInformation(
                "Top-up completed: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                transaction.UserId, transaction.Amount, newBalance);

            // Notify user via SignalR
            await _driverNotifier.NotifyWalletBalanceChangedAsync(transaction.UserId,
                new WalletBalanceChangedMessage
                {
                    UserId = transaction.UserId,
                    NewBalance = newBalance,
                    ChangeAmount = transaction.Amount,
                    Reason = $"Top-up via {transaction.PaymentGateway}",
                    Timestamp = DateTime.UtcNow
                });

            return new TopUpCallbackResultDto
            {
                Success = true,
                TransactionId = transaction.Id,
                NewBalance = newBalance
            };
        }
        else if (request.Status == TransactionStatus.Failed)
        {
            transaction.MarkFailed();
            await _dbContext.SaveChangesAsync();

            _logger.LogWarning(
                "Top-up failed: UserId={UserId}, ReferenceCode={ReferenceCode}",
                transaction.UserId, request.ReferenceCode);

            return new TopUpCallbackResultDto
            {
                Success = false,
                TransactionId = transaction.Id,
                Error = "Payment gateway reported failure"
            };
        }

        return new TopUpCallbackResultDto { Success = false, Error = "Invalid callback status" };
    }

    public async Task<VnPayIpnResponse> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams)
    {
        // Step 1: Find wallet transaction by TxnRef
        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef");
        if (string.IsNullOrEmpty(txnRef))
            return VnPayIpnResponse.OrderNotFound();

        // Idempotency lock: VnPay guarantees at-least-once IPN delivery and can retry
        // within milliseconds. Without this lock, two concurrent requests can both pass
        // the Status == Completed check and double-credit the wallet.
        // The lock is per-txnRef so unrelated top-ups are never blocked.
        var lockKey = $"ipn:wallet:{txnRef}";
        var lockAcquired = await _redis.StringSetAsync(
            lockKey, "1", TimeSpan.FromSeconds(60), When.NotExists);
        if (!lockAcquired)
        {
            _logger.LogWarning(
                "[VnPay IPN] Concurrent IPN for TxnRef={TxnRef} — skipping duplicate", txnRef);
            return VnPayIpnResponse.AlreadyConfirmed();
        }

        var transaction = await _dbContext.WalletTransactions
            .FirstOrDefaultAsync(t => t.ReferenceCode == txnRef);

        if (transaction == null)
        {
            _logger.LogWarning("[VnPay IPN] Wallet transaction not found: TxnRef={TxnRef}", txnRef);
            return VnPayIpnResponse.OrderNotFound();
        }

        if (transaction.Status == TransactionStatus.Completed)
            return VnPayIpnResponse.AlreadyConfirmed();

        // Step 2: Validate signature and amount via shared validator
        var validation = await _callbackValidator.ValidateVnPayIpnAsync(queryParams, transaction.Amount);
        if (!validation.IsValid)
        {
            _logger.LogWarning("[VnPay IPN] Validation failed: {Error}, TxnRef={TxnRef}",
                validation.ErrorMessage, txnRef);
            return validation.ErrorMessage?.Contains("Amount") == true
                ? VnPayIpnResponse.InvalidAmount()
                : VnPayIpnResponse.InvalidSignature();
        }

        // Step 3: Process based on response code
        if (validation.IsPaymentSuccess)
        {
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == transaction.UserId);

            if (user == null)
            {
                _logger.LogWarning("[VnPay IPN] User not found for transaction {TxnRef}", txnRef);
                return VnPayIpnResponse.UnknownError();
            }

            // Retry loop handles the rare case where a concurrent operation (e.g.
            // a session payment) modified the user row between our read and save.
            // xmin concurrency token causes DbUpdateConcurrencyException in that case.
            decimal newBalance = 0;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        // Reload the user so xmin is fresh before retrying
                        await _dbContext.Entry(user).ReloadAsync();
                    }

                    var (balance, _) = _walletDomainService.TopUp(
                        user,
                        transaction.Amount,
                        transaction.PaymentGateway ?? PaymentGateway.VnPay,
                        validation.GatewayTransactionId);
                    newBalance = balance;

                    transaction.MarkCompleted(validation.GatewayTransactionId);
                    await _dbContext.SaveChangesAsync();
                    break;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) when (attempt < 2)
                {
                    _logger.LogWarning(
                        "[VnPay IPN] Concurrency conflict on wallet update for {UserId}, retrying (attempt {Attempt})",
                        transaction.UserId, attempt + 1);
                }
            }

            // Invalidate cache
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(transaction.UserId));
            await _cache.RemoveAsync(CacheKeys.UserProfile(transaction.UserId));

            // Notify user via SignalR
            await _driverNotifier.NotifyWalletBalanceChangedAsync(transaction.UserId,
                new WalletBalanceChangedMessage
                {
                    UserId = transaction.UserId,
                    NewBalance = newBalance,
                    ChangeAmount = transaction.Amount,
                    Reason = $"Top-up via VnPay",
                    Timestamp = DateTime.UtcNow
                });

            // Create in-app notification for successful top-up
            var successNotification = new KLC.Notifications.Notification(
                Guid.NewGuid(),
                transaction.UserId,
                KLC.Enums.NotificationType.WalletTopUp,
                "Nạp ví thành công",
                $"Bạn đã nạp thành công {transaction.Amount:N0}đ vào ví. Số dư hiện tại: {newBalance:N0}đ.");
            _dbContext.SetAuditFields(successNotification);
            _dbContext.Notifications.Add(successNotification);
            await _dbContext.SaveChangesAsync();

            // Push notification: topup success
            _ = Task.Run(async () =>
            {
                try
                {
                    await _pushNotificationService.SendToUserAsync(
                        transaction.UserId,
                        "Nạp ví thành công 💰",
                        $"Đã nạp {transaction.Amount:N0}đ vào ví. Số dư: {newBalance:N0}đ",
                        new Dictionary<string, string>
                        {
                            { "type", "wallet_topup" },
                            { "amount", transaction.Amount.ToString("F0") },
                            { "newBalance", newBalance.ToString("F0") }
                        });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Push notification failed for wallet topup"); }
            });

            _logger.LogInformation(
                "[VnPay IPN] Wallet top-up completed: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                transaction.UserId, transaction.Amount, newBalance);
        }
        else
        {
            transaction.MarkFailed();

            // Create in-app notification for payment failure
            var failNotification = new KLC.Notifications.Notification(
                Guid.NewGuid(),
                transaction.UserId,
                KLC.Enums.NotificationType.PaymentFailed,
                "Nạp ví thất bại",
                $"Giao dịch nạp {transaction.Amount:N0}đ qua VnPay không thành công (mã: {validation.ResponseCode}). Vui lòng thử lại.");
            _dbContext.SetAuditFields(failNotification);
            _dbContext.Notifications.Add(failNotification);

            await _dbContext.SaveChangesAsync();

            // Notify via SignalR (in-app real-time)
            try
            {
                await _driverNotifier.NotifyWalletBalanceChangedAsync(transaction.UserId,
                    new WalletBalanceChangedMessage
                    {
                        UserId = transaction.UserId,
                        NewBalance = 0, // unchanged
                        ChangeAmount = 0,
                        Reason = $"Top-up failed (VnPay code: {validation.ResponseCode})",
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception notifyEx)
            {
                _logger.LogWarning(notifyEx, "Failed to send failure notification via SignalR");
            }

            // Push notification: topup failure
            _ = Task.Run(async () =>
            {
                try
                {
                    await _pushNotificationService.SendToUserAsync(
                        transaction.UserId,
                        "Nạp ví thất bại ❌",
                        $"Giao dịch nạp {transaction.Amount:N0}đ qua VnPay không thành công. Vui lòng thử lại.",
                        new Dictionary<string, string>
                        {
                            { "type", "wallet_topup_failed" },
                            { "amount", transaction.Amount.ToString("F0") },
                            { "referenceCode", transaction.ReferenceCode }
                        });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Push notification failed for wallet topup failure"); }
            });

            _logger.LogWarning(
                "[VnPay IPN] Wallet top-up failed: TxnRef={TxnRef}, ResponseCode={ResponseCode}",
                txnRef, validation.ResponseCode);
        }

        return VnPayIpnResponse.Success();
    }

    public async Task<TopUpStatusDto?> GetTopUpStatusAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.WalletTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId &&
                                      t.UserId == userId &&
                                      t.Type == WalletTransactionType.TopUp);

        if (transaction == null) return null;

        return new TopUpStatusDto
        {
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            Status = transaction.Status,
            Gateway = transaction.PaymentGateway,
            ReferenceCode = transaction.ReferenceCode,
            GatewayTransactionId = transaction.GatewayTransactionId,
            CreatedAt = transaction.CreationTime
        };
    }

    public async Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(
        Guid userId, Guid? cursor, int pageSize, WalletTransactionType? type)
    {
        var query = _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreationTime);

        if (type.HasValue)
        {
            query = (IOrderedQueryable<WalletTransaction>)query
                .Where(t => t.Type == type.Value);
        }

        if (cursor.HasValue)
        {
            var cursorTransaction = await _dbContext.WalletTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == cursor.Value);

            if (cursorTransaction != null)
            {
                query = (IOrderedQueryable<WalletTransaction>)query
                    .Where(t => t.CreationTime < cursorTransaction.CreationTime);
            }
        }

        var transactions = await query
            .Take(pageSize + 1)
            .Select(t => new WalletTransactionDto
            {
                TransactionId = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Status = t.Status,
                Description = t.Description,
                ReferenceCode = t.ReferenceCode,
                RelatedSessionId = t.RelatedSessionId,
                CreatedAt = t.CreationTime
            })
            .ToListAsync();

        var hasMore = transactions.Count > pageSize;
        var items = hasMore ? transactions.Take(pageSize).ToList() : transactions;
        var nextCursor = hasMore && items.Any() ? items.Last().TransactionId : (Guid?)null;

        return new PagedResult<WalletTransactionDto>
        {
            Data = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    public async Task<WalletTransactionDetailDto?> GetTransactionDetailAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.WalletTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null) return null;

        // Get related session info if applicable
        string? sessionStationName = null;
        if (transaction.RelatedSessionId.HasValue)
        {
            var session = await _dbContext.ChargingSessions
                .AsNoTracking()
                .Where(s => s.Id == transaction.RelatedSessionId.Value)
                .Select(s => new { s.StationId })
                .FirstOrDefaultAsync();

            if (session != null)
            {
                sessionStationName = await _dbContext.ChargingStations
                    .AsNoTracking()
                    .Where(s => s.Id == session.StationId)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }
        }

        return new WalletTransactionDetailDto
        {
            TransactionId = transaction.Id,
            Type = transaction.Type,
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            Status = transaction.Status,
            Description = transaction.Description,
            ReferenceCode = transaction.ReferenceCode,
            PaymentGateway = transaction.PaymentGateway,
            GatewayTransactionId = transaction.GatewayTransactionId,
            RelatedSessionId = transaction.RelatedSessionId,
            RelatedStationName = sessionStationName,
            CreatedAt = transaction.CreationTime
        };
    }

    public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserWalletSummary(userId);

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var completedTransactions = _dbContext.WalletTransactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed);

            var totalTopUp = await completedTransactions
                .Where(t => t.Type == WalletTransactionType.TopUp)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalSpent = await completedTransactions
                .Where(t => t.Type == WalletTransactionType.SessionPayment)
                .SumAsync(t => (decimal?)Math.Abs(t.Amount)) ?? 0;

            var totalRefunded = await completedTransactions
                .Where(t => t.Type == WalletTransactionType.Refund)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalVoucherCredit = await completedTransactions
                .Where(t => t.Type == WalletTransactionType.VoucherCredit)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var transactionCount = await completedTransactions.CountAsync();

            var user = await _dbContext.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            return new TransactionSummaryDto
            {
                CurrentBalance = user?.WalletBalance ?? 0,
                TotalTopUp = totalTopUp,
                TotalSpent = totalSpent,
                TotalRefunded = totalRefunded,
                TotalVoucherCredit = totalVoucherCredit,
                TransactionCount = transactionCount,
                Currency = "VND"
            };
        }, TimeSpan.FromMinutes(2));
    }

}

// DTOs
public record WalletBalanceDto
{
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "VND";
    public WalletTransactionType? LastTransactionType { get; init; }
    public decimal? LastTransactionAmount { get; init; }
    public DateTime? LastTransactionAt { get; init; }
}

public record TopUpRequest
{
    public decimal Amount { get; init; }
    public PaymentGateway Gateway { get; init; }
    public string? BankCode { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ClientIpAddress { get; init; }
}

public record TopUpResultDto
{
    public bool Success { get; init; }
    public Guid? TransactionId { get; init; }
    public string? ReferenceCode { get; init; }
    public string? RedirectUrl { get; init; }
    public TransactionStatus? Status { get; init; }
    public string? Error { get; init; }
}

public record TopUpCallbackRequest
{
    public string ReferenceCode { get; init; } = string.Empty;
    public string? GatewayTransactionId { get; init; }
    public TransactionStatus Status { get; init; }
    public PaymentGateway? Gateway { get; init; }
}

public record TopUpCallbackResultDto
{
    public bool Success { get; init; }
    public Guid? TransactionId { get; init; }
    public decimal? NewBalance { get; init; }
    public string? Error { get; init; }
}

public record TopUpStatusDto
{
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public TransactionStatus Status { get; init; }
    public PaymentGateway? Gateway { get; init; }
    public string ReferenceCode { get; init; } = string.Empty;
    public string? GatewayTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WalletTransactionDto
{
    public Guid TransactionId { get; init; }
    public WalletTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public TransactionStatus Status { get; init; }
    public string? Description { get; init; }
    public string ReferenceCode { get; init; } = string.Empty;
    public Guid? RelatedSessionId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WalletTransactionDetailDto
{
    public Guid TransactionId { get; init; }
    public WalletTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public TransactionStatus Status { get; init; }
    public string? Description { get; init; }
    public string ReferenceCode { get; init; } = string.Empty;
    public PaymentGateway? PaymentGateway { get; init; }
    public string? GatewayTransactionId { get; init; }
    public Guid? RelatedSessionId { get; init; }
    public string? RelatedStationName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TransactionSummaryDto
{
    public decimal CurrentBalance { get; init; }
    public decimal TotalTopUp { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal TotalRefunded { get; init; }
    public decimal TotalVoucherCredit { get; init; }
    public int TransactionCount { get; init; }
    public string Currency { get; init; } = "VND";
}
