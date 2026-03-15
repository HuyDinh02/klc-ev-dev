using KLC.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace KLC.Permissions;

public class KLCPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(KLCPermissions.GroupName);

        // Station permissions
        var stationsPermission = myGroup.AddPermission(KLCPermissions.Stations.Default, L("Permission:Stations"));
        stationsPermission.AddChild(KLCPermissions.Stations.Create, L("Permission:Stations.Create"));
        stationsPermission.AddChild(KLCPermissions.Stations.Update, L("Permission:Stations.Update"));
        stationsPermission.AddChild(KLCPermissions.Stations.Delete, L("Permission:Stations.Delete"));
        stationsPermission.AddChild(KLCPermissions.Stations.Decommission, L("Permission:Stations.Decommission"));

        // Connector permissions
        var connectorsPermission = myGroup.AddPermission(KLCPermissions.Connectors.Default, L("Permission:Connectors"));
        connectorsPermission.AddChild(KLCPermissions.Connectors.Create, L("Permission:Connectors.Create"));
        connectorsPermission.AddChild(KLCPermissions.Connectors.Update, L("Permission:Connectors.Update"));
        connectorsPermission.AddChild(KLCPermissions.Connectors.Delete, L("Permission:Connectors.Delete"));
        connectorsPermission.AddChild(KLCPermissions.Connectors.Enable, L("Permission:Connectors.Enable"));
        connectorsPermission.AddChild(KLCPermissions.Connectors.Disable, L("Permission:Connectors.Disable"));

        // E-Invoice permissions
        var eInvoicesPermission = myGroup.AddPermission(KLCPermissions.EInvoices.Default, L("Permission:EInvoices"));
        eInvoicesPermission.AddChild(KLCPermissions.EInvoices.Generate, L("Permission:EInvoices.Generate"));
        eInvoicesPermission.AddChild(KLCPermissions.EInvoices.Retry, L("Permission:EInvoices.Retry"));
        eInvoicesPermission.AddChild(KLCPermissions.EInvoices.Cancel, L("Permission:EInvoices.Cancel"));

        // Tariff permissions
        var tariffsPermission = myGroup.AddPermission(KLCPermissions.Tariffs.Default, L("Permission:Tariffs"));
        tariffsPermission.AddChild(KLCPermissions.Tariffs.Create, L("Permission:Tariffs.Create"));
        tariffsPermission.AddChild(KLCPermissions.Tariffs.Update, L("Permission:Tariffs.Update"));
        tariffsPermission.AddChild(KLCPermissions.Tariffs.Activate, L("Permission:Tariffs.Activate"));
        tariffsPermission.AddChild(KLCPermissions.Tariffs.Deactivate, L("Permission:Tariffs.Deactivate"));

        // Session permissions
        var sessionsPermission = myGroup.AddPermission(KLCPermissions.Sessions.Default, L("Permission:Sessions"));
        sessionsPermission.AddChild(KLCPermissions.Sessions.ViewAll, L("Permission:Sessions.ViewAll"));

        // Fault permissions
        var faultsPermission = myGroup.AddPermission(KLCPermissions.Faults.Default, L("Permission:Faults"));
        faultsPermission.AddChild(KLCPermissions.Faults.Update, L("Permission:Faults.Update"));

        // Alert permissions
        var alertsPermission = myGroup.AddPermission(KLCPermissions.Alerts.Default, L("Permission:Alerts"));
        alertsPermission.AddChild(KLCPermissions.Alerts.Acknowledge, L("Permission:Alerts.Acknowledge"));

        // Monitoring permissions
        var monitoringPermission = myGroup.AddPermission(KLCPermissions.Monitoring.Default, L("Permission:Monitoring"));
        monitoringPermission.AddChild(KLCPermissions.Monitoring.Dashboard, L("Permission:Monitoring.Dashboard"));
        monitoringPermission.AddChild(KLCPermissions.Monitoring.StatusHistory, L("Permission:Monitoring.StatusHistory"));
        monitoringPermission.AddChild(KLCPermissions.Monitoring.EnergySummary, L("Permission:Monitoring.EnergySummary"));

        // Station Group permissions
        var stationGroupsPermission = myGroup.AddPermission(KLCPermissions.StationGroups.Default, L("Permission:StationGroups"));
        stationGroupsPermission.AddChild(KLCPermissions.StationGroups.Create, L("Permission:StationGroups.Create"));
        stationGroupsPermission.AddChild(KLCPermissions.StationGroups.Update, L("Permission:StationGroups.Update"));
        stationGroupsPermission.AddChild(KLCPermissions.StationGroups.Delete, L("Permission:StationGroups.Delete"));
        stationGroupsPermission.AddChild(KLCPermissions.StationGroups.Assign, L("Permission:StationGroups.Assign"));

        // Payment permissions
        var paymentsPermission = myGroup.AddPermission(KLCPermissions.Payments.Default, L("Permission:Payments"));
        paymentsPermission.AddChild(KLCPermissions.Payments.ViewAll, L("Permission:Payments.ViewAll"));
        paymentsPermission.AddChild(KLCPermissions.Payments.Refund, L("Permission:Payments.Refund"));

        // Audit Log permissions
        var auditLogsPermission = myGroup.AddPermission(KLCPermissions.AuditLogs.Default, L("Permission:AuditLogs"));
        auditLogsPermission.AddChild(KLCPermissions.AuditLogs.Export, L("Permission:AuditLogs.Export"));

        // User Management permissions
        var userManagementPermission = myGroup.AddPermission(KLCPermissions.UserManagement.Default, L("Permission:UserManagement"));
        userManagementPermission.AddChild(KLCPermissions.UserManagement.Create, L("Permission:UserManagement.Create"));
        userManagementPermission.AddChild(KLCPermissions.UserManagement.Update, L("Permission:UserManagement.Update"));
        userManagementPermission.AddChild(KLCPermissions.UserManagement.Delete, L("Permission:UserManagement.Delete"));
        userManagementPermission.AddChild(KLCPermissions.UserManagement.ManageRoles, L("Permission:UserManagement.ManageRoles"));
        userManagementPermission.AddChild(KLCPermissions.UserManagement.ManagePermissions, L("Permission:UserManagement.ManagePermissions"));

        // Role Management permissions
        var roleManagementPermission = myGroup.AddPermission(KLCPermissions.RoleManagement.Default, L("Permission:RoleManagement"));
        roleManagementPermission.AddChild(KLCPermissions.RoleManagement.Create, L("Permission:RoleManagement.Create"));
        roleManagementPermission.AddChild(KLCPermissions.RoleManagement.Update, L("Permission:RoleManagement.Update"));
        roleManagementPermission.AddChild(KLCPermissions.RoleManagement.Delete, L("Permission:RoleManagement.Delete"));
        roleManagementPermission.AddChild(KLCPermissions.RoleManagement.ManagePermissions, L("Permission:RoleManagement.ManagePermissions"));

        // Mobile User Management permissions
        var mobileUsersPermission = myGroup.AddPermission(KLCPermissions.MobileUsers.Default, L("Permission:MobileUsers"));
        mobileUsersPermission.AddChild(KLCPermissions.MobileUsers.ViewAll, L("Permission:MobileUsers.ViewAll"));
        mobileUsersPermission.AddChild(KLCPermissions.MobileUsers.Suspend, L("Permission:MobileUsers.Suspend"));
        mobileUsersPermission.AddChild(KLCPermissions.MobileUsers.WalletAdjust, L("Permission:MobileUsers.WalletAdjust"));

        // Voucher permissions
        var vouchersPermission = myGroup.AddPermission(KLCPermissions.Vouchers.Default, L("Permission:Vouchers"));
        vouchersPermission.AddChild(KLCPermissions.Vouchers.Create, L("Permission:Vouchers.Create"));
        vouchersPermission.AddChild(KLCPermissions.Vouchers.Update, L("Permission:Vouchers.Update"));
        vouchersPermission.AddChild(KLCPermissions.Vouchers.Delete, L("Permission:Vouchers.Delete"));

        // Promotion permissions
        var promotionsPermission = myGroup.AddPermission(KLCPermissions.Promotions.Default, L("Permission:Promotions"));
        promotionsPermission.AddChild(KLCPermissions.Promotions.Create, L("Permission:Promotions.Create"));
        promotionsPermission.AddChild(KLCPermissions.Promotions.Update, L("Permission:Promotions.Update"));
        promotionsPermission.AddChild(KLCPermissions.Promotions.Delete, L("Permission:Promotions.Delete"));

        // Feedback permissions
        var feedbackPermission = myGroup.AddPermission(KLCPermissions.Feedback.Default, L("Permission:Feedback"));
        feedbackPermission.AddChild(KLCPermissions.Feedback.Respond, L("Permission:Feedback.Respond"));

        // Notification permissions
        var notificationsPermission = myGroup.AddPermission(KLCPermissions.Notifications.Default, L("Permission:Notifications"));
        notificationsPermission.AddChild(KLCPermissions.Notifications.Broadcast, L("Permission:Notifications.Broadcast"));

        // Maintenance permissions
        var maintenancePermission = myGroup.AddPermission(KLCPermissions.Maintenance.Default, L("Permission:Maintenance"));
        maintenancePermission.AddChild(KLCPermissions.Maintenance.Create, L("Permission:Maintenance.Create"));
        maintenancePermission.AddChild(KLCPermissions.Maintenance.Update, L("Permission:Maintenance.Update"));
        maintenancePermission.AddChild(KLCPermissions.Maintenance.Delete, L("Permission:Maintenance.Delete"));

        // Settings permissions
        var settingsPermission = myGroup.AddPermission(KLCPermissions.Settings.Default, L("Permission:Settings"));
        settingsPermission.AddChild(KLCPermissions.Settings.Update, L("Permission:Settings.Update"));

        // Power Sharing permissions
        var powerSharingPermission = myGroup.AddPermission(KLCPermissions.PowerSharing.Default, L("Permission:PowerSharing"));
        powerSharingPermission.AddChild(KLCPermissions.PowerSharing.Create, L("Permission:PowerSharing.Create"));
        powerSharingPermission.AddChild(KLCPermissions.PowerSharing.Update, L("Permission:PowerSharing.Update"));
        powerSharingPermission.AddChild(KLCPermissions.PowerSharing.Delete, L("Permission:PowerSharing.Delete"));
        powerSharingPermission.AddChild(KLCPermissions.PowerSharing.ManageMembers, L("Permission:PowerSharing.ManageMembers"));

        // Operator permissions
        var operatorsPermission = myGroup.AddPermission(KLCPermissions.Operators.Default, L("Permission:Operators"));
        operatorsPermission.AddChild(KLCPermissions.Operators.Create, L("Permission:Operators.Create"));
        operatorsPermission.AddChild(KLCPermissions.Operators.Update, L("Permission:Operators.Update"));
        operatorsPermission.AddChild(KLCPermissions.Operators.Delete, L("Permission:Operators.Delete"));
        operatorsPermission.AddChild(KLCPermissions.Operators.ManageStations, L("Permission:Operators.ManageStations"));
        operatorsPermission.AddChild(KLCPermissions.Operators.ManageWebhooks, L("Permission:Operators.ManageWebhooks"));

        // Fleet permissions
        var fleetsPermission = myGroup.AddPermission(KLCPermissions.Fleets.Default, L("Permission:Fleets"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.Create, L("Permission:Fleets.Create"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.Update, L("Permission:Fleets.Update"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.Delete, L("Permission:Fleets.Delete"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.ManageVehicles, L("Permission:Fleets.ManageVehicles"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.ManageSchedules, L("Permission:Fleets.ManageSchedules"));
        fleetsPermission.AddChild(KLCPermissions.Fleets.ViewAnalytics, L("Permission:Fleets.ViewAnalytics"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KLCResource>(name);
    }
}
