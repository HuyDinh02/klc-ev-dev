using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using KLC.ChargingSessions;

namespace KLC.EntityFrameworkCore.Repositories;

public class EfCoreChargingSessionRepository : EfCoreRepository<KlcDbContext, ChargingSession, Guid>, IChargingSessionRepository
{
    public EfCoreChargingSessionRepository(IDbContextProvider<KlcDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<ChargingSession> FindByTransactionIdAsync(int transactionId)
    {
        return await DbSet
            .Include(x => x.MeterValues)
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId);
    }

    public async Task<IQueryable<ChargingSession>> GetActiveSessionsAsync(string chargePointId = null)
    {
        var query = DbSet
            .Include(x => x.MeterValues)
            .Where(x => x.Status == "Charging" || x.Status == "Preparing" || x.Status == "SuspendedEVSE" || x.Status == "SuspendedEV");

        if (!string.IsNullOrEmpty(chargePointId))
        {
            query = query.Where(x => x.ChargePointId == chargePointId);
        }

        return await Task.FromResult(query);
    }
}
