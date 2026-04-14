using System.Collections.Generic;
using System.Linq;
using KLC.Configuration;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<WalletBffService> _logger;
    private readonly IWalletAppService _walletAppService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDriverHubNotifier _driverNotifier;

    public WalletBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<WalletBffService> logger,
        IWalletAppService walletAppService,
        IServiceScopeFactory serviceScopeFactory,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _walletAppService = walletAppService;
        _serviceScopeFactory = serviceScopeFactory;
        _driverNotifier = driverNotifier;
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
        // Delegate business logic to Application layer
        var result = await _walletAppService.InitiateTopUpAsync(new InitiateTopUpInput
        {
            UserId = userId,
            Amount = request.Amount,
            Gateway = request.Gateway,
            ClientIpAddress = request.ClientIpAddress,
            BankCode = request.BankCode
        });

        // Map Application DTO to BFF DTO (same shape, preserves mobile API contract)
        return new TopUpResultDto
        {
            Success = result.Success,
            TransactionId = result.TransactionId,
            ReferenceCode = result.ReferenceCode,
            RedirectUrl = result.RedirectUrl,
            Status = result.Status,
            Error = result.Error
        };
    }

    public async Task<TopUpCallbackResultDto> ProcessTopUpCallbackAsync(TopUpCallbackRequest request)
    {
        // Delegate business logic to Application layer
        var result = await _walletAppService.ProcessTopUpCallbackAsync(new ProcessTopUpCallbackInput
        {
            ReferenceCode = request.ReferenceCode,
            GatewayTransactionId = request.GatewayTransactionId,
            Status = request.Status,
            Gateway = request.Gateway
        });

        // BFF handles cache invalidation and notifications
        if (result.Success && result.NewBalance.HasValue)
        {
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(result.UserId));
            await _cache.RemoveAsync(CacheKeys.UserProfile(result.UserId));

            // Notify user via SignalR
            await _driverNotifier.NotifyWalletBalanceChangedAsync(result.UserId,
                new WalletBalanceChangedMessage
                {
                    UserId = result.UserId,
                    NewBalance = result.NewBalance.Value,
                    ChangeAmount = result.Amount,
                    Reason = $"Top-up via payment gateway",
                    Timestamp = DateTime.UtcNow
                });
        }

        return new TopUpCallbackResultDto
        {
            Success = result.Success,
            TransactionId = result.TransactionId,
            NewBalance = result.NewBalance,
            Error = result.Error
        };
    }

    public async Task<VnPayIpnResponse> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams)
    {
        // Delegate all business logic to Application layer
        var result = await _walletAppService.ProcessVnPayIpnAsync(queryParams);

        // BFF handles cache invalidation, SignalR notifications, and push notifications
        if (result.Completion is { Success: true })
        {
            var completion = result.Completion;

            // Invalidate cache
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(completion.UserId));
            await _cache.RemoveAsync(CacheKeys.UserProfile(completion.UserId));

            // Notify user via SignalR
            await _driverNotifier.NotifyWalletBalanceChangedAsync(completion.UserId,
                new WalletBalanceChangedMessage
                {
                    UserId = completion.UserId,
                    NewBalance = completion.NewBalance,
                    ChangeAmount = completion.Amount,
                    Reason = "Top-up via VnPay",
                    Timestamp = DateTime.UtcNow
                });

            // Push notification in a separate DI scope to avoid DbContext concurrency
            // with the IPN handler's UoW (FirebasePush queries DeviceTokens on the same DbContext)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                    await pushService.SendToUserAsync(
                        completion.UserId,
                        "Nạp ví thành công 💰",
                        $"Đã nạp {completion.Amount:N0}đ vào ví. Số dư: {completion.NewBalance:N0}đ",
                        new Dictionary<string, string>
                        {
                            { "type", "wallet_topup" },
                            { "amount", completion.Amount.ToString("F0") },
                            { "newBalance", completion.NewBalance.ToString("F0") }
                        });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Push notification failed for wallet topup"); }
            });
        }
        else if (result.Failure != null)
        {
            var failure = result.Failure;

            // Notify via SignalR (in-app real-time)
            try
            {
                await _driverNotifier.NotifyWalletBalanceChangedAsync(failure.UserId,
                    new WalletBalanceChangedMessage
                    {
                        UserId = failure.UserId,
                        NewBalance = 0, // unchanged
                        ChangeAmount = 0,
                        Reason = $"Top-up failed (VnPay code: {failure.ResponseCode})",
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception notifyEx)
            {
                _logger.LogWarning(notifyEx, "Failed to send failure notification via SignalR");
            }

            // Push notification in a separate DI scope (same reason as success path above)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                    await pushService.SendToUserAsync(
                        failure.UserId,
                        "Nạp ví thất bại ❌",
                        $"Giao dịch nạp {failure.Amount:N0}đ qua VnPay không thành công. Vui lòng thử lại.",
                        new Dictionary<string, string>
                        {
                            { "type", "wallet_topup_failed" },
                            { "amount", failure.Amount.ToString("F0") },
                            { "referenceCode", failure.ReferenceCode }
                        });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Push notification failed for wallet topup failure"); }
            });
        }

        return result.IpnResponse;
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
