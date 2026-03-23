using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace KLC.ChargingSessions;

public interface IChargingSessionRepository : IRepository<ChargingSession, Guid>
{
    Task<ChargingSession?> FindByTransactionIdAsync(
        int transactionId,
        bool includeDetails = true,
        CancellationToken cancellationToken = default);

    Task<List<ChargingSession>> GetActiveSessionsAsync(
        string? chargePointId = null,
        CancellationToken cancellationToken = default);
}
