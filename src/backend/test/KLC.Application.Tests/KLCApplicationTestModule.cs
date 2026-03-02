using Volo.Abp.Modularity;

namespace KLC;

[DependsOn(
    typeof(KLCApplicationModule),
    typeof(KLCDomainTestModule)
)]
public class KLCApplicationTestModule : AbpModule
{

}
