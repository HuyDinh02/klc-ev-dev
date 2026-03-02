using System;
using System.Collections.Generic;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Sessions;

/// <summary>
/// Represents a charging session from start to end.
/// Aggregate root for the Sessions bounded context.
/// </summary>
public class ChargingSession : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Reference to the user who initiated the session.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Reference to the vehicle being charged (optional).
    /// </summary>
    public Guid? VehicleId { get; private set; }

    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Connector number on the station.
    /// </summary>
    public int ConnectorNumber { get; private set; }

    /// <summary>
    /// OCPP transaction ID assigned by the charger.
    /// </summary>
    public int? OcppTransactionId { get; private set; }

    /// <summary>
    /// Current session status.
    /// </summary>
    public SessionStatus Status { get; private set; }

    /// <summary>
    /// When charging started (StartTransaction received).
    /// </summary>
    public DateTime? StartTime { get; private set; }

    /// <summary>
    /// When charging ended (StopTransaction received).
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Meter value at start of charging (Wh).
    /// </summary>
    public int? MeterStart { get; private set; }

    /// <summary>
    /// Meter value at end of charging (Wh).
    /// </summary>
    public int? MeterStop { get; private set; }

    /// <summary>
    /// Total energy consumed in kWh.
    /// </summary>
    public decimal TotalEnergyKwh { get; private set; }

    /// <summary>
    /// Total cost of the session in VND.
    /// </summary>
    public decimal TotalCost { get; private set; }

    /// <summary>
    /// Reference to the tariff plan used for billing.
    /// </summary>
    public Guid? TariffPlanId { get; private set; }

    /// <summary>
    /// Rate per kWh at time of session (snapshot).
    /// </summary>
    public decimal RatePerKwh { get; private set; }

    /// <summary>
    /// Reason for stopping (from OCPP StopTransaction).
    /// </summary>
    public string? StopReason { get; private set; }

    /// <summary>
    /// OCPP IdTag used to authorize the session.
    /// </summary>
    public string? IdTag { get; private set; }

    /// <summary>
    /// Collection of meter values recorded during the session.
    /// </summary>
    public ICollection<MeterValue> MeterValues { get; private set; } = new List<MeterValue>();

    protected ChargingSession()
    {
        // Required by EF Core
    }

    public ChargingSession(
        Guid id,
        Guid userId,
        Guid stationId,
        int connectorNumber,
        Guid? vehicleId = null,
        Guid? tariffPlanId = null,
        decimal ratePerKwh = 0,
        string? idTag = null)
        : base(id)
    {
        UserId = userId;
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        VehicleId = vehicleId;
        TariffPlanId = tariffPlanId;
        RatePerKwh = ratePerKwh;
        IdTag = idTag;
        Status = SessionStatus.Pending;
        TotalEnergyKwh = 0;
        TotalCost = 0;
    }

    public void MarkStarting()
    {
        if (Status != SessionStatus.Pending)
            throw new InvalidOperationException("Session must be in Pending status to start");
        Status = SessionStatus.Starting;
    }

    public void RecordStart(int ocppTransactionId, int meterStart)
    {
        OcppTransactionId = ocppTransactionId;
        MeterStart = meterStart;
        StartTime = DateTime.UtcNow;
        Status = SessionStatus.InProgress;
    }

    public void Suspend()
    {
        if (Status != SessionStatus.InProgress)
            throw new InvalidOperationException("Session must be in progress to suspend");
        Status = SessionStatus.Suspended;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Suspended)
            throw new InvalidOperationException("Session must be suspended to resume");
        Status = SessionStatus.InProgress;
    }

    public void MarkStopping()
    {
        if (Status != SessionStatus.InProgress && Status != SessionStatus.Suspended)
            throw new InvalidOperationException("Session must be in progress or suspended to stop");
        Status = SessionStatus.Stopping;
    }

    public void RecordStop(int meterStop, string? stopReason = null)
    {
        MeterStop = meterStop;
        EndTime = DateTime.UtcNow;
        StopReason = stopReason;

        // Calculate total energy
        if (MeterStart.HasValue)
        {
            var energyWh = meterStop - MeterStart.Value;
            TotalEnergyKwh = Math.Round(energyWh / 1000m, 3);
        }

        // Calculate total cost
        TotalCost = Math.Round(TotalEnergyKwh * RatePerKwh, 0);

        Status = SessionStatus.Completed;
    }

    public void MarkFailed(string? reason = null)
    {
        EndTime = DateTime.UtcNow;
        StopReason = reason ?? "Session failed";
        Status = SessionStatus.Failed;
    }

    public MeterValue AddMeterValue(
        Guid meterValueId,
        decimal energyKwh,
        decimal? currentAmps,
        decimal? voltageVolts,
        decimal? powerKw,
        decimal? socPercent)
    {
        var meterValue = new MeterValue(
            meterValueId,
            Id,
            StationId,
            ConnectorNumber,
            energyKwh,
            currentAmps,
            voltageVolts,
            powerKw,
            socPercent);
        MeterValues.Add(meterValue);
        return meterValue;
    }

    public void UpdateTotalEnergy(decimal totalEnergyKwh)
    {
        TotalEnergyKwh = totalEnergyKwh;
        TotalCost = Math.Round(TotalEnergyKwh * RatePerKwh, 0);
    }
}
