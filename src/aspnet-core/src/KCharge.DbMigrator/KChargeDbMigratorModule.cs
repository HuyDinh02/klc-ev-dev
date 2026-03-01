using KCharge.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace KCharge.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(KChargeEntityFrameworkCoreModule),
    typeof(KChargeApplicationContractsModule)
    )]
public class KChargeDbMigratorModule : AbpModule
{
}
