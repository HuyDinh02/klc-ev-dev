using KLC.EntityFrameworkCore;
using KLC.Enums;
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
    private readonly IPaymentProcessingAppService _paymentProcessingAppService;
    private readonly IDriverHubNotifier _driverNotifier;

    public PaymentBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<PaymentBffService> logger,
        IPaymentProcessingAppService paymentProcessingAppService,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _paymentProcessingAppService = paymentProcessingAppService;
        _driverNotifier = driverNotifier;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(Guid userId, ProcessPaymentRequest request)
    {
        // Delegate business logic to Application layer
        var result = await _paymentProcessingAppService.ProcessSessionPaymentAsync(new ProcessSessionPaymentInput
        {
            UserId = userId,
            SessionId = request.SessionId,
            Gateway = request.Gateway,
            PaymentMethodId = request.PaymentMethodId,
            VoucherCode = request.VoucherCode,
            ClientIpAddress = request.ClientIpAddress
        });

        // BFF handles cache invalidation after payment
        if (result.Success)
        {
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(userId));
            await _cache.RemoveAsync(CacheKeys.UserWalletSummary(userId));
        }

        if (result.VoucherDiscount.HasValue && result.VoucherDiscount.Value > 0)
        {
            await _cache.RemoveAsync(CacheKeys.UserAvailableVouchers(userId));
        }

        // Map Application DTO to BFF DTO (preserves mobile API contract)
        return new PaymentResultDto
        {
            Success = result.Success,
            PaymentId = result.PaymentId,
            Status = result.Status,
            RedirectUrl = result.RedirectUrl,
            VoucherDiscount = result.VoucherDiscount,
            Error = result.Error
        };
    }

    public async Task<PagedResult<PaymentHistoryDto>> GetPaymentHistoryAsync(Guid userId, Guid? cursor, int pageSize)
    {
        var query = _dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreationTime).ThenByDescending(p => p.Id);

        if (cursor.HasValue)
        {
            var cursorPayment = await _dbContext.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == cursor.Value);

            if (cursorPayment != null)
            {
                query = (IOrderedQueryable<PaymentTransaction>)query
                    .Where(p => p.CreationTime < cursorPayment.CreationTime
                        || (p.CreationTime == cursorPayment.CreationTime && p.Id.CompareTo(cursorPayment.Id) < 0));
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
        var cacheKey = CacheKeys.UserPaymentMethods(userId);

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

        await _cache.RemoveAsync(CacheKeys.UserPaymentMethods(userId));

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
            await _cache.RemoveAsync(CacheKeys.UserPaymentMethods(userId));
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
        await _cache.RemoveAsync(CacheKeys.UserPaymentMethods(userId));
    }

}

// DTOs
public record ProcessPaymentRequest
{
    public Guid SessionId { get; init; }
    public PaymentGateway Gateway { get; init; }
    public Guid? PaymentMethodId { get; init; }
    public string? VoucherCode { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ClientIpAddress { get; init; }
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
