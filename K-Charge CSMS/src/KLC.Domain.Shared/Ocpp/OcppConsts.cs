namespace KLC.Ocpp;

public static class OcppConsts
{
    public const string SubProtocol = "ocpp1.6";
    public const int MaxMessageSize = 65536; // 64KB
    public const int DefaultHeartbeatInterval = 300;
    public const int DefaultMeterValueSampleInterval = 60;
    public const int DefaultClockAlignedDataInterval = 900;
    public const int DefaultConnectionTimeout = 60;
    public const int CommandTimeoutSeconds = 30;
    public const string WebSocketPath = "/ocpp";
}
