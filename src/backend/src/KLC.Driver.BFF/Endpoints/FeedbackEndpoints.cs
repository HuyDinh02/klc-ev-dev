using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var feedbackGroup = app.MapGroup("/api/v1/feedback")
            .WithTags("Feedback")
            .RequireAuthorization();

        // POST /api/v1/feedback
        feedbackGroup.MapPost("/", async (
            [FromBody] SubmitFeedbackRequest request,
            ClaimsPrincipal user,
            IFeedbackBffService feedbackService) =>
        {
            var userId = GetUserId(user);
            var result = await feedbackService.SubmitFeedbackAsync(userId, request);

            return result.Success
                ? Results.Created($"/api/v1/feedback/{result.FeedbackId}", result)
                : Results.BadRequest(new { error = new { code = "FEEDBACK_FAILED", message = result.Error } });
        })
        .WithName("SubmitFeedback")
        .WithSummary("Submit user feedback or support ticket")
        .Produces<FeedbackResultDto>(201)
        .Produces(400);

        // GET /api/v1/feedback
        feedbackGroup.MapGet("/", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int pageSize = 20,
            ClaimsPrincipal user = null!,
            IFeedbackBffService feedbackService = null!) =>
        {
            var userId = GetUserId(user);
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await feedbackService.GetUserFeedbackAsync(userId, cursor, pageSize);
            return Results.Ok(new
            {
                data = result.Data,
                pagination = new
                {
                    nextCursor = result.NextCursor,
                    hasMore = result.HasMore,
                    pageSize = result.PageSize
                }
            });
        })
        .WithName("GetUserFeedback")
        .WithSummary("List user's feedback and support tickets")
        .Produces<object>(200);

        // Support group
        var supportGroup = app.MapGroup("/api/v1/support")
            .WithTags("Support")
            .RequireAuthorization();

        // GET /api/v1/support/faq
        supportGroup.MapGet("/faq", async (
            IFeedbackBffService feedbackService) =>
        {
            var faqs = await feedbackService.GetFaqsAsync();
            return Results.Ok(new { data = faqs });
        })
        .WithName("GetFaqs")
        .WithSummary("Get frequently asked questions")
        .Produces<object>(200);

        // GET /api/v1/support/about
        supportGroup.MapGet("/about", async (
            IFeedbackBffService feedbackService) =>
        {
            var about = await feedbackService.GetAboutInfoAsync();
            return Results.Ok(about);
        })
        .WithName("GetAboutInfo")
        .WithSummary("Get application information")
        .Produces<AboutInfoDto>(200);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
