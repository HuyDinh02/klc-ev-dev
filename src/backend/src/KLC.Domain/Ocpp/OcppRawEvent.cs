using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities;

namespace KLC.Ocpp;

/// <summary>
/// Stores raw OCPP messages for audit, debugging, and replay.
/// JSONB payload column for vendor-agnostic storage.
/// </summary>
public class OcppRawEvent : Entity<Guid>
{
    /// <summary>
    /// Charge point identifier (e.g., "KC-HN-001").
    /// </summary>
    public string ChargePointId { get; private set; } = string.Empty;

    /// <summary>
    /// OCPP action name (e.g., "MeterValues", "StartTransaction").
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// OCPP message unique ID for correlation.
    /// </summary>
    public string UniqueId { get; private set; } = string.Empty;

    /// <summary>
    /// OCPP message type: 2 = Call, 3 = CallResult, 4 = CallError.
    /// </summary>
    public int MessageType { get; private set; }

    /// <summary>
    /// Raw JSON payload stored as JSONB.
    /// </summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// Processing latency in milliseconds.
    /// </summary>
    public long? LatencyMs { get; private set; }

    /// <summary>
    /// Vendor profile type active when message was processed.
    /// </summary>
    public VendorProfileType VendorProfile { get; private set; }

    /// <summary>
    /// When the event was received (UTC).
    /// </summary>
    public DateTime ReceivedAt { get; private set; }

    protected OcppRawEvent()
    {
        // Required by EF Core
    }

    public OcppRawEvent(
        Guid id,
        string chargePointId,
        string action,
        string uniqueId,
        int messageType,
        string payload,
        VendorProfileType vendorProfile,
        long? latencyMs = null)
        : base(id)
    {
        ChargePointId = chargePointId;
        Action = action;
        UniqueId = uniqueId;
        MessageType = messageType;
        Payload = payload;
        VendorProfile = vendorProfile;
        LatencyMs = latencyMs;
        ReceivedAt = DateTime.UtcNow;
    }
}
