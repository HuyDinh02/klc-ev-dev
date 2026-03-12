namespace KLC.Permissions;

public static class KLCPermissions
{
    public const string GroupName = "KLC";

    public static class Stations
    {
        public const string Default = GroupName + ".Stations";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Decommission = Default + ".Decommission";
    }

    public static class Connectors
    {
        public const string Default = GroupName + ".Connectors";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Enable = Default + ".Enable";
        public const string Disable = Default + ".Disable";
    }

    public static class Tariffs
    {
        public const string Default = GroupName + ".Tariffs";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Activate = Default + ".Activate";
        public const string Deactivate = Default + ".Deactivate";
    }

    public static class Sessions
    {
        public const string Default = GroupName + ".Sessions";
        public const string ViewAll = Default + ".ViewAll";
    }

    public static class Faults
    {
        public const string Default = GroupName + ".Faults";
        public const string Update = Default + ".Update";
    }

    public static class Alerts
    {
        public const string Default = GroupName + ".Alerts";
        public const string Acknowledge = Default + ".Acknowledge";
    }

    public static class Monitoring
    {
        public const string Default = GroupName + ".Monitoring";
        public const string Dashboard = Default + ".Dashboard";
        public const string StatusHistory = Default + ".StatusHistory";
        public const string EnergySummary = Default + ".EnergySummary";
    }

    public static class StationGroups
    {
        public const string Default = GroupName + ".StationGroups";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Assign = Default + ".Assign";
    }

    public static class Payments
    {
        public const string Default = GroupName + ".Payments";
        public const string ViewAll = Default + ".ViewAll";
        public const string Refund = Default + ".Refund";
    }

    public static class AuditLogs
    {
        public const string Default = GroupName + ".AuditLogs";
        public const string Export = Default + ".Export";
    }

    public static class EInvoices
    {
        public const string Default = GroupName + ".EInvoices";
        public const string Generate = Default + ".Generate";
        public const string Retry = Default + ".Retry";
        public const string Cancel = Default + ".Cancel";
    }

    public static class UserManagement
    {
        public const string Default = GroupName + ".UserManagement";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageRoles = Default + ".ManageRoles";
        public const string ManagePermissions = Default + ".ManagePermissions";
    }

    public static class RoleManagement
    {
        public const string Default = GroupName + ".RoleManagement";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManagePermissions = Default + ".ManagePermissions";
    }

    public static class MobileUsers
    {
        public const string Default = GroupName + ".MobileUsers";
        public const string ViewAll = Default + ".ViewAll";
        public const string Suspend = Default + ".Suspend";
        public const string WalletAdjust = Default + ".WalletAdjust";
    }

    public static class Vouchers
    {
        public const string Default = GroupName + ".Vouchers";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Promotions
    {
        public const string Default = GroupName + ".Promotions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Feedback
    {
        public const string Default = GroupName + ".Feedback";
        public const string Respond = Default + ".Respond";
    }

    public static class Notifications
    {
        public const string Default = GroupName + ".Notifications";
        public const string Broadcast = Default + ".Broadcast";
    }

    public static class Maintenance
    {
        public const string Default = GroupName + ".Maintenance";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class PowerSharing
    {
        public const string Default = GroupName + ".PowerSharing";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageMembers = Default + ".ManageMembers";
    }

    public static class Operators
    {
        public const string Default = GroupName + ".Operators";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageStations = Default + ".ManageStations";
        public const string ManageWebhooks = Default + ".ManageWebhooks";
    }

    public static class Fleets
    {
        public const string Default = GroupName + ".Fleets";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageVehicles = Default + ".ManageVehicles";
        public const string ManageSchedules = Default + ".ManageSchedules";
        public const string ViewAnalytics = Default + ".ViewAnalytics";
    }
}
