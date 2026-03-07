using KLC.Marketing;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCorePromotionAppServiceTests : PromotionAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
