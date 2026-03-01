# OCPP Flows

## Protocol: OCPP 1.6J (JSON over WebSocket)
Connection URL: ws://csms-host/ocpp/{chargePointId}

## Normal Charging Session Flow
```
1. CP → BootNotification → CSMS (Accepted, heartbeatInterval)
2. CP → Heartbeat (periodic) → CSMS (currentTime)
3. CP → StatusNotification (Available)
4. App → CSMS → RemoteStartTransaction → CP
5. CP → StartTransaction (meterStart) → CSMS (transactionId)
6. CP → MeterValues (periodic kWh, V, A) → CSMS stores
7. App → CSMS → RemoteStopTransaction → CP
8. CP → StopTransaction (meterStop, reason) → CSMS finalizes
9. CP → StatusNotification (Available)
```

## Key Messages
### CP → CSMS
BootNotification, Heartbeat, StatusNotification, Authorize, StartTransaction, StopTransaction, MeterValues, DiagnosticsStatusNotification, FirmwareStatusNotification

### CSMS → CP
RemoteStartTransaction, RemoteStopTransaction, Reset, ChangeConfiguration, GetConfiguration, UnlockConnector, UpdateFirmware, SetChargingProfile, ClearChargingProfile, TriggerMessage

## Implementation Rules
- Idempotent handling (chargers retry)
- Persist transaction data immediately
- Queue commands for offline chargers
- Heartbeat timeout → mark Offline
- Strongly-typed message models
- MeterValues measurands: Energy.Active.Import.Register, Current.Import, Voltage, Power.Active.Import, SoC
