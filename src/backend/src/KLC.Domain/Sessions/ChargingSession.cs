using System;
using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using KLC.Tariffs;
using Volo.Abp;
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
        StartTime = DateTime.UtcNow;
        TotalEnergyKwh = 0;
        TotalCost = 0;
    }

    public void MarkStarting()
    {
        if (Status != SessionStatus.Pending)
            throw new BusinessException(KLCDomainErrorCodes.Session.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("expectedStatus", nameof(SessionStatus.Pending));
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
            throw new BusinessException(KLCDomainErrorCodes.Session.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("expectedStatus", nameof(SessionStatus.InProgress));
        Status = SessionStatus.Suspended;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Suspended)
            throw new BusinessException(KLCDomainErrorCodes.Session.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("expectedStatus", nameof(SessionStatus.Suspended));
        Status = SessionStatus.InProgress;
    }

    public void MarkStopping()
    {
        if (Status != SessionStatus.InProgress && Status != SessionStatus.Suspended)
            throw new BusinessException(KLCDomainErrorCodes.Session.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("expectedStatus", $"{nameof(SessionStatus.InProgress)}/{nameof(SessionStatus.Suspended)}");
        Status = SessionStatus.Stopping;
    }

    public void RecordStop(int meterStop, string? stopReason = null)
    {
        RecordStop(meterStop, stopReason, tariffPlan: null);
    }

    public void RecordStop(int meterStop, string? stopReason, TariffPlan? tariffPlan)
    {
        MeterStop = meterStop;
        EndTime = DateTime.UtcNow;
        StopReason = stopReason;

        // Calculate total energy from meter readings
        if (MeterStart.HasValue)
        {
            var energyFromMeterWh = meterStop - MeterStart.Value;
            var energyFromMeterKwh = Math.Round(energyFromMeterWh / 1000m, 3);

            // Use the higher of: meter-based calculation vs running total from MeterValues.
            // MeterValues during charging may have tracked a higher total, and some chargers
            // send an unreliable meterStop value.
            if (energyFromMeterKwh > TotalEnergyKwh)
            {
                TotalEnergyKwh = energyFromMeterKwh;
            }
        }

        // Calculate total cost — use TOU if tariff supports it and we have meter values
        if (tariffPlan != null && tariffPlan.TariffType == TariffType.TimeOfUse && MeterValues.Count >= 2)
        {
            var meterData = MeterValues
                .OrderBy(mv => mv.Timestamp)
                .Select(mv => (mv.Timestamp, mv.EnergyKwh))
                .ToList();

            var breakdown = tariffPlan.CalculateTouCost(meterData, MeterStart, MeterStop, StartTime, EndTime);
            TotalCost = breakdown.TotalCost;
        }
        else
        {
            // Flat rate calculation
            TotalCost = Math.Round(TotalEnergyKwh * RatePerKwh, 0);
        }

        Status = SessionStatus.Completed;
    }

    /// <summary>
    /// Mark session as terminated due to an incident (power failure, timeout, etc.)
    /// If energy was delivered (TotalEnergyKwh > 0), the session is COMPLETED
    /// with billing — the user received electricity and must pay for it.
    /// If no energy was delivered, the session is FAILED — nothing to bill.
    /// </summary>
    public void MarkFailed(string? reason = null, TariffPlan? tariffPlan = null)
    {
        EndTime = DateTime.UtcNow;
        StopReason = reason ?? "Session failed";

        if (TotalEnergyKwh > 0)
        {
            // Energy was delivered — complete with billing
            if (tariffPlan != null && tariffPlan.TariffType == TariffType.TimeOfUse && MeterValues.Count >= 2)
            {
                var meterData = MeterValues
                    .OrderBy(mv => mv.Timestamp)
                    .Select(mv => (mv.Timestamp, mv.EnergyKwh))
                    .ToList();
                var breakdown = tariffPlan.CalculateTouCost(meterData, MeterStart, MeterStop, StartTime, EndTime);
                TotalCost = breakdown.TotalCost;
            }
            else
            {
                TotalCost = Math.Round(TotalEnergyKwh * RatePerKwh, 0);
            }

            Status = SessionStatus.Completed;
        }
        else
        {
            // No energy delivered — truly failed
            Status = SessionStatus.Failed;
        }
    }

    /// <summary>
    /// Adds a meter value reading. Returns null if a duplicate (same timestamp + energy) already exists.
    /// </summary>
    public MeterValue? AddMeterValue(
        Guid meterValueId,
        decimal energyKwh,
        DateTime timestamp,
        decimal? currentAmps,
        decimal? voltageVolts,
        decimal? powerKw,
        decimal? socPercent)
    {
        // Idempotency: reject duplicate readings (charger retries)
        if (MeterValues.Any(mv => mv.Timestamp == timestamp && mv.EnergyKwh == energyKwh))
        {
            return null;
        }

        var meterValue = new MeterValue(
            meterValueId,
            Id,
            StationId,
            ConnectorNumber,
            energyKwh,
            timestamp,
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
