using KLC.Samples;
using Xunit;

namespace KLC.EntityFrameworkCore.Domains;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<KLCEntityFrameworkCoreTestModule>
{

}
