using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Support;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface IFeedbackBffService
{
    Task<FeedbackResultDto> SubmitFeedbackAsync(Guid userId, SubmitFeedbackRequest request);
    Task<PagedResult<FeedbackDto>> GetUserFeedbackAsync(Guid userId, Guid? cursor, int pageSize);
    Task<List<FaqDto>> GetFaqsAsync();
    Task<AboutInfoDto> GetAboutInfoAsync();
}

public class FeedbackBffService : IFeedbackBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<FeedbackBffService> _logger;

    public FeedbackBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<FeedbackBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FeedbackResultDto> SubmitFeedbackAsync(Guid userId, SubmitFeedbackRequest request)
    {
        try
        {
            var feedback = new UserFeedback(
                Guid.NewGuid(),
                userId,
                request.Type,
                request.Subject,
                request.Message);

            await _dbContext.UserFeedbacks.AddAsync(feedback);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} submitted feedback {FeedbackId} of type {Type}",
                userId, feedback.Id, request.Type);

            return new FeedbackResultDto
            {
                Success = true,
                FeedbackId = feedback.Id,
                Status = feedback.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feedback for user {UserId}", userId);
            return new FeedbackResultDto { Success = false, Error = "Failed to submit feedback" };
        }
    }

    public async Task<PagedResult<FeedbackDto>> GetUserFeedbackAsync(Guid userId, Guid? cursor, int pageSize)
    {
        var query = _dbContext.UserFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreationTime).ThenByDescending(f => f.Id);

        if (cursor.HasValue)
        {
            var cursorFeedback = await _dbContext.UserFeedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == cursor.Value);

            if (cursorFeedback != null)
            {
                query = (IOrderedQueryable<UserFeedback>)query
                    .Where(f => f.CreationTime < cursorFeedback.CreationTime
                        || (f.CreationTime == cursorFeedback.CreationTime && f.Id.CompareTo(cursorFeedback.Id) < 0));
            }
        }

        var feedbacks = await query
            .Take(pageSize + 1)
            .Select(f => new FeedbackDto
            {
                Id = f.Id,
                Type = f.Type,
                Subject = f.Subject,
                Message = f.Message,
                Status = f.Status,
                AdminResponse = f.AdminResponse,
                RespondedAt = f.RespondedAt,
                CreatedAt = f.CreationTime
            })
            .ToListAsync();

        var hasMore = feedbacks.Count > pageSize;
        var items = hasMore ? feedbacks.Take(pageSize).ToList() : feedbacks;
        var nextCursor = hasMore && items.Any() ? items.Last().Id : (Guid?)null;

        return new PagedResult<FeedbackDto>
        {
            Data = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    public async Task<List<FaqDto>> GetFaqsAsync()
    {
        var cacheKey = "support:faq";

        return await _cache.GetOrSetAsync(cacheKey, () =>
        {
            // Static FAQ data - in production, this could come from a CMS or database
            var faqs = new List<FaqDto>
            {
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Question = "How do I start a charging session?",
                    Answer = "Navigate to a station on the map, select an available connector, and tap 'Start Charging'. Make sure your vehicle is connected before starting.",
                    Category = "Charging",
                    Order = 1
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Question = "How am I charged for a session?",
                    Answer = "Charging is billed per kWh based on the station's tariff rate. The total cost is calculated at the end of each session. You can view the rate before starting a session.",
                    Category = "Payment",
                    Order = 2
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Question = "What payment methods are supported?",
                    Answer = "We support MoMo, ZaloPay, VNPay, and in-app wallet top-up. You can add and manage payment methods in your profile settings.",
                    Category = "Payment",
                    Order = 3
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    Question = "What do I do if the charger is not working?",
                    Answer = "Please report the issue through the 'Submit Feedback' option in the app, selecting 'Charging Issue' as the type. Our team will investigate and respond promptly.",
                    Category = "Support",
                    Order = 4
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Question = "How do I use a voucher?",
                    Answer = "Go to the Vouchers section, enter your voucher code, and it will be applied to your next eligible charging session automatically.",
                    Category = "Promotions",
                    Order = 5
                },
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                    Question = "Can I stop a charging session remotely?",
                    Answer = "Yes, you can stop your active charging session at any time through the app. Go to your active session and tap 'Stop Charging'.",
                    Category = "Charging",
                    Order = 6
                }
            };

            return Task.FromResult(faqs);
        }, TimeSpan.FromHours(1));
    }

    public async Task<AboutInfoDto> GetAboutInfoAsync()
    {
        var cacheKey = "support:about";

        return await _cache.GetOrSetAsync(cacheKey, () =>
        {
            var about = new AboutInfoDto
            {
                AppName = "KLC EV Charging",
                Version = "1.0.0",
                CompanyName = "KLC Vietnam",
                Website = "https://klc.vn",
                SupportEmail = "support@klc.vn",
                SupportPhone = "1900-xxxx",
                PrivacyPolicyUrl = "https://klc.vn/privacy",
                TermsOfServiceUrl = "https://klc.vn/terms",
                Description = "KLC EV Charging is a smart EV charging station management platform, providing convenient and reliable charging services across Vietnam."
            };

            return Task.FromResult(about);
        }, TimeSpan.FromHours(24));
    }
}

// DTOs
public record SubmitFeedbackRequest
{
    public FeedbackType Type { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record FeedbackResultDto
{
    public bool Success { get; init; }
    public Guid? FeedbackId { get; init; }
    public FeedbackStatus? Status { get; init; }
    public string? Error { get; init; }
}

public record FeedbackDto
{
    public Guid Id { get; init; }
    public FeedbackType Type { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public FeedbackStatus Status { get; init; }
    public string? AdminResponse { get; init; }
    public DateTime? RespondedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record FaqDto
{
    public Guid Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int Order { get; init; }
}

public record AboutInfoDto
{
    public string AppName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
    public string SupportEmail { get; init; } = string.Empty;
    public string SupportPhone { get; init; } = string.Empty;
    public string PrivacyPolicyUrl { get; init; } = string.Empty;
    public string TermsOfServiceUrl { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
