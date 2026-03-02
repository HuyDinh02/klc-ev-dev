using Xunit;

namespace KLC.EntityFrameworkCore;

[CollectionDefinition(KLCTestConsts.CollectionDefinitionName)]
public class KLCEntityFrameworkCoreCollection : ICollectionFixture<KLCEntityFrameworkCoreFixture>
{

}
