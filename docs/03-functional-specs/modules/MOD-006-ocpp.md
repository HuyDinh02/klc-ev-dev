# MOD-006: OCPP 1.6J Integration

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Core integration module implementing OCPP 1.6J (JSON over WebSocket) for bidirectional communication between charging stations and CSMS. Handles all protocol messages, connection lifecycle, and message reliability.

## 2. Actors
| Actor | Role |
|-------|------|
| Charge Point (Hardware) | Connects via WebSocket, sends/receives OCPP messages |
| CSMS (System) | WebSocket server, processes messages, sends commands |
| Admin | Configure OCPP parameters, view connection status |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-006-01 | WebSocket server accepting charger connections at ws://host/ocpp/{chargePointId} | Must |
| FR-006-02 | Process all CP→CSMS messages: BootNotification, Heartbeat, StatusNotification, Authorize, StartTransaction, StopTransaction, MeterValues | Must |
| FR-006-03 | Send CSMS→CP commands: RemoteStartTransaction, RemoteStopTransaction, Reset, ChangeConfiguration, GetConfiguration, UnlockConnector | Must |
| FR-006-04 | Maintain registry of active charger connections | Must |
| FR-006-05 | Handle charger reconnection with session state recovery | Must |
| FR-006-06 | Message retry and offline queuing for unreachable chargers | Must |
| FR-006-07 | Idempotent message handling (chargers may retry same message) | Must |
| FR-006-08 | Strongly-typed message serialization/deserialization | Must |
| FR-006-09 | Process DiagnosticsStatusNotification and FirmwareStatusNotification | Should |
| FR-006-10 | Support SetChargingProfile and ClearChargingProfile for smart charging | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-006-01 | Charger ID in WebSocket URL must match registered station code |
| BR-006-02 | Unknown chargers rejected at connection |
| BR-006-03 | BootNotification response includes heartbeat interval |
| BR-006-04 | Transaction data persisted immediately upon StartTransaction/StopTransaction |
| BR-006-05 | MeterValues persisted immediately for billing accuracy |
| BR-006-06 | Heartbeat timeout → mark station Offline |

## 5. Data Model
### OcppConnection (Runtime - Redis)
| Field | Type | Description |
|-------|------|-------------|
| ChargePointId | string | Station code |
| ConnectionId | string | WebSocket connection ID |
| ConnectedAt | DateTime | Connection time |
| LastHeartbeat | DateTime | Last heartbeat received |
| Status | string | Connected, Disconnected |

### OcppMessageLog (Entity - for debugging)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| ChargePointId | string | Station code |
| Direction | string | CP_TO_CSMS, CSMS_TO_CP |
| Action | string | OCPP action name |
| Payload | string(JSON) | Message payload |
| Timestamp | DateTime | When processed |

## 6. Message Flow: Normal Charging Session
```
1. CP → BootNotification → CSMS responds (Accepted, heartbeatInterval)
2. CP → Heartbeat (periodic) → CSMS responds (currentTime)
3. CP → StatusNotification (Available) → CSMS acknowledges
4. User starts from app → CSMS → RemoteStartTransaction → CP
5. CP → StartTransaction (meterStart, timestamp) → CSMS responds (transactionId)
6. CP → MeterValues (periodic kWh, V, A) → CSMS stores
7. User stops from app → CSMS → RemoteStopTransaction → CP
8. CP → StopTransaction (meterStop, timestamp, reason) → CSMS finalizes session
9. CP → StatusNotification (Available) → CSMS acknowledges
```

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_006_001 | Unknown charge point ID | WebSocket rejected |
| MOD_006_002 | Invalid OCPP message format | Error response |
| MOD_006_003 | Charge point not connected | 503 |
| MOD_006_004 | Command timeout | 504 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-006-01 | Charger connects with valid ID | Connection accepted, BootNotification processed |
| TC-006-02 | Charger connects with unknown ID | Connection rejected |
| TC-006-03 | Full charging session flow | All messages processed, session created and finalized |
| TC-006-04 | Charger disconnects and reconnects | Session state recovered |
| TC-006-05 | Duplicate StartTransaction message | Handled idempotently |
| TC-006-06 | No heartbeat for timeout period | Station marked Offline |
| TC-006-07 | RemoteStart to offline charger | Queued, sent on reconnect |
