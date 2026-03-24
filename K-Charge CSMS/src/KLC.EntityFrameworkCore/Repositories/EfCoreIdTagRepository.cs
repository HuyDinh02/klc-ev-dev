using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using KLC.Authorization;

namespace KLC.EntityFrameworkCore.Repositories;

public class EfCoreIdTagRepository : EfCoreRepository<KlcDbContext, IdTag, Guid>, IIdTagRepository
{
    public EfCoreIdTagRepository(IDbContextProvider<KlcDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<IdTag> FindByTagIdAsync(string tagId)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.TagId == tagId);
    }
}
