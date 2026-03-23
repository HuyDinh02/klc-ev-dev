using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace KLC.ChargingSessions;

public class MeterValue : Entity<Guid>
{
    public Guid ChargingSessionId { get; set; }
    public DateTime Timestamp { get; private set; }
    public string Value { get; private set; }
    public string Measurand { get; private set; }
    public string Unit { get; private set; }
    public string Context { get; private set; }
    public string Format { get; private set; }
    public string? Location { get; private set; }
    public string? Phase { get; private set; }

    protected MeterValue()
    {
    }

    public MeterValue(
        Guid id,
        DateTime timestamp,
        string value,
        string measurand,
        string unit,
        string context,
        string format = "Raw",
        string? location = null,
        string? phase = null) : base(id)
    {
        Timestamp = timestamp;
        Value = Check.NotNullOrWhiteSpace(value, nameof(value));
        Measurand = Check.NotNullOrWhiteSpace(measurand, nameof(measurand));
        Unit = Check.NotNullOrWhiteSpace(unit, nameof(unit));
        Context = Check.NotNullOrWhiteSpace(context, nameof(context));
        Format = Check.NotNullOrWhiteSpace(format, nameof(format));
        Location = location;
        Phase = phase;
    }
}
