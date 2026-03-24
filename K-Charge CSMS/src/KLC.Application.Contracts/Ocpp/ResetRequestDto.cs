using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class ResetRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } // "Hard" or "Soft"
}
