using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.connector_status_changed")]
public class ConnectorStatusChangedEto
{
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public string Status { get; set; }
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
}
