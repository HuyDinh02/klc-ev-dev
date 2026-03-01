using System.Text.Json.Serialization;

namespace KCharge.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J MeterValues.req - Sent by Charge Point to report meter readings.
/// </summary>
public class MeterValuesRequest
{
    /// <summary>
    /// Connector ID for the meter values.
    /// </summary>
    [JsonPropertyName("connectorId")]
    public int ConnectorId { get; set; }

    /// <summary>
    /// Transaction ID if meter values are related to a transaction.
    /// </summary>
    [JsonPropertyName("transactionId")]
    public int? TransactionId { get; set; }

    /// <summary>
    /// Collection of meter values.
    /// </summary>
    [JsonPropertyName("meterValue")]
    public MeterValue[] MeterValue { get; set; } = [];
}

/// <summary>
/// OCPP 1.6J MeterValues.conf - Response to MeterValues.
/// </summary>
public class MeterValuesResponse
{
    // MeterValues response has no fields
}

/// <summary>
/// A meter value with timestamp and sampled values.
/// </summary>
public class MeterValue
{
    /// <summary>
    /// Timestamp of the meter value.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Collection of sampled values.
    /// </summary>
    [JsonPropertyName("sampledValue")]
    public SampledValue[] SampledValue { get; set; } = [];
}

/// <summary>
/// A single sampled value.
/// </summary>
public class SampledValue
{
    /// <summary>
    /// The value as a string.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Context of the value (e.g., Sample.Periodic).
    /// </summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    /// <summary>
    /// Format of the value (Raw or SignedData).
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// Measurand (what is being measured).
    /// </summary>
    [JsonPropertyName("measurand")]
    public string? Measurand { get; set; }

    /// <summary>
    /// Phase (L1, L2, L3, L1-N, etc.).
    /// </summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>
    /// Location (Cable, EV, Inlet, Outlet, Body).
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
}

/// <summary>
/// Measurand types.
/// </summary>
public static class Measurand
{
    public const string EnergyActiveExportRegister = "Energy.Active.Export.Register";
    public const string EnergyActiveImportRegister = "Energy.Active.Import.Register";
    public const string EnergyReactiveExportRegister = "Energy.Reactive.Export.Register";
    public const string EnergyReactiveImportRegister = "Energy.Reactive.Import.Register";
    public const string EnergyActiveExportInterval = "Energy.Active.Export.Interval";
    public const string EnergyActiveImportInterval = "Energy.Active.Import.Interval";
    public const string PowerActiveExport = "Power.Active.Export";
    public const string PowerActiveImport = "Power.Active.Import";
    public const string PowerReactiveExport = "Power.Reactive.Export";
    public const string PowerReactiveImport = "Power.Reactive.Import";
    public const string CurrentExport = "Current.Export";
    public const string CurrentImport = "Current.Import";
    public const string Voltage = "Voltage";
    public const string Temperature = "Temperature";
    public const string SoC = "SoC";
    public const string Frequency = "Frequency";
}

/// <summary>
/// Reading context.
/// </summary>
public static class ReadingContext
{
    public const string InterruptionBegin = "Interruption.Begin";
    public const string InterruptionEnd = "Interruption.End";
    public const string SampleClock = "Sample.Clock";
    public const string SamplePeriodic = "Sample.Periodic";
    public const string TransactionBegin = "Transaction.Begin";
    public const string TransactionEnd = "Transaction.End";
    public const string Trigger = "Trigger";
    public const string Other = "Other";
}
