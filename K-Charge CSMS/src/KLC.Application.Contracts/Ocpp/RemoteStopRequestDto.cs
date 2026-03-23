using System.ComponentModel.DataAnnotations;

namespace KLC.Ocpp;

public class RemoteStopRequestDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    public int TransactionId { get; set; }
}
