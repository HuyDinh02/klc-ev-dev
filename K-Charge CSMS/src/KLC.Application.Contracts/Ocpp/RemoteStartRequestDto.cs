using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class RemoteStartRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    [MaxLength(50)]
    public string IdTag { get; set; }

    [Required]
    public int ConnectorId { get; set; }

    [MaxLength(100)]
    public string ChargingProfile { get; set; }
}
