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

## RFID-Initiated Session Flow
```
1. Driver taps RFID card on charger
2. CP → Authorize(idTag: "B4A63CDF") → CSMS
3. CSMS looks up UserIdTag table → finds active, non-expired tag → Accepted
4. CP → StartTransaction(connectorId, idTag, meterStart) → CSMS
5. CSMS resolves userId from UserIdTag → creates session with UserId + tariff rate
6. CP → MeterValues (periodic) → CSMS stores, updates running cost
7. CP → StopTransaction(meterStop) → CSMS finalizes session
```

## IdTag Resolution Order
1. GUID parse → mobile app flow (userId sent directly as idTag)
2. UserIdTag table lookup → RFID/physical tag (WHERE IdTag = value AND IsActive = true AND not expired)
3. TEST/DEMO prefix → dev/staging only
4. Reject unknown tags

## Implementation Rules
- Idempotent handling (chargers retry)
- Persist transaction data immediately
- Queue commands for offline chargers
- Heartbeat timeout → mark Offline
- Strongly-typed message models
- MeterValues measurands: Energy.Active.Import.Register, Current.Import, Voltage, Power.Active.Import, SoC

## Implemented Safeguards
- MeterValue idempotency: duplicate timestamp + energy readings rejected
- Monotonic validation: backward readings and unreasonable jumps (>500 kWh) rejected
- Orphaned session cleanup: station disconnect marks active sessions as Failed
- Tariff snapshot: rate captured at session start for billing consistency
