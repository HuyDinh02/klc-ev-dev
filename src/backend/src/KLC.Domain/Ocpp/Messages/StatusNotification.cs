using System.Text.Json.Serialization;

namespace KLC.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J StatusNotification.req - Sent by Charge Point to report connector status.
/// </summary>
public class StatusNotificationRequest
{
    /// <summary>
    /// Connector ID. 0 = entire Charge Point, 1+ = specific connector.
    /// </summary>
    [JsonPropertyName("connectorId")]
    public int ConnectorId { get; set; }

    /// <summary>
    /// Error code if status is Faulted.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = "NoError";

    /// <summary>
    /// Additional info about the error.
    /// </summary>
    [JsonPropertyName("info")]
    public string? Info { get; set; }

    /// <summary>
    /// Current status of the connector.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the status notification.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// Vendor-specific error code.
    /// </summary>
    [JsonPropertyName("vendorErrorCode")]
    public string? VendorErrorCode { get; set; }

    /// <summary>
    /// Vendor ID.
    /// </summary>
    [JsonPropertyName("vendorId")]
    public string? VendorId { get; set; }
}

/// <summary>
/// OCPP 1.6J StatusNotification.conf - Response to StatusNotification.
/// </summary>
public class StatusNotificationResponse
{
    // StatusNotification response has no fields
}

/// <summary>
/// OCPP 1.6J ChargePointStatus values.
/// </summary>
public static class ChargePointStatus
{
    public const string Available = "Available";
    public const string Preparing = "Preparing";
    public const string Charging = "Charging";
    public const string SuspendedEVSE = "SuspendedEVSE";
    public const string SuspendedEV = "SuspendedEV";
    public const string Finishing = "Finishing";
    public const string Reserved = "Reserved";
    public const string Unavailable = "Unavailable";
    public const string Faulted = "Faulted";
}

/// <summary>
/// OCPP 1.6J ChargePointErrorCode values.
/// </summary>
public static class ChargePointErrorCode
{
    public const string NoError = "NoError";
    public const string ConnectorLockFailure = "ConnectorLockFailure";
    public const string EVCommunicationError = "EVCommunicationError";
    public const string GroundFailure = "GroundFailure";
    public const string HighTemperature = "HighTemperature";
    public const string InternalError = "InternalError";
    public const string LocalListConflict = "LocalListConflict";
    public const string OtherError = "OtherError";
    public const string OverCurrentFailure = "OverCurrentFailure";
    public const string OverVoltage = "OverVoltage";
    public const string PowerMeterFailure = "PowerMeterFailure";
    public const string PowerSwitchFailure = "PowerSwitchFailure";
    public const string ReaderFailure = "ReaderFailure";
    public const string ResetFailure = "ResetFailure";
    public const string UnderVoltage = "UnderVoltage";
    public const string WeakSignal = "WeakSignal";
}
