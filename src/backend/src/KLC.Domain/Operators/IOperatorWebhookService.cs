using System;
using System.Threading.Tasks;
using KLC.Enums;

namespace KLC.Operators;

/// <summary>
/// Service for delivering webhook notifications to external operators
/// when key events occur (session started/completed, fault detected, station offline).
/// </summary>
public interface IOperatorWebhookService
{
    /// <summary>
    /// Enqueue a webhook notification for all operators that have access to the given station.
    /// Fire-and-forget: failures are logged but never break the main flow.
    /// </summary>
    /// <param name="eventType">The type of webhook event.</param>
    /// <param name="stationId">The station ID (null to notify all operators with a webhook URL).</param>
    /// <param name="payload">The event payload data to serialize as JSON.</param>
    Task EnqueueWebhookAsync(WebhookEventType eventType, Guid? stationId, object payload);
}
