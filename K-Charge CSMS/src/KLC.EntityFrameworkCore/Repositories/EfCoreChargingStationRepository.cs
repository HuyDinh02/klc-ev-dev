using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using KLC.ChargingStations;

namespace KLC.EntityFrameworkCore.Repositories;

public class EfCoreChargingStationRepository : EfCoreRepository<KlcDbContext, ChargingStation, Guid>, IChargingStationRepository
{
    public EfCoreChargingStationRepository(IDbContextProvider<KlcDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<ChargingStation> FindByChargePointIdAsync(string chargePointId)
    {
        return await DbSet
            .Include(x => x.Connectors)
            .FirstOrDefaultAsync(x => x.ChargePointId == chargePointId);
    }

    public async Task<IQueryable<ChargingStation>> GetOnlineStationsAsync()
    {
        return await Task.FromResult(
            DbSet
                .Include(x => x.Connectors)
                .Where(x => x.IsOnline)
        );
    }
}
