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

        // Tariff permissions
        var tariffsPermission = myGroup.AddPermission(KChargePermissions.Tariffs.Default, L("Permission:Tariffs"));
        tariffsPermission.AddChild(KChargePermissions.Tariffs.Create, L("Permission:Tariffs.Create"));
        tariffsPermission.AddChild(KChargePermissions.Tariffs.Update, L("Permission:Tariffs.Update"));
        tariffsPermission.AddChild(KChargePermissions.Tariffs.Activate, L("Permission:Tariffs.Activate"));
        tariffsPermission.AddChild(KChargePermissions.Tariffs.Deactivate, L("Permission:Tariffs.Deactivate"));

        // Session permissions
        var sessionsPermission = myGroup.AddPermission(KChargePermissions.Sessions.Default, L("Permission:Sessions"));
        sessionsPermission.AddChild(KChargePermissions.Sessions.ViewAll, L("Permission:Sessions.ViewAll"));

        // Fault permissions
        var faultsPermission = myGroup.AddPermission(KChargePermissions.Faults.Default, L("Permission:Faults"));
        faultsPermission.AddChild(KChargePermissions.Faults.Update, L("Permission:Faults.Update"));

        // Alert permissions
        var alertsPermission = myGroup.AddPermission(KChargePermissions.Alerts.Default, L("Permission:Alerts"));
        alertsPermission.AddChild(KChargePermissions.Alerts.Acknowledge, L("Permission:Alerts.Acknowledge"));

        // Monitoring permissions
        var monitoringPermission = myGroup.AddPermission(KChargePermissions.Monitoring.Default, L("Permission:Monitoring"));
        monitoringPermission.AddChild(KChargePermissions.Monitoring.Dashboard, L("Permission:Monitoring.Dashboard"));
        monitoringPermission.AddChild(KChargePermissions.Monitoring.StatusHistory, L("Permission:Monitoring.StatusHistory"));
        monitoringPermission.AddChild(KChargePermissions.Monitoring.EnergySummary, L("Permission:Monitoring.EnergySummary"));

        // Station Group permissions
        var stationGroupsPermission = myGroup.AddPermission(KChargePermissions.StationGroups.Default, L("Permission:StationGroups"));
        stationGroupsPermission.AddChild(KChargePermissions.StationGroups.Create, L("Permission:StationGroups.Create"));
        stationGroupsPermission.AddChild(KChargePermissions.StationGroups.Update, L("Permission:StationGroups.Update"));
        stationGroupsPermission.AddChild(KChargePermissions.StationGroups.Delete, L("Permission:StationGroups.Delete"));
        stationGroupsPermission.AddChild(KChargePermissions.StationGroups.Assign, L("Permission:StationGroups.Assign"));

        // Payment permissions
        var paymentsPermission = myGroup.AddPermission(KChargePermissions.Payments.Default, L("Permission:Payments"));
        paymentsPermission.AddChild(KChargePermissions.Payments.ViewAll, L("Permission:Payments.ViewAll"));
        paymentsPermission.AddChild(KChargePermissions.Payments.Refund, L("Permission:Payments.Refund"));

        // Audit Log permissions
        var auditLogsPermission = myGroup.AddPermission(KChargePermissions.AuditLogs.Default, L("Permission:AuditLogs"));
        auditLogsPermission.AddChild(KChargePermissions.AuditLogs.Export, L("Permission:AuditLogs.Export"));

        // User Management permissions
        var userManagementPermission = myGroup.AddPermission(KChargePermissions.UserManagement.Default, L("Permission:UserManagement"));
        userManagementPermission.AddChild(KChargePermissions.UserManagement.Create, L("Permission:UserManagement.Create"));
        userManagementPermission.AddChild(KChargePermissions.UserManagement.Update, L("Permission:UserManagement.Update"));
        userManagementPermission.AddChild(KChargePermissions.UserManagement.Delete, L("Permission:UserManagement.Delete"));
        userManagementPermission.AddChild(KChargePermissions.UserManagement.ManageRoles, L("Permission:UserManagement.ManageRoles"));
        userManagementPermission.AddChild(KChargePermissions.UserManagement.ManagePermissions, L("Permission:UserManagement.ManagePermissions"));

        // Role Management permissions
        var roleManagementPermission = myGroup.AddPermission(KChargePermissions.RoleManagement.Default, L("Permission:RoleManagement"));
        roleManagementPermission.AddChild(KChargePermissions.RoleManagement.Create, L("Permission:RoleManagement.Create"));
        roleManagementPermission.AddChild(KChargePermissions.RoleManagement.Update, L("Permission:RoleManagement.Update"));
        roleManagementPermission.AddChild(KChargePermissions.RoleManagement.Delete, L("Permission:RoleManagement.Delete"));
        roleManagementPermission.AddChild(KChargePermissions.RoleManagement.ManagePermissions, L("Permission:RoleManagement.ManagePermissions"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KChargeResource>(name);
    }
}
