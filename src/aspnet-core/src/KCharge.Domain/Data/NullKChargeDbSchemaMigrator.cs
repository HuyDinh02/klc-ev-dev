using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace KCharge.Data;

/* This is used if database provider does't define
 * IKChargeDbSchemaMigrator implementation.
 */
public class NullKChargeDbSchemaMigrator : IKChargeDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
