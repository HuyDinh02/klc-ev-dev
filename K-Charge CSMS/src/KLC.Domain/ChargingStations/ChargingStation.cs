using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using KLC.Ocpp;

namespace KLC.ChargingStations;

public class ChargingStation : FullAuditedAggregateRoot<Guid>
{
    // Identity
    public string ChargePointId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    // Hardware (from BootNotification)
    public string? Vendor { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? FirmwareVersion { get; private set; }
    public string? Iccid { get; private set; }
    public string? Imsi { get; private set; }

    // Location
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? Address { get; private set; }
    public Guid? StationGroupId { get; private set; }

    // Status
    public ChargePointStatus Status { get; private set; }
    public bool IsOnline { get; private set; }
    public DateTime? LastBootTime { get; private set; }
    public DateTime? LastHeartbeat { get; private set; }

    // Navigation
    public ICollection<Connector> Connectors { get; private set; }

    protected ChargingStation()
    {
        Connectors = new List<Connector>();
    }

    public ChargingStation(
        Guid id,
        string chargePointId,
        string name,
        string? description = null,
        Guid? stationGroupId = null,
        double? latitude = null,
        double? longitude = null,
        string? address = null) : base(id)
    {
        ChargePointId = Check.NotNullOrWhiteSpace(chargePointId, nameof(chargePointId));
        Name = Check.NotNullOrWhiteSpace(name, nameof(name));
        Description = description;
        StationGroupId = stationGroupId;
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        Status = ChargePointStatus.Unavailable;
        IsOnline = false;
        Connectors = new List<Connector>();
    }

    public void UpdateBootInfo(
        string vendor,
        string model,
        string? serialNumber = null,
        string? firmwareVersion = null,
        string? iccid = null,
        string? imsi = null)
    {
        Vendor = Check.NotNullOrWhiteSpace(vendor, nameof(vendor));
        Model = Check.NotNullOrWhiteSpace(model, nameof(model));
        SerialNumber = serialNumber;
        FirmwareVersion = firmwareVersion;
        Iccid = iccid;
        Imsi = imsi;
        LastBootTime = DateTime.UtcNow;
    }

    public void SetOnline()
    {
        IsOnline = true;
        LastHeartbeat = DateTime.UtcNow;
    }

    public void SetOffline()
    {
        IsOnline = false;
    }

    public void UpdateStatus(ChargePointStatus status)
    {
        Status = status;
    }

    public void UpdateHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        if (!IsOnline)
        {
            SetOnline();
        }
    }

    public void UpdateLocation(double latitude, double longitude, string? address = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
    }

    public void AddConnector(Connector connector)
    {
        Check.NotNull(connector, nameof(connector));
        Connectors.Add(connector);
    }

    public Connector? GetConnector(int connectorId)
    {
        return Connectors.FirstOrDefault(c => c.ConnectorId == connectorId);
    }

    public void RemoveConnector(int connectorId)
    {
        var connector = GetConnector(connectorId);
        if (connector != null)
        {
            Connectors.Remove(connector);
        }
    }
}
