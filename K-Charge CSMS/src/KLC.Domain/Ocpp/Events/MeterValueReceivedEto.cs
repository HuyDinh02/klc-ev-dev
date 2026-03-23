using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.meter_value_received")]
public class MeterValueReceivedEto
{
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public int TransactionId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? EnergyWh { get; set; }
    public decimal? PowerW { get; set; }
    public decimal? CurrentA { get; set; }
    public decimal? VoltageV { get; set; }
    public decimal? SoCPercent { get; set; }
}
