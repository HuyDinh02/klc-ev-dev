using System.Text.Json.Serialization;

namespace KCharge.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J BootNotification.req - Sent by Charge Point when it boots.
/// </summary>
public class BootNotificationRequest
{
    /// <summary>
    /// Vendor of the Charge Point.
    /// </summary>
    [JsonPropertyName("chargePointVendor")]
    public string ChargePointVendor { get; set; } = string.Empty;

    /// <summary>
    /// Model of the Charge Point.
    /// </summary>
    [JsonPropertyName("chargePointModel")]
    public string ChargePointModel { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the Charge Point.
    /// </summary>
    [JsonPropertyName("chargePointSerialNumber")]
    public string? ChargePointSerialNumber { get; set; }

    /// <summary>
    /// Serial number of the Charge Box.
    /// </summary>
    [JsonPropertyName("chargeBoxSerialNumber")]
    public string? ChargeBoxSerialNumber { get; set; }

    /// <summary>
    /// Firmware version of the Charge Point.
    /// </summary>
    [JsonPropertyName("firmwareVersion")]
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// ICCID of the SIM card.
    /// </summary>
    [JsonPropertyName("iccid")]
    public string? Iccid { get; set; }

    /// <summary>
    /// IMSI of the SIM card.
    /// </summary>
    [JsonPropertyName("imsi")]
    public string? Imsi { get; set; }

    /// <summary>
    /// Meter type.
    /// </summary>
    [JsonPropertyName("meterType")]
    public string? MeterType { get; set; }

    /// <summary>
    /// Meter serial number.
    /// </summary>
    [JsonPropertyName("meterSerialNumber")]
    public string? MeterSerialNumber { get; set; }
}

/// <summary>
/// OCPP 1.6J BootNotification.conf - Response to BootNotification.
/// </summary>
public class BootNotificationResponse
{
    /// <summary>
    /// Registration status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Accepted";

    /// <summary>
    /// Current time of Central System.
    /// </summary>
    [JsonPropertyName("currentTime")]
    public string CurrentTime { get; set; } = string.Empty;

    /// <summary>
    /// Heartbeat interval in seconds.
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 300;
}

/// <summary>
/// Registration status values.
/// </summary>
public static class RegistrationStatus
{
    public const string Accepted = "Accepted";
    public const string Pending = "Pending";
    public const string Rejected = "Rejected";
}
