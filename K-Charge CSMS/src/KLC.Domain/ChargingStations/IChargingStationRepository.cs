using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace KLC.ChargingStations;

public interface IChargingStationRepository : IRepository<ChargingStation, Guid>
{
    Task<ChargingStation?> FindByChargePointIdAsync(
        string chargePointId,
        bool includeDetails = true,
        CancellationToken cancellationToken = default);

    Task<List<ChargingStation>> GetOnlineStationsAsync(CancellationToken cancellationToken = default);
}
