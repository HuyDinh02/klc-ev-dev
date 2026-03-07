using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Marketing;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface IPaymentBffService
{
    Task<PaymentResultDto> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request);
    Task<PagedResult<PaymentHistoryDto>> GetPaymentHistoryAsync(Guid userId, Guid? cursor, int pageSize);
    Task<PaymentDetailDto?> GetPaymentDetailAsync(Guid userId, Guid paymentId);
    Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(Guid userId);
    Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, AddPaymentMethodRequest request);
    Task DeletePaymentMethodAsync(Guid userId, Guid methodId);
    Task SetDefaultPaymentMethodAsync(Guid userId, Guid methodId);
}

public class PaymentBffService : IPaymentBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<PaymentBffService> _logger;
    private readonly IEnumerable<IPaymentGatewayService> _paymentGateways;
    private readonly WalletDomainService _walletDomainService;
    private readonly IDriverHubNotifier _driverNotifier;

    public PaymentBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<PaymentBffService> logger,
        IEnumerable<IPaymentGatewayService> paymentGateways,
        WalletDomainService walletDomainService,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _paymentGateways = paymentGateways;
        _walletDomainService = walletDomainService;
        _driverNotifier = driverNotifier;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request)
    {
        var session = await _dbContext.ChargingSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

        if (session == null)
        {
            return new PaymentResultDto { Success = false, Error = "Session not found" };
        }

        if (session.Status != SessionStatus.Completed)
        {
            return new PaymentResultDto { Success = false, Error = "Session not completed" };
        }

        // Check for existing payment
        var existingPayment = await _dbContext.PaymentTransactions
            .FirstOrDefaultAsync(p => p.SessionId == request.SessionId &&
                                      p.Status == PaymentStatus.Completed);

        if (existingPayment != null)
        {
            return new PaymentResultDto { Success = false, Error = "Payment already processed" };
        }

        var sessionCost = session.TotalCost;
        decimal voucherDiscount = 0;
        Voucher? voucher = null;

        // Apply voucher discount if provided
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            voucher = await _dbContext.Vouchers
                .FirstOrDefaultAsync(v => v.Code == request.VoucherCode && !v.IsDeleted);

            if (voucher == null)
            {
                return new PaymentResultDto { Success = false, Error = "Voucher not found" };
            }

            if (!voucher.IsValid())
            {
                return new PaymentResultDto { Success = false, Error = "Voucher is not valid or has expired" };
            }

            // Check user hasn't already used this voucher
            var alreadyUsed = await _dbContext.UserVouchers
                .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.Id && uv.IsUsed);

            if (alreadyUsed)
            {
                return new PaymentResultDto { Success = false, Error = "You have already used this voucher" };
            }

            // Check minimum order amount
            if (voucher.MinOrderAmount.HasValue && sessionCost < voucher.MinOrderAmount.Value)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Error = $"Minimum order amount is {voucher.MinOrderAmount.Value:N0}đ for this voucher"
                };
            }

            // Calculate discount
            voucherDiscount = voucher.Type switch
            {
                VoucherType.FixedAmount => Math.Min(voucher.Value, sessionCost),
                VoucherType.Percentage => Math.Min(
                    sessionCost * voucher.Value / 100m,
                    voucher.MaxDiscountAmount ?? sessionCost),
                VoucherType.FreeCharging => sessionCost,
                _ => 0
            };
        }

        var finalAmount = sessionCost - voucherDiscount;

        // Record voucher usage if applied
        if (voucher != null && voucherDiscount > 0)
        {
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            if (user == null)
            {
                return new PaymentResultDto { Success = false, Error = "User not found" };
            }

            // Create voucher credit transaction
            var (_, voucherTransaction) = _walletDomainService.ApplyVoucher(
                user, voucherDiscount, $"Voucher {voucher.Code} discount for session");

            // Deduct immediately for the session (net effect: discount applied)
            user.DeductFromWallet(voucherDiscount);

            var userVoucher = new UserVoucher(Guid.NewGuid(), userId, voucher.Id);
            userVoucher.MarkUsed();
            voucher.IncrementUsage();

            await _dbContext.WalletTransactions.AddAsync(voucherTransaction);
            await _dbContext.UserVouchers.AddAsync(userVoucher);

            // Invalidate caches
            await _cache.RemoveAsync($"user:{userId}:wallet-balance");
            await _cache.RemoveAsync($"user:{userId}:available-vouchers");
            await _cache.RemoveAsync($"user:{userId}:wallet-summary");
        }

        // If fully covered by voucher or zero-cost session
        if (finalAmount <= 0)
        {
            var paymentGateway = voucher != null ? PaymentGateway.Voucher : request.Gateway;
            var payment = new PaymentTransaction(
                Guid.NewGuid(),
                request.SessionId,
                userId,
                paymentGateway,
                sessionCost);

            payment.MarkProcessing();
            var reference = voucher != null ? $"voucher:{voucher.Code}" : "zero-cost-session";
            payment.MarkCompleted(reference);

            await _dbContext.PaymentTransactions.AddAsync(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Payment completed (no gateway needed): SessionId={SessionId}, VoucherCode={VoucherCode}, Amount={Amount}",
                request.SessionId, voucher?.Code, sessionCost);

            return new PaymentResultDto
            {
                Success = true,
                PaymentId = payment.Id,
                Status = payment.Status,
                VoucherDiscount = voucherDiscount
            };
        }

        // Process remaining amount via gateway
        var gatewayPayment = new PaymentTransaction(
            Guid.NewGuid(),
            request.SessionId,
            userId,
            request.Gateway,
            finalAmount);

        gatewayPayment.MarkProcessing();

        var gateway = _paymentGateways.FirstOrDefault(g => g.Gateway == request.Gateway);
        if (gateway == null)
        {
            gatewayPayment.MarkFailed($"Gateway {request.Gateway} not supported");
            await _dbContext.PaymentTransactions.AddAsync(gatewayPayment);
            await _dbContext.SaveChangesAsync();
            return new PaymentResultDto { Success = false, PaymentId = gatewayPayment.Id, Error = $"Gateway {request.Gateway} not supported" };
        }

        try
        {
            var gatewayResult = await gateway.CreateTopUpAsync(new CreateTopUpRequest
            {
                ReferenceCode = gatewayPayment.ReferenceCode,
                Amount = finalAmount,
                Description = $"Session payment #{gatewayPayment.ReferenceCode}",
                ReturnUrl = "klc://payment/callback",
                NotifyUrl = "/api/v1/payments/callback"
            });

            if (gatewayResult.Success)
            {
                gatewayPayment.MarkCompleted(gatewayResult.GatewayTransactionId ?? "");
            }
            else
            {
                gatewayPayment.MarkFailed(gatewayResult.ErrorMessage ?? "Payment failed");
            }

            await _dbContext.PaymentTransactions.AddAsync(gatewayPayment);
            await _dbContext.SaveChangesAsync();

            return new PaymentResultDto
            {
                Success = gatewayResult.Success,
                PaymentId = gatewayPayment.Id,
                Status = gatewayPayment.Status,
                RedirectUrl = gatewayResult.RedirectUrl,
                VoucherDiscount = voucherDiscount > 0 ? voucherDiscount : null,
                Error = gatewayResult.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment gateway error for session {SessionId}, gateway {Gateway}", request.SessionId, request.Gateway);
            gatewayPayment.MarkFailed("Payment processing error");
            await _dbContext.PaymentTransactions.AddAsync(gatewayPayment);
            await _dbContext.SaveChangesAsync();
            return new PaymentResultDto { Success = false, PaymentId = gatewayPayment.Id, Error = "Payment processing failed" };
        }
    }

    public async Task<PagedResult<PaymentHistoryDto>> GetPaymentHistoryAsync(Guid userId, Guid? cursor, int pageSize)
    {
        var query = _dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreationTime);

        if (cursor.HasValue)
        {
            var cursorPayment = await _dbContext.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == cursor.Value);

            if (cursorPayment != null)
            {
                query = (IOrderedQueryable<PaymentTransaction>)query
                    .Where(p => p.CreationTime < cursorPayment.CreationTime);
            }
        }

        var payments = await query
            .Take(pageSize + 1)
            .Select(p => new PaymentHistoryDto
            {
                PaymentId = p.Id,
                SessionId = p.SessionId,
                Amount = p.Amount,
                Gateway = p.Gateway,
                Status = p.Status,
                CreatedAt = p.CreationTime
            })
            .ToListAsync();

        var hasMore = payments.Count > pageSize;
        var items = hasMore ? payments.Take(pageSize).ToList() : payments;
        var nextCursor = hasMore && items.Any() ? items.Last().PaymentId : (Guid?)null;

        return new PagedResult<PaymentHistoryDto>
        {
            Data = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    public async Task<PaymentDetailDto?> GetPaymentDetailAsync(Guid userId, Guid paymentId)
    {
        var payment = await _dbContext.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

        if (payment == null) return null;

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PaymentTransactionId == paymentId);

        return new PaymentDetailDto
        {
            PaymentId = payment.Id,
            SessionId = payment.SessionId,
            Amount = payment.Amount,
            Gateway = payment.Gateway,
            Status = payment.Status,
            ReferenceCode = payment.ReferenceCode,
            GatewayTransactionId = payment.GatewayTransactionId,
            CreatedAt = payment.CreationTime,
            CompletedAt = payment.CompletedAt,
            InvoiceId = invoice?.Id,
            InvoiceNumber = invoice?.InvoiceNumber
        };
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:payment-methods";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _dbContext.UserPaymentMethods
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderByDescending(m => m.IsDefault)
                .ThenByDescending(m => m.CreationTime)
                .Select(m => new PaymentMethodDto
                {
                    Id = m.Id,
                    Gateway = m.Gateway,
                    DisplayName = m.DisplayName,
                    LastFourDigits = m.LastFourDigits,
                    IsDefault = m.IsDefault
                })
                .ToListAsync();
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, AddPaymentMethodRequest request)
    {
        var method = new UserPaymentMethod(
            Guid.NewGuid(),
            userId,
            request.Gateway,
            request.DisplayName,
            request.TokenReference,
            request.LastFourDigits);

        // If this is the first method, make it default
        var hasOther = await _dbContext.UserPaymentMethods
            .AnyAsync(m => m.UserId == userId && m.IsActive);

        if (!hasOther)
        {
            method.SetAsDefault();
        }

        await _dbContext.UserPaymentMethods.AddAsync(method);
        await _dbContext.SaveChangesAsync();

        await _cache.RemoveAsync($"user:{userId}:payment-methods");

        return new PaymentMethodDto
        {
            Id = method.Id,
            Gateway = method.Gateway,
            DisplayName = method.DisplayName,
            LastFourDigits = method.LastFourDigits,
            IsDefault = method.IsDefault
        };
    }

    public async Task DeletePaymentMethodAsync(Guid userId, Guid methodId)
    {
        var method = await _dbContext.UserPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == methodId && m.UserId == userId);

        if (method != null)
        {
            method.Deactivate();
            await _dbContext.SaveChangesAsync();
            await _cache.RemoveAsync($"user:{userId}:payment-methods");
        }
    }

    public async Task SetDefaultPaymentMethodAsync(Guid userId, Guid methodId)
    {
        var methods = await _dbContext.UserPaymentMethods
            .Where(m => m.UserId == userId && m.IsActive)
            .ToListAsync();

        foreach (var method in methods)
        {
            if (method.Id == methodId)
            {
                method.SetAsDefault();
            }
            else if (method.IsDefault)
            {
                method.RemoveDefault();
            }
        }

        await _dbContext.SaveChangesAsync();
        await _cache.RemoveAsync($"user:{userId}:payment-methods");
    }

}

