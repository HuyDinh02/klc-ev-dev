using Volo.Abp.Modularity;

namespace KLC;

public abstract class KLCApplicationTestBase<TStartupModule> : KLCTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
