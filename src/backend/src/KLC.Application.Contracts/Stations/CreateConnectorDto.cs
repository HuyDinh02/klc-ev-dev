using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Stations;

/// <summary>
/// DTO for creating a new connector.
/// </summary>
public class CreateConnectorDto
{
    [Required]
    [Range(1, 100)]
    public int ConnectorNumber { get; set; }

    [Required]
    public ConnectorType ConnectorType { get; set; }

    [Required]
    [Range(0.1, 1000)]
    public decimal MaxPowerKw { get; set; }
}
