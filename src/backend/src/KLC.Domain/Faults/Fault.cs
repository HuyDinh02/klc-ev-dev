using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Faults;

/// <summary>
/// Represents a fault or error reported by a charging station.
/// </summary>
public class Fault : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Connector number where the fault occurred (null if station-level).
    /// </summary>
    public int? ConnectorNumber { get; private set; }

    /// <summary>
    /// OCPP error code (e.g., "ConnectorLockFailure", "GroundFailure").
    /// </summary>
    public string ErrorCode { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable error info from the charger.
    /// </summary>
    public string? ErrorInfo { get; private set; }

    /// <summary>
    /// Vendor-specific error code.
    /// </summary>
    public string? VendorErrorCode { get; private set; }

    /// <summary>
    /// Current fault status.
    /// </summary>
    public FaultStatus Status { get; private set; }

    /// <summary>
    /// When the fault was detected.
    /// </summary>
    public DateTime DetectedAt { get; private set; }

    /// <summary>
    /// When the fault was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// User who resolved the fault.
    /// </summary>
    public Guid? ResolvedByUserId { get; private set; }

    /// <summary>
    /// Resolution notes.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// Priority level (1 = critical, 2 = high, 3 = medium, 4 = low).
    /// </summary>
    public int Priority { get; private set; }

    protected Fault()
    {
        // Required by EF Core
    }

    public Fault(
        Guid id,
        Guid stationId,
        int? connectorNumber,
        string errorCode,
        string? errorInfo = null,
        string? vendorErrorCode = null)
        : base(id)
    {
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        ErrorCode = errorCode;
        ErrorInfo = errorInfo;
        VendorErrorCode = vendorErrorCode;
        Status = FaultStatus.Open;
        DetectedAt = DateTime.UtcNow;
        Priority = DeterminePriority(errorCode);
    }

    private static int DeterminePriority(string errorCode)
    {
        // Critical errors that affect safety or prevent charging
        var criticalErrors = new[]
        {
            "GroundFailure", "HighTemperature", "OverCurrentFailure",
            "OverVoltage", "UnderVoltage", "PowerMeterFailure"
        };

        // High priority errors that affect charging capability
        var highErrors = new[]
        {
            "ConnectorLockFailure", "EVCommunicationError",
            "ReaderFailure", "InternalError"
        };

        if (Array.Exists(criticalErrors, e => e.Equals(errorCode, StringComparison.OrdinalIgnoreCase)))
            return 1;
        if (Array.Exists(highErrors, e => e.Equals(errorCode, StringComparison.OrdinalIgnoreCase)))
            return 2;

        return 3; // Default to medium priority
    }

    public void StartInvestigation()
    {
        if (Status != FaultStatus.Open)
            throw new BusinessException(KLCDomainErrorCodes.Fault.InvalidStatusTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("newStatus", nameof(FaultStatus.Investigating));
        Status = FaultStatus.Investigating;
    }

    public void Resolve(Guid resolvedByUserId, string? resolutionNotes = null)
    {
        Status = FaultStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByUserId = resolvedByUserId;
        ResolutionNotes = resolutionNotes;
    }

    public void Close(string? notes = null)
    {
        Status = FaultStatus.Closed;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = notes;
    }

    public void Reopen()
    {
        if (Status != FaultStatus.Resolved && Status != FaultStatus.Closed)
            throw new BusinessException(KLCDomainErrorCodes.Fault.InvalidStatusTransition)
                .WithData("currentStatus", Status.ToString())
                .WithData("newStatus", nameof(FaultStatus.Open));
        Status = FaultStatus.Open;
        ResolvedAt = null;
        ResolvedByUserId = null;
        ResolutionNotes = null;
    }

    public void SetPriority(int priority)
    {
        if (priority < 1 || priority > 4)
            throw new BusinessException(KLCDomainErrorCodes.Fault.InvalidPriority);
        Priority = priority;
    }

    public void UpdateStatus(FaultStatus newStatus, string? notes = null)
    {
        switch (newStatus)
        {
            case FaultStatus.Investigating:
                StartInvestigation();
                break;
            case FaultStatus.Resolved:
                Status = FaultStatus.Resolved;
                ResolvedAt = DateTime.UtcNow;
                ResolutionNotes = notes;
                break;
            case FaultStatus.Closed:
                Close(notes);
                break;
            case FaultStatus.Open:
                if (Status == FaultStatus.Resolved || Status == FaultStatus.Closed)
                {
                    Reopen();
                }
                break;
        }
    }
}
