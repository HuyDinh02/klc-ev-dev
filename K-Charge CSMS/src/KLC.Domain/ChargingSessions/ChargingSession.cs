using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.ChargingSessions;

public class ChargingSession : FullAuditedAggregateRoot<Guid>
{
    public int TransactionId { get; private set; }
    public string ChargePointId { get; private set; }
    public int ConnectorId { get; private set; }
    public string IdTag { get; private set; }

    // Metering
    public int MeterStartWh { get; private set; }
    public int? MeterStopWh { get; private set; }
    public int EnergyConsumedWh => (MeterStopWh ?? MeterStartWh) - MeterStartWh;

    // Timestamps
    public DateTime StartTimestamp { get; private set; }
    public DateTime? StopTimestamp { get; private set; }
    public TimeSpan? Duration => StopTimestamp.HasValue ? StopTimestamp.Value - StartTimestamp : (TimeSpan?)null;

    // Status
    public ChargingSessionStatus Status { get; private set; }
    public string? StopReason { get; private set; }

    // Billing
    public decimal? TotalCostVnd { get; private set; }
    public Guid? TariffPlanId { get; private set; }

    // Navigation
    public ICollection<MeterValue> MeterValues { get; private set; }

    protected ChargingSession()
    {
        MeterValues = new List<MeterValue>();
    }

    public ChargingSession(
        Guid id,
        int transactionId,
        string chargePointId,
        int connectorId,
        string idTag,
        int meterStartWh,
        DateTime startTimestamp) : base(id)
    {
        TransactionId = Check.Range(transactionId, nameof(transactionId), 1, int.MaxValue);
        ChargePointId = Check.NotNullOrWhiteSpace(chargePointId, nameof(chargePointId));
        Check.Range(connectorId, nameof(connectorId), 0, int.MaxValue);
        IdTag = Check.NotNullOrWhiteSpace(idTag, nameof(idTag));
        Check.Range(meterStartWh, nameof(meterStartWh), 0, int.MaxValue);

        ConnectorId = connectorId;
        MeterStartWh = meterStartWh;
        StartTimestamp = startTimestamp;
        Status = ChargingSessionStatus.Active;
        MeterValues = new List<MeterValue>();
    }

    public void Stop(
        int meterStopWh,
        DateTime stopTimestamp,
        string? stopReason = null)
    {
        Check.Range(meterStopWh, nameof(meterStopWh), MeterStartWh, int.MaxValue,
            message: "Meter stop value must be greater than or equal to meter start value");

        MeterStopWh = meterStopWh;
        StopTimestamp = stopTimestamp;
        StopReason = stopReason;
        Status = ChargingSessionStatus.Completed;
    }

    public void AddMeterValue(MeterValue meterValue)
    {
        Check.NotNull(meterValue, nameof(meterValue));
        meterValue.ChargingSessionId = Id;
        MeterValues.Add(meterValue);
    }

    public void SetBillingResult(decimal totalCostVnd, Guid? tariffPlanId = null)
    {
        Check.Range(totalCostVnd, nameof(totalCostVnd), 0m, decimal.MaxValue);
        TotalCostVnd = totalCostVnd;
        TariffPlanId = tariffPlanId;
    }

    public bool IsActive => Status == ChargingSessionStatus.Active;

    public bool IsCompleted => Status == ChargingSessionStatus.Completed;
}
