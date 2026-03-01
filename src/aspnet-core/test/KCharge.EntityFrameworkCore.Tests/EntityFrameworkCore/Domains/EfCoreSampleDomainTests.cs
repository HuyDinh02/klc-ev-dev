using KCharge.Samples;
using Xunit;

namespace KCharge.EntityFrameworkCore.Domains;

[Collection(KChargeTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<KChargeEntityFrameworkCoreTestModule>
{

}
