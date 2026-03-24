using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.charging_session_completed")]
public class ChargingSessionCompletedEto
{
    public Guid SessionId { get; set; }
    public int TransactionId { get; set; }
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public int EnergyConsumedWh { get; set; }
    public TimeSpan Duration { get; set; }
    public string? StopReason { get; set; }
}
