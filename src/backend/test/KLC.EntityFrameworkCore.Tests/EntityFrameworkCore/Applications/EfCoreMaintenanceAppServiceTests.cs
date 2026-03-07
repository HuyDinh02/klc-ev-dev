using KLC.Maintenance;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreMaintenanceAppServiceTests : MaintenanceAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
