using KCharge.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace KCharge.Permissions;

public class KChargePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(KChargePermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(KChargePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KChargeResource>(name);
    }
}
