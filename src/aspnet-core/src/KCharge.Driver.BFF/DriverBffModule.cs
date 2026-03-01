using KCharge.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace KCharge.Driver;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreModule),
    typeof(KChargeEntityFrameworkCoreModule)
)]
public class DriverBffModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // BFF services are registered in Program.cs
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // No additional initialization needed
    }
}
