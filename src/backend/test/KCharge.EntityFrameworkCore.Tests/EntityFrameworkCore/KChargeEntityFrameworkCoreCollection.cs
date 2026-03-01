using Xunit;

namespace KCharge.EntityFrameworkCore;

[CollectionDefinition(KChargeTestConsts.CollectionDefinitionName)]
public class KChargeEntityFrameworkCoreCollection : ICollectionFixture<KChargeEntityFrameworkCoreFixture>
{

}
