using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Operators;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace KLC.Services;

/// <summary>
/// Delivers webhook notifications to external operators when key events occur.
/// Includes HMAC-SHA256 signature and retry with exponential backoff.
/// Fire-and-forget with logging — failures never break the main flow.
/// </summary>
public class OperatorWebhookService : IOperatorWebhookService
{
    private readonly IRepository<Operator, Guid> _operatorRepository;
    private readonly IRepository<OperatorWebhookLog, Guid> _webhookLogRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<OperatorWebhookService> _logger;

    private const int MaxRetries = 3;
    private static readonly int[] RetryDelaysMs = [0, 2000, 5000];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OperatorWebhookService(
        IRepository<Operator, Guid> operatorRepository,
        IRepository<OperatorWebhookLog, Guid> webhookLogRepository,
        IHttpClientFactory httpClientFactory,
        IGuidGenerator guidGenerator,
        ILogger<OperatorWebhookService> logger)
    {
        _operatorRepository = operatorRepository;
        _webhookLogRepository = webhookLogRepository;
        _httpClientFactory = httpClientFactory;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    public async Task EnqueueWebhookAsync(WebhookEventType eventType, Guid? stationId, object payload)
    {
        try
        {
            var operators = await GetTargetOperatorsAsync(stationId);

            if (operators.Count == 0)
            {
                _logger.LogDebug("No operators to notify for event {EventType} on station {StationId}",
                    eventType, stationId);
                return;
            }

            _logger.LogInformation(
                "Delivering webhook {EventType} to {OperatorCount} operator(s) for station {StationId}",
                eventType, operators.Count, stationId);

            var tasks = operators.Select(op => DeliverWithRetryAsync(op, eventType, stationId, payload));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in EnqueueWebhookAsync for event {EventType}, station {StationId}",
                eventType, stationId);
        }
    }

    private async Task<List<Operator>> GetTargetOperatorsAsync(Guid? stationId)
    {
        var queryable = await _operatorRepository.GetQueryableAsync();
        var query = queryable.Where(o => o.IsActive && o.WebhookUrl != null && o.WebhookUrl != "");

        if (stationId.HasValue)
        {
            query = query.Where(o =>
                o.AllowedStations.Any(s => s.StationId == stationId.Value && !s.IsDeleted));
        }

        return query.ToList();
    }

    private async Task DeliverWithRetryAsync(
        Operator op,
        WebhookEventType eventType,
        Guid? stationId,
        object payload)
    {
        var webhookPayload = new
        {
            eventType = eventType.ToString(),
            stationId,
            timestamp = DateTime.UtcNow,
            data = payload
        };

        var payloadJson = JsonSerializer.Serialize(webhookPayload, JsonOptions);
        int? httpStatusCode = null;
        bool success = false;
        string? errorMessage = null;
        int attempt = 0;

        for (attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 1)
            {
                await Task.Delay(RetryDelaysMs[attempt - 1]);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("OperatorWebhook");
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                // HMAC-SHA256 signature using operator's API key hash as secret
                var signature = ComputeHmacSignature(payloadJson, op.ApiKeyHash);
                content.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                content.Headers.Add("X-Webhook-Event", eventType.ToString());

                var response = await client.PostAsync(op.WebhookUrl, content);
                httpStatusCode = (int)response.StatusCode;
                success = response.IsSuccessStatusCode;

                if (success)
                {
                    _logger.LogInformation(
                        "Webhook delivered to operator {OperatorId} ({OperatorName}): {EventType} (attempt {Attempt})",
                        op.Id, op.Name, eventType, attempt);
                    break;
                }

                errorMessage = $"HTTP {httpStatusCode}: {response.ReasonPhrase}";

                // Don't retry on 4xx client errors (except 429)
                if (httpStatusCode >= 400 && httpStatusCode < 500 && httpStatusCode != 429)
                {
                    _logger.LogWarning(
                        "Webhook delivery failed (no retry) for operator {OperatorId}: {Error}",
                        op.Id, errorMessage);
                    break;
                }

                _logger.LogWarning(
                    "Webhook delivery failed for operator {OperatorId} (attempt {Attempt}/{MaxRetries}): {Error}",
                    op.Id, attempt, MaxRetries, errorMessage);
            }
            catch (TaskCanceledException)
            {
                errorMessage = "Request timed out (10s)";
                _logger.LogWarning(
                    "Webhook timed out for operator {OperatorId} (attempt {Attempt}/{MaxRetries})",
                    op.Id, attempt, MaxRetries);
            }
            catch (HttpRequestException ex)
            {
                errorMessage = $"HTTP error: {ex.Message}";
                _logger.LogWarning(ex,
                    "Webhook HTTP error for operator {OperatorId} (attempt {Attempt}/{MaxRetries})",
                    op.Id, attempt, MaxRetries);
            }
            catch (Exception ex)
            {
                errorMessage = $"Unexpected error: {ex.Message}";
                _logger.LogError(ex,
                    "Unexpected webhook error for operator {OperatorId} (attempt {Attempt})",
                    op.Id, attempt);
                break; // Don't retry unexpected errors
            }
        }

        // Persist audit log
        try
        {
            var log = new OperatorWebhookLog(
                _guidGenerator.Create(),
                op.Id,
                eventType,
                payloadJson,
                httpStatusCode,
                success,
                errorMessage,
                attempt);

            await _webhookLogRepository.InsertAsync(log, autoSave: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist webhook log for operator {OperatorId}", op.Id);
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
