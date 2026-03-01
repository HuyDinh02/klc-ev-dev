# MOD-002: Connector Management

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Manages individual charging connectors (ports) within a station. Each connector has its own type, power rating, and status. Connectors can be registered, configured, and enabled/disabled remotely.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Register, configure, enable/disable connectors |
| Operator | View connector status, request enable/disable |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-002-01 | Register a new connector for a station with type, power rating, and initial status | Must |
| FR-002-02 | Update connector configuration (max power, availability rules) | Must |
| FR-002-03 | Enable or disable individual connectors remotely | Must |
| FR-002-04 | View all connectors for a station with current status | Must |
| FR-002-05 | View connector detail including power specs and current session info | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-002-01 | Each connector belongs to exactly one station |
| BR-002-02 | Connector with active charging session cannot be disabled |
| BR-002-03 | Connector types: Type2, CCS, CHAdeMO |
| BR-002-04 | Power rating must be positive and within station capacity |
| BR-002-05 | Connector number must be unique within a station |

## 5. Data Model
### Connector (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| StationId | Guid | Yes | FK to ChargingStation |
| ConnectorNumber | int | Yes | Unique within station (1, 2, 3...) |
| ConnectorType | ConnectorType (enum) | Yes | Type2, CCS, CHAdeMO |
| MaxPowerKw | decimal | Yes | Maximum power output in kW |
| Status | ConnectorStatus (enum) | Yes | Available, Occupied, Charging, Faulted, Unavailable |
| IsEnabled | bool | Yes | Admin enable/disable flag |

### ConnectorType Enum
`Type2 = 1, CCS = 2, CHAdeMO = 3`

### ConnectorStatus Enum
`Available = 0, Occupied = 1, Charging = 2, Faulted = 3, Unavailable = 4`

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/stations/{stationId}/connectors | Register connector | Admin |
| GET | /api/v1/stations/{stationId}/connectors | List connectors for station | Admin, Operator |
| GET | /api/v1/connectors/{id} | Get connector detail | Admin, Operator |
| PUT | /api/v1/connectors/{id} | Update connector config | Admin |
| POST | /api/v1/connectors/{id}/enable | Enable connector | Admin |
| POST | /api/v1/connectors/{id}/disable | Disable connector | Admin |

## 7. UI/UX
- Connector list within station detail page
- Status indicators (color-coded by status)
- Enable/disable toggle with confirmation dialog
- Connector type icons (Type2, CCS, CHAdeMO)

## 8. OCPP Messages
| Direction | Message | Relevance |
|-----------|---------|-----------|
| CP → CSMS | StatusNotification | Updates connector status (connectorId maps to ConnectorNumber) |
| CSMS → CP | ChangeAvailability | Enable/disable connector remotely |

## 9. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_002_001 | Station not found | 404 |
| MOD_002_002 | Connector number already exists in station | 409 |
| MOD_002_003 | Cannot disable connector with active session | 400 |
| MOD_002_004 | Invalid power rating | 400 |

## 10. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-002-01 | Register connector with valid data | Connector created, status = Available |
| TC-002-02 | Register duplicate connector number | 409 error |
| TC-002-03 | Disable connector with no session | Connector disabled successfully |
| TC-002-04 | Disable connector with active session | 400 error |
| TC-002-05 | OCPP StatusNotification updates connector | Status reflected in DB and portal |
