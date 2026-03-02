using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Stations;

/// <summary>
/// Represents a charging connector (port) on a charging station.
/// </summary>
public class Connector : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the parent charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// The connector number on the station (1, 2, 3, etc.).
    /// OCPP uses this as the connectorId.
    /// </summary>
    public int ConnectorNumber { get; private set; }

    /// <summary>
    /// Type of connector (Type2, CCS2, CHAdeMO, etc.).
    /// </summary>
    public ConnectorType ConnectorType { get; private set; }

    /// <summary>
    /// Maximum power output in kilowatts.
    /// </summary>
    public decimal MaxPowerKw { get; private set; }

    /// <summary>
    /// Current operational status.
    /// </summary>
    public ConnectorStatus Status { get; private set; }

    /// <summary>
    /// Whether this connector is enabled for use.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Navigation property to parent station.
    /// </summary>
    public ChargingStation? Station { get; private set; }

    protected Connector()
    {
        // Required by EF Core
    }

    internal Connector(
        Guid id,
        Guid stationId,
        int connectorNumber,
        ConnectorType connectorType,
        decimal maxPowerKw)
        : base(id)
    {
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        ConnectorType = connectorType;
        MaxPowerKw = maxPowerKw;
        Status = ConnectorStatus.Unavailable;
        IsEnabled = true;
    }

    public void UpdateStatus(ConnectorStatus newStatus)
    {
        Status = newStatus;
    }

    public void SetMaxPower(decimal maxPowerKw)
    {
        if (maxPowerKw <= 0)
            throw new ArgumentException("Max power must be greater than 0", nameof(maxPowerKw));
        MaxPowerKw = maxPowerKw;
    }

    public void Enable()
    {
        IsEnabled = true;
        if (Status == ConnectorStatus.Unavailable)
        {
            Status = ConnectorStatus.Available;
        }
    }

    public void Disable()
    {
        IsEnabled = false;
        Status = ConnectorStatus.Unavailable;
    }
}
