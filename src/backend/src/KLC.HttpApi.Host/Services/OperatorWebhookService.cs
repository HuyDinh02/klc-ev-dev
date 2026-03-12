using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
/// Fire-and-forget with logging — failures never break the main flow.
/// </summary>
public class OperatorWebhookService : IOperatorWebhookService
{
    private readonly IRepository<Operator, Guid> _operatorRepository;
    private readonly IRepository<OperatorWebhookLog, Guid> _webhookLogRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<OperatorWebhookService> _logger;

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
            // Find all active operators with a webhook URL
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

            // Deliver to each operator in parallel (fire-and-forget per operator)
            var tasks = operators.Select(op => DeliverWebhookAsync(op, eventType, stationId, payload));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Top-level catch — webhook failures must never break the main flow
            _logger.LogError(ex, "Unexpected error in EnqueueWebhookAsync for event {EventType}, station {StationId}",
                eventType, stationId);
        }
    }

    private async Task<List<Operator>> GetTargetOperatorsAsync(Guid? stationId)
    {
        var queryable = await _operatorRepository.GetQueryableAsync();

        // Filter: active operators with a webhook URL configured
        var query = queryable.Where(o => o.IsActive && o.WebhookUrl != null && o.WebhookUrl != "");

        if (stationId.HasValue)
        {
            // Only operators that have access to this specific station
            query = query.Where(o =>
                o.AllowedStations.Any(s => s.StationId == stationId.Value && !s.IsDeleted));
        }

        return query.ToList();
    }

    private async Task DeliverWebhookAsync(
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

        try
        {
            var client = _httpClientFactory.CreateClient("OperatorWebhook");
            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(op.WebhookUrl, content);
            httpStatusCode = (int)response.StatusCode;
            success = response.IsSuccessStatusCode;

            if (!success)
            {
                errorMessage = $"HTTP {httpStatusCode}: {response.ReasonPhrase}";
                _logger.LogWarning(
                    "Webhook delivery failed for operator {OperatorId} ({OperatorName}): {Error}",
                    op.Id, op.Name, errorMessage);
            }
            else
            {
                _logger.LogInformation(
                    "Webhook delivered to operator {OperatorId} ({OperatorName}): {EventType}",
                    op.Id, op.Name, eventType);
            }
        }
        catch (TaskCanceledException)
        {
            errorMessage = "Request timed out (10s)";
            _logger.LogWarning(
                "Webhook delivery timed out for operator {OperatorId} ({OperatorName})",
                op.Id, op.Name);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = $"HTTP error: {ex.Message}";
            _logger.LogWarning(ex,
                "Webhook delivery HTTP error for operator {OperatorId} ({OperatorName})",
                op.Id, op.Name);
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex,
                "Unexpected webhook delivery error for operator {OperatorId} ({OperatorName})",
                op.Id, op.Name);
        }

        // Log the delivery attempt
        try
        {
            var log = new OperatorWebhookLog(
                _guidGenerator.Create(),
                op.Id,
                eventType,
                payloadJson,
                httpStatusCode,
                success,
                errorMessage);

            await _webhookLogRepository.InsertAsync(log, autoSave: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist webhook log for operator {OperatorId}",
                op.Id);
        }
    }
}
