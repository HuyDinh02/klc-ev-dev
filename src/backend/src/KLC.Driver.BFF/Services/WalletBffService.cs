using System.Collections.Generic;
using System.Linq;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;

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
    private readonly IDriverHubNotifier _driverNotifier;

    public WalletBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<WalletBffService> logger,
        WalletDomainService walletDomainService,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _walletDomainService = walletDomainService;
        _paymentGateways = paymentGateways;
        _driverNotifier = driverNotifier;
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:wallet-balance";

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

        // Minimum top-up: 10,000 VND
        if (request.Amount < 10_000)
        {
            return new TopUpResultDto { Success = false, Error = "Minimum top-up amount is 10,000 VND" };
        }

        // Maximum top-up: 10,000,000 VND per transaction
        if (request.Amount > 10_000_000)
        {
            return new TopUpResultDto { Success = false, Error = "Maximum top-up amount is 10,000,000 VND" };
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
                    ReturnUrl = "klc://wallet/topup/callback",
                    NotifyUrl = "/api/v1/wallet/topup/callback",
                    ClientIpAddress = request.ClientIpAddress
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
            await _cache.RemoveAsync($"user:{transaction.UserId}:wallet-balance");
            await _cache.RemoveAsync($"user:{transaction.UserId}:profile");

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
        // Build raw query string for signature verification
        var rawData = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var vnpayGateway = _paymentGateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);
        if (vnpayGateway == null)
        {
            _logger.LogWarning("[VnPay IPN] VnPay gateway service not registered");
            return VnPayIpnResponse.UnknownError();
        }

        // Step 1: Verify signature
        var verifyResult = await vnpayGateway.VerifyCallbackAsync(rawData, null);
        if (!verifyResult.IsValid)
        {
            _logger.LogWarning("[VnPay IPN] Invalid signature for wallet top-up");
            return VnPayIpnResponse.InvalidSignature();
        }

        // Step 2: Find wallet transaction by TxnRef (= ReferenceCode)
        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef");
        if (string.IsNullOrEmpty(txnRef))
        {
            return VnPayIpnResponse.OrderNotFound();
        }

        var transaction = await _dbContext.WalletTransactions
            .FirstOrDefaultAsync(t => t.ReferenceCode == txnRef);

        if (transaction == null)
        {
            _logger.LogWarning("[VnPay IPN] Wallet transaction not found: TxnRef={TxnRef}", txnRef);
            return VnPayIpnResponse.OrderNotFound();
        }

        // Step 3: Idempotency — already completed
        if (transaction.Status == TransactionStatus.Completed)
        {
            return VnPayIpnResponse.AlreadyConfirmed();
        }

        // Step 4: Verify amount
        if (verifyResult.CallbackAmount.HasValue && verifyResult.CallbackAmount.Value != transaction.Amount)
        {
            _logger.LogWarning(
                "[VnPay IPN] Amount mismatch: Expected={Expected}, Got={Got}, TxnRef={TxnRef}",
                transaction.Amount, verifyResult.CallbackAmount.Value, txnRef);
            return VnPayIpnResponse.InvalidAmount();
        }

        // Step 5: Process based on response code
        var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode");
        if (responseCode == "00")
        {
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == transaction.UserId);

            if (user == null)
            {
                _logger.LogWarning("[VnPay IPN] User not found for transaction {TxnRef}", txnRef);
                return VnPayIpnResponse.UnknownError();
            }

            var (newBalance, _) = _walletDomainService.TopUp(
                user,
                transaction.Amount,
                transaction.PaymentGateway ?? PaymentGateway.VnPay,
                verifyResult.GatewayTransactionId);

            transaction.MarkCompleted(verifyResult.GatewayTransactionId);
            await _dbContext.SaveChangesAsync();

            // Invalidate cache
            await _cache.RemoveAsync($"user:{transaction.UserId}:wallet-balance");
            await _cache.RemoveAsync($"user:{transaction.UserId}:profile");

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

            _logger.LogInformation(
                "[VnPay IPN] Wallet top-up completed: UserId={UserId}, Amount={Amount}, NewBalance={NewBalance}",
                transaction.UserId, transaction.Amount, newBalance);
        }
        else
        {
            transaction.MarkFailed();
            await _dbContext.SaveChangesAsync();

            _logger.LogWarning(
                "[VnPay IPN] Wallet top-up failed: TxnRef={TxnRef}, ResponseCode={ResponseCode}",
                txnRef, responseCode);
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
        var cacheKey = $"user:{userId}:wallet-summary";

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
