using System;
using System.Collections.Generic;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Stations;

/// <summary>
/// Represents a physical EV charging station.
/// Aggregate root for the Stations bounded context.
/// </summary>
public class ChargingStation : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Unique station code (e.g., "KC-HN-001").
    /// </summary>
    public string StationCode { get; private set; } = string.Empty;

    /// <summary>
    /// Display name of the station.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Full address of the station.
    /// </summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public double Latitude { get; private set; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public double Longitude { get; private set; }

    /// <summary>
    /// Current operational status.
    /// </summary>
    public StationStatus Status { get; private set; }

    /// <summary>
    /// Current firmware version installed on the station.
    /// </summary>
    public string? FirmwareVersion { get; private set; }

    /// <summary>
    /// Station model/manufacturer info.
    /// </summary>
    public string? Model { get; private set; }

    /// <summary>
    /// Station vendor name.
    /// </summary>
    public string? Vendor { get; private set; }

    /// <summary>
    /// Serial number of the station.
    /// </summary>
    public string? SerialNumber { get; private set; }

    /// <summary>
    /// Reference to the station group this station belongs to.
    /// </summary>
    public Guid? StationGroupId { get; private set; }

    /// <summary>
    /// Reference to the default tariff plan for this station.
    /// </summary>
    public Guid? TariffPlanId { get; private set; }

    /// <summary>
    /// Last heartbeat received from the station (OCPP).
    /// </summary>
    public DateTime? LastHeartbeat { get; private set; }

    /// <summary>
    /// Whether the station is enabled for use.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Collection of connectors belonging to this station.
    /// </summary>
    public ICollection<Connector> Connectors { get; private set; } = new List<Connector>();

    protected ChargingStation()
    {
        // Required by EF Core
    }

    public ChargingStation(
        Guid id,
        string stationCode,
        string name,
        string address,
        double latitude,
        double longitude,
        Guid? stationGroupId = null,
        Guid? tariffPlanId = null)
        : base(id)
    {
        SetStationCode(stationCode);
        SetName(name);
        SetAddress(address);
        SetLocation(latitude, longitude);
        StationGroupId = stationGroupId;
        TariffPlanId = tariffPlanId;
        Status = StationStatus.Offline;
        IsEnabled = true;
    }

    public void SetStationCode(string stationCode)
    {
        StationCode = Check.NotNullOrWhiteSpace(stationCode, nameof(stationCode), maxLength: 50);
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetAddress(string address)
    {
        Address = Check.NotNullOrWhiteSpace(address, nameof(address), maxLength: 500);
    }

    public void SetLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new BusinessException(KLCDomainErrorCodes.Station.InvalidLatitude);
        if (longitude < -180 || longitude > 180)
            throw new BusinessException(KLCDomainErrorCodes.Station.InvalidLongitude);

        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetStationInfo(string? vendor, string? model, string? serialNumber, string? firmwareVersion)
    {
        Vendor = vendor;
        Model = model;
        SerialNumber = serialNumber;
        FirmwareVersion = firmwareVersion;
    }

    public void SetTariffPlan(Guid? tariffPlanId)
    {
        TariffPlanId = tariffPlanId;
    }

    public void SetStationGroup(Guid? stationGroupId)
    {
        StationGroupId = stationGroupId;
    }

    public void UpdateStatus(StationStatus newStatus)
    {
        Status = newStatus;
    }

    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        if (Status == StationStatus.Offline)
        {
            Status = StationStatus.Available;
        }
    }

    public void MarkOffline()
    {
        Status = StationStatus.Offline;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
        Status = StationStatus.Unavailable;
    }

    public Connector AddConnector(
        Guid connectorId,
        int connectorNumber,
        ConnectorType connectorType,
        decimal maxPowerKw)
    {
        var connector = new Connector(connectorId, Id, connectorNumber, connectorType, maxPowerKw);
        Connectors.Add(connector);
        return connector;
    }
}
