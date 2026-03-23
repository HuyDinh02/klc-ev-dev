using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace KLC.ChargingSessions;

public class ChargingSessionDto : AuditedEntityDto<Guid>
{
    public int TransactionId { get; set; }
    public string ChargePointId { get; set; }
    public int ConnectorId { get; set; }
    public string IdTag { get; set; }
    public string Status { get; set; }
    public DateTime StartTimestamp { get; set; }
    public DateTime? StopTimestamp { get; set; }
    public int MeterStart { get; set; }
    public int? MeterStop { get; set; }
    public string ReservationId { get; set; }
    public string StopReason { get; set; }
    public List<MeterValueDto> MeterValues { get; set; } = new();

    /// <summary>
    /// Computed property: Energy consumed in Wh (MeterStop - MeterStart)
    /// </summary>
    public int? EnergyConsumedWh
    {
        get
        {
            if (MeterStop.HasValue)
            {
                return MeterStop.Value - MeterStart;
            }
            return null;
        }
    }

    /// <summary>
    /// Computed property: Duration of the session
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (StopTimestamp.HasValue)
            {
                return StopTimestamp.Value - StartTimestamp;
            }
            return null;
        }
    }
}