// DTOs
public record ProcessPaymentRequest
{
    public Guid SessionId { get; init; }
    public PaymentGateway Gateway { get; init; }
    public Guid? PaymentMethodId { get; init; }
    public string? VoucherCode { get; init; }
}

public record PaymentResultDto
{
    public bool Success { get; init; }
    public Guid? PaymentId { get; init; }
    public PaymentStatus? Status { get; init; }
    public string? RedirectUrl { get; init; }
    public decimal? VoucherDiscount { get; init; }
    public string? Error { get; init; }
}

public record PaymentHistoryDto
{
    public Guid PaymentId { get; init; }
    public Guid SessionId { get; init; }
    public decimal Amount { get; init; }
    public PaymentGateway Gateway { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PaymentDetailDto
{
    public Guid PaymentId { get; init; }
    public Guid SessionId { get; init; }
    public decimal Amount { get; init; }
    public PaymentGateway Gateway { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ReferenceCode { get; init; }
    public string? GatewayTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public Guid? InvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
}

public record PaymentMethodDto
{
    public Guid Id { get; init; }
    public PaymentGateway Gateway { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? LastFourDigits { get; init; }
    public bool IsDefault { get; init; }
}

public record AddPaymentMethodRequest
{
    public PaymentGateway Gateway { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string TokenReference { get; init; } = string.Empty;
    public string? LastFourDigits { get; init; }
}
