using KLC.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace KLC.Driver;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreModule),
    typeof(KLCEntityFrameworkCoreModule)
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
