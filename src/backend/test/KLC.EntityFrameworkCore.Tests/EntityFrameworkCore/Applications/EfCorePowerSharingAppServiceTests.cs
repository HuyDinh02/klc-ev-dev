using KLC.PowerSharing;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCorePowerSharingAppServiceTests : PowerSharingAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
