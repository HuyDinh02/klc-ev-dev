# MOD-010: Charging Session

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Manages the complete lifecycle of a charging event: initiation via QR scan, real-time tracking, energy metering, session termination, cost calculation, and billing trigger.

## 2. Actors
| Actor | Role |
|-------|------|
| EV Driver | Start (QR scan), monitor, stop charging |
| System (OCPP) | Process start/stop transactions, meter values |
| Operator | View active and historical sessions |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-010-01 | Initiate charging session via QR code scan (validates charger, links to user/vehicle) | Must |
| FR-010-02 | Send RemoteStartTransaction to charger via OCPP | Must |
| FR-010-03 | Track real-time session data: duration, energy consumed, estimated cost | Must |
| FR-010-04 | Receive and store MeterValues during session | Must |
| FR-010-05 | Stop charging session (user-initiated or system-initiated) | Must |
| FR-010-06 | Calculate final cost based on tariff plan (MOD-007) | Must |
| FR-010-07 | Trigger payment processing after session ends (MOD-008) | Must |
| FR-010-08 | Store complete session record for history and audit | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-010-01 | User must have an active vehicle and valid payment method to start |
| BR-010-02 | Connector must be Available to start session |
| BR-010-03 | Only one active session per user at a time |
| BR-010-04 | Session data persisted immediately for billing accuracy |
| BR-010-05 | If charger stops unexpectedly, session finalized with last known meter values |
| BR-010-06 | Cost = energy consumed × tariff rate (with discount and tax per MOD-007) |
| BR-010-07 | Tariff rate is resolved and snapshotted at session start (RatePerKwh field) |
| BR-010-08 | Cost = TotalEnergyKwh × RatePerKwh (calculated on stop and updated during charging) |
| BR-010-09 | RFID-initiated sessions resolve user via UserIdTag table (MOD-006) |

## 5. Data Model
### ChargingSession (Aggregate Root)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | Auto-generated |
| UserId | Guid | Yes | FK to AppUser |
| VehicleId | Guid | Yes | FK to Vehicle |
| ConnectorId | Guid | Yes | FK to Connector |
| StationId | Guid | Yes | FK to ChargingStation |
| OcppTransactionId | int? | No | OCPP transaction ID |
| Status | SessionStatus (enum) | Yes | Initiated, Charging, Completed, Failed |
| StartTime | DateTime | Yes | Session start |
| EndTime | DateTime? | No | Session end |
| MeterStart | decimal | Yes | Initial meter reading (kWh) |
| MeterEnd | decimal? | No | Final meter reading (kWh) |
| TotalEnergyKwh | decimal? | No | Calculated total |
| TotalCost | decimal? | No | Calculated cost (VNĐ) |
| TariffPlanId | Guid | Yes | Tariff used for billing |
| IdTag | string(50)? | No | OCPP idTag used to start session |
| RatePerKwh | decimal | Yes | Tariff rate snapshot at session start (VNĐ/kWh) |
| StopReason | string? | No | Why session stopped |

### SessionStatus Enum
`Pending = 0, Starting = 1, InProgress = 2, Suspended = 3, Stopping = 4, Completed = 5, Failed = 6`

## 6. API Endpoints (Driver BFF)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/sessions/start | Start session (QR scan data) | Driver |
| POST | /api/v1/sessions/{id}/stop | Stop session | Driver |
| GET | /api/v1/sessions/{id} | Get session detail (real-time) | Driver |
| GET | /api/v1/sessions/active | Get user's active session | Driver |
| GET | /api/v1/sessions/history | User session history (paginated) | Driver |
| GET | /api/v1/admin/sessions | All sessions (admin view) | Admin, Operator |

## 7. Session Flow
```
1. Driver scans QR → app sends connector ID to BFF
2. BFF validates: connector available, user has vehicle + payment method
3. BFF creates ChargingSession (Initiated)
4. CSMS sends RemoteStartTransaction to charger
5. Charger responds → CP sends StartTransaction → session status = Charging
6. CP sends periodic MeterValues → stored, real-time updates via SignalR
7. Driver taps Stop → CSMS sends RemoteStopTransaction
8. CP sends StopTransaction → session finalized (Completed)
9. Cost calculated (MOD-007) → Payment triggered (MOD-008)
10. Invoice generated → Push notification sent
```

### RFID-Initiated Session (Alternative Flow)
```
1. Driver taps RFID card on charger
2. CP → Authorize(idTag: "CARD-12345") → CSMS
3. CSMS looks up UserIdTag table → finds user → Accepted
4. CP → StartTransaction(connectorId, idTag, meterStart) → CSMS
5. CSMS resolves userId from UserIdTag → creates session with UserId + billing
6. CP → MeterValues (periodic) → CSMS stores, updates cost
7. Driver taps card again / CP auto-stops → StopTransaction
8. Session finalized, payment triggered
```

## 8. Real-time Updates
- SignalR hub pushes live session data to mobile app
- Data: current energy (kWh), duration, estimated cost, charging power (kW)
- Update frequency: every MeterValue received (typically 10-30 seconds)

## 9. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_010_001 | Connector not available | 400 |
| MOD_010_002 | User already has active session | 400 |
| MOD_010_003 | No active vehicle selected | 400 |
| MOD_010_004 | No valid payment method | 400 |
| MOD_010_005 | Session not found | 404 |
| MOD_010_006 | Charger not responding | 503 |

## 10. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-010-01 | Complete session: start → charge → stop → pay | Full flow succeeds |
| TC-010-02 | Start on unavailable connector | 400 error |
| TC-010-03 | Start with no vehicle | 400 error |
| TC-010-04 | Charger disconnects during session | Session finalized with last meter values |
| TC-010-05 | Real-time updates via SignalR | Client receives live energy/cost data |
| TC-010-06 | View session history | Paginated list with correct data |
