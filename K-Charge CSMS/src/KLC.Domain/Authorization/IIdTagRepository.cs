using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace KLC.Authorization;

public interface IIdTagRepository : IRepository<IdTag, Guid>
{
    Task<IdTag?> FindByTagIdAsync(
        string tagId,
        CancellationToken cancellationToken = default);
}
