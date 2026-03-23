using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.fault_detected")]
public class FaultDetectedEto
{
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public string ErrorCode { get; set; }
    public string? Info { get; set; }
    public DateTime Timestamp { get; set; }
}
