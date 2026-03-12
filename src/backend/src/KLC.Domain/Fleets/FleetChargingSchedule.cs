using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Fleets;

/// <summary>
/// Defines a time window during which fleet vehicles are permitted to charge (for ScheduledOnly policy).
/// </summary>
public class FleetChargingSchedule : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the parent fleet.
    /// </summary>
    public Guid FleetId { get; private set; }

    /// <summary>
    /// Day of the week (0 = Sunday, 6 = Saturday).
    /// </summary>
    public int DayOfWeek { get; private set; }

    /// <summary>
    /// Start time of the charging window (UTC).
    /// </summary>
    public TimeSpan StartTimeUtc { get; private set; }

    /// <summary>
    /// End time of the charging window (UTC).
    /// </summary>
    public TimeSpan EndTimeUtc { get; private set; }

    protected FleetChargingSchedule()
    {
        // Required by EF Core
    }

    public FleetChargingSchedule(
        Guid id,
        Guid fleetId,
        int dayOfWeek,
        TimeSpan startTimeUtc,
        TimeSpan endTimeUtc)
        : base(id)
    {
        FleetId = fleetId;

        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new BusinessException("KLC:Fleet:InvalidDayOfWeek");

        if (startTimeUtc >= endTimeUtc)
            throw new BusinessException("KLC:Fleet:InvalidScheduleTime");

        DayOfWeek = dayOfWeek;
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
    }
}
