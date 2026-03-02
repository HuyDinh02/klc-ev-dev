using KLC.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace KLC.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(KLCEntityFrameworkCoreModule),
    typeof(KLCApplicationContractsModule)
    )]
public class KLCDbMigratorModule : AbpModule
{
}
