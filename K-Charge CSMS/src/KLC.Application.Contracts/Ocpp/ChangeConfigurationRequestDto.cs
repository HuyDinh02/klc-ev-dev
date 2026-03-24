using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class ChangeConfigurationRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; }

    [Required]
    [MaxLength(500)]
    public string Value { get; set; }
}
