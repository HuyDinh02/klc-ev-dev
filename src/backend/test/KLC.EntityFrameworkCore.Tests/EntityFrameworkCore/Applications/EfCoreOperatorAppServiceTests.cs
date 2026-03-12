using KLC.Operators;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreOperatorAppServiceTests : OperatorAppServiceTests<KLCEntityFrameworkCoreTestModule>
{
}
