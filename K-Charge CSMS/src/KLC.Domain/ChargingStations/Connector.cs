using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using KLC.Ocpp;

namespace KLC.ChargingStations;

public class Connector : Entity<Guid>
{
    public Guid ChargingStationId { get; private set; }
    public int ConnectorId { get; private set; }
    public ConnectorType Type { get; private set; }
    public ChargePointStatus Status { get; private set; }
    public ChargePointErrorCode ErrorCode { get; private set; }
    public DateTime? StatusTimestamp { get; private set; }
    public decimal MaxPowerKw { get; private set; }

    protected Connector()
    {
    }

    public Connector(
        Guid id,
        Guid chargingStationId,
        int connectorId,
        ConnectorType type,
        decimal maxPowerKw = 7m) : base(id)
    {
        ChargingStationId = Check.NotDefaultOrEmpty(chargingStationId, nameof(chargingStationId));
        Check.Range(connectorId, nameof(connectorId), 1, int.MaxValue);

        ConnectorId = connectorId;
        Type = type;
        MaxPowerKw = Check.Range(maxPowerKw, nameof(maxPowerKw), 0.1m, decimal.MaxValue);
        Status = ChargePointStatus.Unavailable;
        ErrorCode = ChargePointErrorCode.NoError;
        StatusTimestamp = DateTime.UtcNow;
    }

    public void UpdateStatus(
        ChargePointStatus status,
        ChargePointErrorCode errorCode = ChargePointErrorCode.NoError,
        DateTime? timestamp = null)
    {
        Status = status;
        ErrorCode = errorCode;
        StatusTimestamp = timestamp ?? DateTime.UtcNow;
    }

    public void ClearError()
    {
        ErrorCode = ChargePointErrorCode.NoError;
    }
}
