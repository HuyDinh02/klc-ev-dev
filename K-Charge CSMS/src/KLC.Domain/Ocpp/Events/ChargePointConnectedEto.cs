using System;
using Volo.Abp.EventBus;

namespace KLC.Ocpp.Events;

[EventName("klc.charge_point_connected")]
public class ChargePointConnectedEto
{
    public string ChargePointId { get; set; }
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public DateTime Timestamp { get; set; }
}
