using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class TriggerMessageRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequestedMessage { get; set; } // e.g., "Heartbeat", "BootNotification", "MeterValues", etc.

    public int? ConnectorId { get; set; }
}
