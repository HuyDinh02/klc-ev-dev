using Volo.Abp.Modularity;

namespace KCharge;

[DependsOn(
    typeof(KChargeDomainModule),
    typeof(KChargeTestBaseModule)
)]
public class KChargeDomainTestModule : AbpModule
{

}
