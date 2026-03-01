# MOD-004: Energy Metering

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Collects, validates, and aggregates energy consumption data (kWh, voltage, current, power) from chargers during sessions via OCPP MeterValues. Data serves billing calculation and analytics.

## 2. Actors
| Actor | Role |
|-------|------|
| System (OCPP) | Receives and processes meter values automatically |
| Operator | View metering data and reports |
| Admin | View aggregated energy data, configure metering intervals |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-004-01 | Collect meter values (kWh, voltage, current, power) during charging sessions via OCPP | Must |
| FR-004-02 | Validate and normalize metering data from chargers (unit conversion, range checks) | Must |
| FR-004-03 | Aggregate metering data per session for billing calculation | Must |
| FR-004-04 | Aggregate metering data per station/connector for analytics | Should |
| FR-004-05 | Store raw meter values for audit trail | Must |
| FR-004-06 | Calculate total energy consumed (kWh) per session from meter readings | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-004-01 | Meter values must be persisted immediately upon receipt for billing accuracy |
| BR-004-02 | Energy consumed = last meter reading - first meter reading for session |
| BR-004-03 | Invalid meter values (negative, out of range) logged but excluded from billing |
| BR-004-04 | Measurands: Energy.Active.Import.Register (kWh), Current.Import (A), Voltage (V), Power.Active.Import (kW), SoC (%) |
| BR-004-05 | Meter data retained for minimum 12 months |

## 5. Data Model
### MeterValue (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| SessionId | Guid | Yes | FK to ChargingSession |
| ConnectorId | Guid | Yes | FK to Connector |
| Timestamp | DateTime | Yes | Measurement time |
| EnergyKwh | decimal? | No | Cumulative energy (kWh) |
| CurrentAmps | decimal? | No | Current (A) |
| VoltageVolts | decimal? | No | Voltage (V) |
| PowerKw | decimal? | No | Active power (kW) |
| SocPercent | decimal? | No | State of Charge (%) |
| IsValid | bool | Yes | Passed validation |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/sessions/{sessionId}/meter-values | Get meter values for session | Admin, Operator |
| GET | /api/v1/stations/{stationId}/energy-summary | Aggregated energy data for station | Admin, Operator |
| GET | /api/v1/connectors/{connectorId}/energy-summary | Aggregated energy data for connector | Admin, Operator |

## 7. OCPP Messages
| Direction | Message | Relevance |
|-----------|---------|-----------|
| CP → CSMS | MeterValues | Primary source — periodic meter readings during session |
| CP → CSMS | StopTransaction | Contains final meter value for session |
| CP → CSMS | StartTransaction | Contains initial meter value for session |

## 8. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_004_001 | Session not found | 404 |
| MOD_004_002 | Invalid meter value detected | 200 (logged, excluded from billing) |

## 9. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-004-01 | Receive valid MeterValues during session | Values stored, energy calculated |
| TC-004-02 | Receive invalid meter value (negative kWh) | Logged as invalid, excluded from billing |
| TC-004-03 | Session ends with StopTransaction | Final energy calculated from first/last readings |
| TC-004-04 | Query energy summary for station | Correct aggregated totals returned |
