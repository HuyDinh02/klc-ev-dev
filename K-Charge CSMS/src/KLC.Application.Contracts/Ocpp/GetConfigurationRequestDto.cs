using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class GetConfigurationRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    public List<string> Keys { get; set; } = new();
}
