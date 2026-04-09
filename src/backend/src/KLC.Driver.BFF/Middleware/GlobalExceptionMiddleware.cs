using System.Net;
using System.Text.Json;
using Volo.Abp;

namespace KLC.Driver.Middleware;

/// <summary>
/// Global exception handler for Driver BFF.
/// Catches unhandled exceptions and returns standardized error responses.
/// Does not expose stack traces in production.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business exception: {Code} - {Message}", ex.Code, ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Code ?? "BUSINESS_ERROR", ex.Message);
        }
        catch (Volo.Abp.Authorization.AbpAuthorizationException ex)
        {
            _logger.LogWarning(ex, "Authorization exception: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Forbidden, "FORBIDDEN", "You do not have permission to perform this action.");
        }
        catch (Volo.Abp.Domain.Entities.EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "NOT_FOUND", "The requested resource was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            var message = _environment.IsProduction()
                ? "An unexpected error occurred. Please try again later."
                : ex.Message;

            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", message);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = new
            {
                code,
                message
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
