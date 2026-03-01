using KCharge.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace KCharge.Permissions;

public class KChargePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(KChargePermissions.GroupName);

        // Station permissions
        var stationsPermission = myGroup.AddPermission(KChargePermissions.Stations.Default, L("Permission:Stations"));
        stationsPermission.AddChild(KChargePermissions.Stations.Create, L("Permission:Stations.Create"));
        stationsPermission.AddChild(KChargePermissions.Stations.Update, L("Permission:Stations.Update"));
        stationsPermission.AddChild(KChargePermissions.Stations.Delete, L("Permission:Stations.Delete"));
        stationsPermission.AddChild(KChargePermissions.Stations.Decommission, L("Permission:Stations.Decommission"));

        // Connector permissions
        var connectorsPermission = myGroup.AddPermission(KChargePermissions.Connectors.Default, L("Permission:Connectors"));
        connectorsPermission.AddChild(KChargePermissions.Connectors.Create, L("Permission:Connectors.Create"));
        connectorsPermission.AddChild(KChargePermissions.Connectors.Update, L("Permission:Connectors.Update"));
        connectorsPermission.AddChild(KChargePermissions.Connectors.Delete, L("Permission:Connectors.Delete"));
        connectorsPermission.AddChild(KChargePermissions.Connectors.Enable, L("Permission:Connectors.Enable"));
        connectorsPermission.AddChild(KChargePermissions.Connectors.Disable, L("Permission:Connectors.Disable"));

        // E-Invoice permissions
        var eInvoicesPermission = myGroup.AddPermission(KChargePermissions.EInvoices.Default, L("Permission:EInvoices"));
        eInvoicesPermission.AddChild(KChargePermissions.EInvoices.Generate, L("Permission:EInvoices.Generate"));
        eInvoicesPermission.AddChild(KChargePermissions.EInvoices.Retry, L("Permission:EInvoices.Retry"));
        eInvoicesPermission.AddChild(KChargePermissions.EInvoices.Cancel, L("Permission:EInvoices.Cancel"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KChargeResource>(name);
    }
}
