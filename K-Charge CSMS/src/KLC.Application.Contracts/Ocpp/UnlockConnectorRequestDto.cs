using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class UnlockConnectorRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    public int ConnectorId { get; set; }
}
