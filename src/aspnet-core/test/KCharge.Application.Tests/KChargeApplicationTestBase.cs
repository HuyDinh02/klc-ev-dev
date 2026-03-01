using Volo.Abp.Modularity;

namespace KCharge;

public abstract class KChargeApplicationTestBase<TStartupModule> : KChargeTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
