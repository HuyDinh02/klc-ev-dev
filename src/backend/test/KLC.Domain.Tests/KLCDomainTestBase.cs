using Volo.Abp.Modularity;

namespace KLC;

/* Inherit from this class for your domain layer tests. */
public abstract class KLCDomainTestBase<TStartupModule> : KLCTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
