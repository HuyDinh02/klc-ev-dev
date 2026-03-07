using KLC.Notifications;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreBroadcastAppServiceTests : BroadcastAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
