using KCharge.Samples;
using Xunit;

namespace KCharge.EntityFrameworkCore.Applications;

[Collection(KChargeTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<KChargeEntityFrameworkCoreTestModule>
{

}
