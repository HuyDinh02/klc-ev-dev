using System;
using Volo.Abp.Application.Dtos;

namespace KLC.ChargingSessions;

public class MeterValueDto : AuditedEntityDto<Guid>
{
    public Guid ChargingSessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Value { get; set; }
    public string Measurand { get; set; }
    public string Unit { get; set; }
    public string Context { get; set; }
}
