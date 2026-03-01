using Volo.Abp.Modularity;

namespace KCharge;

/* Inherit from this class for your domain layer tests. */
public abstract class KChargeDomainTestBase<TStartupModule> : KChargeTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
