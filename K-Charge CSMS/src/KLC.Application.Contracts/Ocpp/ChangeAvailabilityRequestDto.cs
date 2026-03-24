using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class ChangeAvailabilityRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    public int ConnectorId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } // "Inoperative" or "Operative"
}
