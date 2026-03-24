namespace KLC.Ocpp;

public enum OcppAction
{
    // Charge Point initiated (CP -> CSMS)
    BootNotification,
    Heartbeat,
    StatusNotification,
    Authorize,
    StartTransaction,
    StopTransaction,
    MeterValues,
    DataTransfer,
    DiagnosticsStatusNotification,
    FirmwareStatusNotification,

    // CSMS initiated (CSMS -> CP)
    RemoteStartTransaction,
    RemoteStopTransaction,
    Reset,
    UnlockConnector,
    ChangeConfiguration,
    GetConfiguration,
    ChangeAvailability,
    TriggerMessage,
    SetChargingProfile,
    ClearChargingProfile,
    UpdateFirmware,
    GetDiagnostics,
    ClearCache,
    GetLocalListVersion,
    SendLocalList,
    ReserveNow,
    CancelReservation
}
