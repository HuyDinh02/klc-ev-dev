using System.Text.Json.Serialization;

namespace KCharge.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J StopTransaction.req - Sent by Charge Point when a charging session stops.
/// </summary>
public class StopTransactionRequest
{
    /// <summary>
    /// Meter value in Wh at end of transaction.
    /// </summary>
    [JsonPropertyName("meterStop")]
    public int MeterStop { get; set; }

    /// <summary>
    /// Timestamp when the transaction stopped.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID assigned by Central System in StartTransaction.
    /// </summary>
    [JsonPropertyName("transactionId")]
    public int TransactionId { get; set; }

    /// <summary>
    /// Reason for stopping the transaction.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// IdTag if different from the one that started the transaction.
    /// </summary>
    [JsonPropertyName("idTag")]
    public string? IdTag { get; set; }

    /// <summary>
    /// Meter values collected during the transaction.
    /// </summary>
    [JsonPropertyName("transactionData")]
    public MeterValue[]? TransactionData { get; set; }
}

/// <summary>
/// OCPP 1.6J StopTransaction.conf - Response to StopTransaction.
/// </summary>
public class StopTransactionResponse
{
    /// <summary>
    /// Information about the idTag.
    /// </summary>
    [JsonPropertyName("idTagInfo")]
    public IdTagInfo? IdTagInfo { get; set; }
}

/// <summary>
/// Reason for stopping a transaction.
/// </summary>
public static class StopReason
{
    public const string EmergencyStop = "EmergencyStop";
    public const string EVDisconnected = "EVDisconnected";
    public const string HardReset = "HardReset";
    public const string Local = "Local";
    public const string Other = "Other";
    public const string PowerLoss = "PowerLoss";
    public const string Reboot = "Reboot";
    public const string Remote = "Remote";
    public const string SoftReset = "SoftReset";
    public const string UnlockCommand = "UnlockCommand";
    public const string DeAuthorized = "DeAuthorized";
}
