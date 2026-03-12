using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Operators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace KLC.Middleware;

/// <summary>
/// Middleware that authenticates requests to /api/v1/operator/ using X-API-Key header.
/// Validates the API key against operator records and sets OperatorId in HttpContext.Items.
/// </summary>
public class OperatorApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperatorApiKeyMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string OperatorApiPathPrefix = "/api/v1/operator";

    public OperatorApiKeyMiddleware(RequestDelegate next, ILogger<OperatorApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // Only intercept requests to the operator API path
        if (path == null || !IsOperatorApiPath(path))
        {
            await _next(context);
            return;
        }

        // Extract API key from header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues) ||
            string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
        {
            _logger.LogWarning("Operator API request without X-API-Key header from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized,
                "KLC:Operator:InvalidApiKey", "Missing or empty X-API-Key header.");
            return;
        }

        var apiKey = apiKeyValues.First()!;
        var apiKeyHash = HashApiKey(apiKey);

        // Look up operator by API key hash
        using var scope = context.RequestServices.CreateScope();
        var operatorRepository = scope.ServiceProvider.GetRequiredService<IRepository<Operator, Guid>>();
        var queryable = await operatorRepository.GetQueryableAsync();

        var op = await operatorRepository.AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(o => o.ApiKeyHash == apiKeyHash));

        if (op == null)
        {
            _logger.LogWarning("Operator API request with invalid API key from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized,
                "KLC:Operator:InvalidApiKey", "Invalid API key.");
            return;
        }

        if (!op.IsActive)
        {
            _logger.LogWarning("Operator API request from inactive operator {OperatorId} ({OperatorName})",
                op.Id, op.Name);
            await WriteErrorResponse(context, StatusCodes.Status403Forbidden,
                "KLC:Operator:NotActive", "Operator account is not active.");
            return;
        }

        // Set operator ID in HttpContext for controller access
        context.Items["OperatorId"] = op.Id;

        _logger.LogDebug("Operator API authenticated: {OperatorId} ({OperatorName})",
            op.Id, op.Name);

        await _next(context);
    }

    private static bool IsOperatorApiPath(string path)
    {
        // Match /api/v1/operator/ and /api/v1/operator (exact)
        // But NOT /api/v1/operators (plural — that's the admin CRUD endpoint)
        return path.StartsWith(OperatorApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
               (path.Length == OperatorApiPathPrefix.Length ||
                path[OperatorApiPathPrefix.Length] == '/');
    }

    private static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
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
