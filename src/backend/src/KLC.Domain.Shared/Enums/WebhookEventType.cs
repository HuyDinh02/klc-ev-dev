namespace KLC.Enums;

/// <summary>
/// Types of webhook events sent to external operators.
/// </summary>
public enum WebhookEventType
{
    SessionStarted = 0,
    SessionCompleted = 1,
    FaultDetected = 2,
    StationOffline = 3,
    ConnectorStatusChanged = 4
}
