using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using KLC.Ocpp;

namespace KLC.Faults;

public class Fault : Entity<Guid>, IHasCreationTime
{
    public Guid ChargingStationId { get; private set; }
    public int ConnectorId { get; private set; }
    public string ErrorCode { get; private set; }
    public string? Info { get; private set; }
    public string? VendorErrorCode { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreationTime { get; set; }

    protected Fault()
    {
    }

    public Fault(
        Guid id,
        Guid chargingStationId,
        int connectorId,
        string errorCode,
        DateTime timestamp,
        string? info = null,
        string? vendorErrorCode = null) : base(id)
    {
        ChargingStationId = Check.NotDefaultOrEmpty(chargingStationId, nameof(chargingStationId));
        Check.Range(connectorId, nameof(connectorId), 0, int.MaxValue);
        ErrorCode = Check.NotNullOrWhiteSpace(errorCode, nameof(errorCode));

        ConnectorId = connectorId;
        Timestamp = timestamp;
        Info = info;
        VendorErrorCode = vendorErrorCode;
        CreationTime = DateTime.UtcNow;
    }

    public void Resolve()
    {
        ResolvedAt = DateTime.UtcNow;
    }

    public bool IsResolved => ResolvedAt.HasValue;
}
