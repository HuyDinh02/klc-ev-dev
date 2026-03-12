using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Operators;

/// <summary>
/// Links an operator to a station they are allowed to manage.
/// </summary>
public class OperatorStation : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the operator.
    /// </summary>
    public Guid OperatorId { get; private set; }

    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    protected OperatorStation()
    {
        // Required by EF Core
    }

    internal OperatorStation(Guid id, Guid operatorId, Guid stationId)
        : base(id)
    {
        OperatorId = operatorId;
        StationId = stationId;
    }

    internal void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletionTime = DateTime.UtcNow;
    }
}
