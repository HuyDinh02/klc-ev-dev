using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Fleets;

/// <summary>
/// Links a vehicle to a fleet with driver assignment and energy tracking.
/// </summary>
public class FleetVehicle : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the parent fleet.
    /// </summary>
    public Guid FleetId { get; private set; }

    /// <summary>
    /// Reference to the vehicle.
    /// </summary>
    public Guid VehicleId { get; private set; }

    /// <summary>
    /// Reference to the driver assigned to this vehicle (optional).
    /// </summary>
    public Guid? DriverUserId { get; private set; }

    /// <summary>
    /// Maximum daily charging limit in kWh (null = unlimited).
    /// </summary>
    public decimal? DailyChargingLimitKwh { get; private set; }

    /// <summary>
    /// Energy consumed today (kWh). Reset daily.
    /// </summary>
    public decimal CurrentDayEnergyKwh { get; private set; }

    /// <summary>
    /// Energy consumed this month (kWh). Reset monthly.
    /// </summary>
    public decimal CurrentMonthEnergyKwh { get; private set; }

    /// <summary>
    /// Whether this fleet vehicle assignment is active.
    /// </summary>
    public bool IsActive { get; private set; }

    protected FleetVehicle()
    {
        // Required by EF Core
    }

    internal FleetVehicle(
        Guid id,
        Guid fleetId,
        Guid vehicleId,
        Guid? driverUserId = null,
        decimal? dailyChargingLimitKwh = null)
        : base(id)
    {
        FleetId = fleetId;
        VehicleId = vehicleId;
        DriverUserId = driverUserId;
        DailyChargingLimitKwh = dailyChargingLimitKwh;
        CurrentDayEnergyKwh = 0;
        CurrentMonthEnergyKwh = 0;
        IsActive = true;
    }

    public void AssignDriver(Guid? userId)
    {
        DriverUserId = userId;
    }

    public void SetDailyLimit(decimal? kwh)
    {
        DailyChargingLimitKwh = kwh;
    }

    public void RecordEnergy(decimal kwh)
    {
        CurrentDayEnergyKwh += kwh;
        CurrentMonthEnergyKwh += kwh;
    }

    public void ResetDailyEnergy()
    {
        CurrentDayEnergyKwh = 0;
    }

    public void ResetMonthlyEnergy()
    {
        CurrentMonthEnergyKwh = 0;
    }

    public bool IsDailyLimitExceeded()
    {
        return DailyChargingLimitKwh.HasValue && CurrentDayEnergyKwh >= DailyChargingLimitKwh.Value;
    }

    internal void SoftDelete()
    {
        IsActive = false;
        IsDeleted = true;
        DeletionTime = DateTime.UtcNow;
    }
}
