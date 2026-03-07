namespace KLC.Settings;

public static class KLCSettings
{
    private const string Prefix = "KLC";

    public static class General
    {
        private const string GroupPrefix = Prefix + ".General";
        public const string SiteName = GroupPrefix + ".SiteName";
        public const string Timezone = GroupPrefix + ".Timezone";
        public const string Currency = GroupPrefix + ".Currency";
        public const string Language = GroupPrefix + ".Language";
    }

    public static class Notifications
    {
        private const string GroupPrefix = Prefix + ".Notifications";
        public const string EmailEnabled = GroupPrefix + ".EmailEnabled";
        public const string SmsEnabled = GroupPrefix + ".SmsEnabled";
        public const string PushEnabled = GroupPrefix + ".PushEnabled";
        public const string AlertEmail = GroupPrefix + ".AlertEmail";
    }

    public static class Ocpp
    {
        private const string GroupPrefix = Prefix + ".Ocpp";
        public const string WebSocketPort = GroupPrefix + ".WebSocketPort";
        public const string HeartbeatInterval = GroupPrefix + ".HeartbeatInterval";
        public const string MeterValueInterval = GroupPrefix + ".MeterValueInterval";
    }

    public static class Payments
    {
        private const string GroupPrefix = Prefix + ".Payments";
        public const string DefaultGateway = GroupPrefix + ".DefaultGateway";
        public const string AutoInvoiceGeneration = GroupPrefix + ".AutoInvoiceGeneration";
        public const string EInvoiceProvider = GroupPrefix + ".EInvoiceProvider";
    }

    public static class Security
    {
        private const string GroupPrefix = Prefix + ".Security";
        public const string SessionTimeout = GroupPrefix + ".SessionTimeout";
        public const string RequireMfa = GroupPrefix + ".RequireMfa";
        public const string PasswordMinLength = GroupPrefix + ".PasswordMinLength";
    }
}
