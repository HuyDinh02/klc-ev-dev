# OCPP WebSocket Architecture

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. Overview

OCPP 1.6J (JSON over WebSocket) is the communication standard between charging stations and CSMS. Each charger maintains a persistent WebSocket connection for full-duplex, real-time communication.

## 2. Connection Model

```
Charger → WebSocket → ws://csms-host/ocpp/{chargePointId} → CSMS Handler
```

Each charger connects with its unique ID in the URL path. The CSMS maintains a registry of active connections for sending commands back to chargers.

## 3. Message Flows

### Charge Point → CSMS (charger-initiated)
| Message | Purpose | When |
|---------|---------|------|
| BootNotification | Register charger on startup | Charger boots/restarts |
| Heartbeat | Periodic alive signal | Every N seconds (configured) |
| StatusNotification | Connector status change | Status changes (Available → Charging, etc.) |
| Authorize | Validate user/RFID tag | Before charging starts |
| StartTransaction | Begin charging session | User initiates charge |
| StopTransaction | End charging session | Charging stops (user/system/error) |
| MeterValues | Energy readings | During charging (periodic interval) |
| DiagnosticsStatusNotification | Diagnostics upload status | After diagnostics request |
| FirmwareStatusNotification | Firmware update status | During firmware update |

### CSMS → Charge Point (server-initiated)
| Message | Purpose | When |
|---------|---------|------|
| RemoteStartTransaction | Start charging remotely | User starts from app |
| RemoteStopTransaction | Stop charging remotely | User stops from app |
| Reset | Hard/soft reset charger | Admin command |
| ChangeConfiguration | Update charger settings | Configuration change |
| GetConfiguration | Read charger settings | Status check |
| UnlockConnector | Remote unlock | Operator recovery |
| UpdateFirmware | Trigger firmware update | Admin command |
| SetChargingProfile | Smart charging profiles | Load management |
| ClearChargingProfile | Remove profiles | Reset load config |
| TriggerMessage | Request specific message | Diagnostic request |

## 4. Implementation Requirements

- Separate OCPP WebSocket handler service
- Strongly-typed message serialization/deserialization
- Idempotent message handling (chargers may retry)
- Persistent message queue for offline chargers
- Immediate transaction data persistence for billing accuracy
- Reconnection handling with session state recovery
- MeterValues measurands: Energy.Active.Import.Register, Current.Import, Voltage, Power.Active.Import, SoC
