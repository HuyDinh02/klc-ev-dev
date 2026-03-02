using Volo.Abp.Modularity;

namespace KLC;

[DependsOn(
    typeof(KLCDomainModule),
    typeof(KLCTestBaseModule)
)]
public class KLCDomainTestModule : AbpModule
{

}
