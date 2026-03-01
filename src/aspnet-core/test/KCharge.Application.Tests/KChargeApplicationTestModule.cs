using Volo.Abp.Modularity;

namespace KCharge;

[DependsOn(
    typeof(KChargeApplicationModule),
    typeof(KChargeDomainTestModule)
)]
public class KChargeApplicationTestModule : AbpModule
{

}
