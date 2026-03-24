using System;
using System.Collections.Generic;
using KLC.Enums;
using NetTopologySuite.Geometries;
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
    /// PostGIS geography point for spatial queries (SRID 4326 = WGS84).
    /// </summary>
    public Point? Location { get; private set; }

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
    /// Vendor profile type detected from BootNotification or set by admin.
    /// </summary>
    public VendorProfileType VendorProfile { get; private set; } = VendorProfileType.Generic;

    /// <summary>
    /// OCPP WebSocket authentication password. Null = no auth required.
    /// Used in HTTP Basic Auth where username = StationCode, password = this value.
    /// </summary>
    public string? OcppPassword { get; private set; }

    /// <summary>
    /// Current firmware update status reported by the charger (e.g., Downloading, Downloaded, Installing, Installed, InstallationFailed, Idle).
    /// </summary>
    public string? FirmwareUpdateStatus { get; private set; }

    /// <summary>
    /// Current diagnostics upload status reported by the charger (e.g., Uploading, Uploaded, UploadFailed, Idle).
    /// </summary>
    public string? DiagnosticsStatus { get; private set; }

    /// <summary>
    /// Collection of connectors belonging to this station.
    /// </summary>
    public ICollection<Connector> Connectors { get; private set; } = new List<Connector>();

    /// <summary>
    /// Collection of amenities at this station.
    /// </summary>
    public ICollection<StationAmenity> Amenities { get; private set; } = new List<StationAmenity>();

    /// <summary>
    /// Collection of photos for this station.
    /// </summary>
    public ICollection<StationPhoto> Photos { get; private set; } = new List<StationPhoto>();

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
        Location = new Point(longitude, latitude) { SRID = 4326 };
    }

    public void SetStationInfo(string? vendor, string? model, string? serialNumber, string? firmwareVersion)
    {
        Vendor = vendor;
        Model = model;
        SerialNumber = serialNumber;
        FirmwareVersion = firmwareVersion;
    }

    public void SetVendorProfile(VendorProfileType vendorProfile)
    {
        VendorProfile = vendorProfile;
    }

    public void SetOcppPassword(string? password)
    {
        OcppPassword = password;
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
            Status = StationStatus.Online;
        }
    }

    public void MarkOffline()
    {
        if (Status != StationStatus.Decommissioned)
            Status = StationStatus.Offline;
    }

    public void MarkOnline()
    {
        if (Status == StationStatus.Offline)
            Status = StationStatus.Online;
    }

    public void Enable()
    {
        if (Status == StationStatus.Decommissioned)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.CannotEnableDecommissioned);
        }

        IsEnabled = true;
        if (Status == StationStatus.Disabled)
            Status = StationStatus.Offline; // Will become Online on next BootNotification
    }

    public void Disable()
    {
        IsEnabled = false;
        if (Status != StationStatus.Decommissioned)
        {
            Status = StationStatus.Disabled;
        }
    }

    public void Decommission()
    {
        IsEnabled = false;
        Status = StationStatus.Decommissioned;
    }

    public void UpdateFirmwareStatus(string status)
    {
        FirmwareUpdateStatus = status;
    }

    public void UpdateDiagnosticsStatus(string status)
    {
        DiagnosticsStatus = status;
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

    public StationAmenity AddAmenity(Guid amenityId, AmenityType amenityType)
    {
        var amenity = new StationAmenity(amenityId, Id, amenityType);
        Amenities.Add(amenity);
        return amenity;
    }

    public StationPhoto AddPhoto(Guid photoId, string url, string? thumbnailUrl = null, bool isPrimary = false, int sortOrder = 0)
    {
        var photo = new StationPhoto(photoId, Id, url, thumbnailUrl, isPrimary, sortOrder);
        Photos.Add(photo);
        return photo;
    }
}
