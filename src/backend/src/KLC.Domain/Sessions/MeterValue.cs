using System;
using Volo.Abp.Domain.Entities;

namespace KLC.Sessions;

/// <summary>
/// Represents a meter value reading during a charging session.
/// Captures energy, current, voltage, power, and SoC data from OCPP MeterValues.
/// </summary>
public class MeterValue : Entity<Guid>
{
    /// <summary>
    /// Reference to the charging session.
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Connector number on the station.
    /// </summary>
    public int ConnectorNumber { get; private set; }

    /// <summary>
    /// Timestamp when the meter value was recorded.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Energy consumed in kWh (Energy.Active.Import.Register).
    /// </summary>
    public decimal EnergyKwh { get; private set; }

    /// <summary>
    /// Current in Amps (Current.Import).
    /// </summary>
    public decimal? CurrentAmps { get; private set; }

    /// <summary>
    /// Voltage in Volts (Voltage).
    /// </summary>
    public decimal? VoltageVolts { get; private set; }

    /// <summary>
    /// Active power in kW (Power.Active.Import).
    /// </summary>
    public decimal? PowerKw { get; private set; }

    /// <summary>
    /// State of Charge percentage (SoC).
    /// </summary>
    public decimal? SocPercent { get; private set; }

    protected MeterValue()
    {
        // Required by EF Core
    }

    internal MeterValue(
        Guid id,
        Guid sessionId,
        Guid stationId,
        int connectorNumber,
        decimal energyKwh,
        DateTime timestamp,
        decimal? currentAmps = null,
        decimal? voltageVolts = null,
        decimal? powerKw = null,
        decimal? socPercent = null)
        : base(id)
    {
        SessionId = sessionId;
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        Timestamp = timestamp;
        EnergyKwh = energyKwh;
        CurrentAmps = currentAmps;
        VoltageVolts = voltageVolts;
        PowerKw = powerKw;
        SocPercent = socPercent;
    }
}
