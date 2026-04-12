using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;
using Volo.Abp.Application.Dtos;

namespace KLC.Stations;

/// <summary>
/// DTO for connector details.
/// </summary>
public class ConnectorDto : FullAuditedEntityDto<Guid>
{
    public Guid StationId { get; set; }
    public int ConnectorNumber { get; set; }
    public ConnectorType ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
    public ConnectorStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public string? QrCodeData { get; set; }
}

/// <summary>
/// DTO for connector list items.
/// </summary>
public class ConnectorListDto : EntityDto<Guid>
{
    public int ConnectorNumber { get; set; }
    public ConnectorType ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
    public ConnectorStatus Status { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// DTO for setting QR code data on a connector.
/// </summary>
public class SetConnectorQrCodeDto
{
    /// <summary>
    /// QR code text to set on the connector (e.g., "251401000004-01").
    /// Set to null to clear.
    /// </summary>
    [StringLength(500)]
    public string? QrCodeData { get; set; }

    /// <summary>
    /// Whether to also send the QR code to the charger via OCPP DataTransfer.
    /// </summary>
    public bool SendToCharger { get; set; }
}
