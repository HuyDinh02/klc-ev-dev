using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.charging_session_started")]
public class ChargingSessionStartedEto
{
    public Guid SessionId { get; set; }
    public int TransactionId { get; set; }
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public string IdTag { get; set; }
    public DateTime StartTimestamp { get; set; }
}
