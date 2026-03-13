using KLC.UserManagement;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreMyPermissionsTests : MyPermissionsTests<KLCEntityFrameworkCoreTestModule>
{
}
