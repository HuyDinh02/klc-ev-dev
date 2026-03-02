using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace KLC.Data;

/* This is used if database provider does't define
 * IKLCDbSchemaMigrator implementation.
 */
public class NullKLCDbSchemaMigrator : IKLCDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
