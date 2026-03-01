using System.ComponentModel.DataAnnotations;
using KCharge.Enums;

namespace KCharge.Stations;

/// <summary>
/// DTO for updating a connector.
/// </summary>
public class UpdateConnectorDto
{
    [Required]
    public ConnectorType ConnectorType { get; set; }

    [Required]
    [Range(0.1, 1000)]
    public decimal MaxPowerKw { get; set; }
}
