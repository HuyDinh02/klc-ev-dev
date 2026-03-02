using System.Text.Json.Serialization;

namespace KLC.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J StartTransaction.req - Sent by Charge Point when a charging session starts.
/// </summary>
public class StartTransactionRequest
{
    /// <summary>
    /// Connector ID where the transaction started.
    /// </summary>
    [JsonPropertyName("connectorId")]
    public int ConnectorId { get; set; }

    /// <summary>
    /// Identifier used to start the transaction.
    /// </summary>
    [JsonPropertyName("idTag")]
    public string IdTag { get; set; } = string.Empty;

    /// <summary>
    /// Meter value in Wh at start of transaction.
    /// </summary>
    [JsonPropertyName("meterStart")]
    public int MeterStart { get; set; }

    /// <summary>
    /// Timestamp when the transaction started.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Optional reservation ID if this transaction is for a reservation.
    /// </summary>
    [JsonPropertyName("reservationId")]
    public int? ReservationId { get; set; }
}

/// <summary>
/// OCPP 1.6J StartTransaction.conf - Response to StartTransaction.
/// </summary>
public class StartTransactionResponse
{
    /// <summary>
    /// Information about the idTag.
    /// </summary>
    [JsonPropertyName("idTagInfo")]
    public IdTagInfo IdTagInfo { get; set; } = new();

    /// <summary>
    /// Unique transaction ID assigned by Central System.
    /// </summary>
    [JsonPropertyName("transactionId")]
    public int TransactionId { get; set; }
}

/// <summary>
/// Information about an idTag.
/// </summary>
public class IdTagInfo
{
    /// <summary>
    /// Authorization status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Accepted";

    /// <summary>
    /// Expiry date of the idTag.
    /// </summary>
    [JsonPropertyName("expiryDate")]
    public string? ExpiryDate { get; set; }

    /// <summary>
    /// Parent idTag.
    /// </summary>
    [JsonPropertyName("parentIdTag")]
    public string? ParentIdTag { get; set; }
}

/// <summary>
/// Authorization status values.
/// </summary>
public static class AuthorizationStatus
{
    public const string Accepted = "Accepted";
    public const string Blocked = "Blocked";
    public const string Expired = "Expired";
    public const string Invalid = "Invalid";
    public const string ConcurrentTx = "ConcurrentTx";
}
