namespace KCharge.Permissions;

public static class KChargePermissions
{
    public const string GroupName = "KCharge";

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
}
