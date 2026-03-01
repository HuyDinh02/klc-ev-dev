namespace KCharge.Enums;

/// <summary>
/// Represents the type of operational alert.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Station went offline.
    /// </summary>
    StationOffline = 0,

    /// <summary>
    /// Connector fault detected.
    /// </summary>
    ConnectorFault = 1,

    /// <summary>
    /// Low utilization warning.
    /// </summary>
    LowUtilization = 2,

    /// <summary>
    /// High utilization warning.
    /// </summary>
    HighUtilization = 3,

    /// <summary>
    /// Firmware update available.
    /// </summary>
    FirmwareUpdate = 4,

    /// <summary>
    /// Payment processing failure.
    /// </summary>
    PaymentFailure = 5,

    /// <summary>
    /// E-invoice generation failure.
    /// </summary>
    EInvoiceFailure = 6,

    /// <summary>
    /// Heartbeat timeout - station not responding.
    /// </summary>
    HeartbeatTimeout = 7
}
