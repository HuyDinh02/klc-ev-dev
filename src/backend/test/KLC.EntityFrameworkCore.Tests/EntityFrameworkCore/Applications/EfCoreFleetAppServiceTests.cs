using KLC.Fleets;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreFleetAppServiceTests : FleetAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
