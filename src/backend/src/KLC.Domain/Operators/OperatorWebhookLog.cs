using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Operators;

/// <summary>
/// Audit log for webhook delivery attempts to external operators.
/// </summary>
public class OperatorWebhookLog : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the operator.
    /// </summary>
    public Guid OperatorId { get; private set; }

    /// <summary>
    /// Type of event that triggered the webhook.
    /// </summary>
    public WebhookEventType EventType { get; private set; }

    /// <summary>
    /// JSON payload sent to the webhook URL.
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>
    /// HTTP status code returned by the webhook endpoint.
    /// </summary>
    public int? HttpStatusCode { get; private set; }

    /// <summary>
    /// Whether the webhook delivery was successful.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Error message if the delivery failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of delivery attempts.
    /// </summary>
    public int AttemptCount { get; private set; }

    protected OperatorWebhookLog()
    {
        // Required by EF Core
    }

    public OperatorWebhookLog(
        Guid id,
        Guid operatorId,
        WebhookEventType eventType,
        string payloadJson,
        int? httpStatusCode = null,
        bool success = false,
        string? errorMessage = null,
        int attemptCount = 1)
        : base(id)
    {
        OperatorId = operatorId;
        EventType = eventType;
        PayloadJson = payloadJson;
        HttpStatusCode = httpStatusCode;
        Success = success;
        ErrorMessage = errorMessage;
        AttemptCount = attemptCount;
    }
}
