# MOD-005: Fault Detection & Management

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Detects and logs charger faults and error codes reported via OCPP. Stores fault events with timestamps and station references for tracking, troubleshooting, and maintenance coordination.

## 2. Actors
| Actor | Role |
|-------|------|
| System (OCPP) | Auto-detects faults from charger messages |
| Technical Support | View faults, diagnose, manage resolution |
| Operator | View fault summary and status |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-005-01 | Detect charger faults from OCPP StatusNotification (Faulted status) and error codes | Must |
| FR-005-02 | Log fault events with timestamp, station, connector, error code, and description | Must |
| FR-005-03 | Display fault list with filtering by station, severity, status, date range | Must |
| FR-005-04 | Link faults to maintenance tickets (MOD-005 → maintenance workflow) | Should |
| FR-005-05 | Track fault resolution status (Open, Investigating, Resolved) | Must |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-005-01 | Every Faulted StatusNotification creates a Fault record |
| BR-005-02 | Fault automatically triggers an Alert (see MOD-003) |
| BR-005-03 | Fault records retained for minimum 12 months |
| BR-005-04 | Duplicate fault detection: suppress repeated identical faults within 5-minute window |

## 5. Data Model
### Fault (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| StationId | Guid | Yes | FK to ChargingStation |
| ConnectorId | Guid? | No | FK to Connector |
| ErrorCode | string(50) | Yes | OCPP error code |
| ErrorDescription | string(500) | No | Human-readable description |
| OcppVendorId | string(255) | No | Vendor-specific ID |
| OcppVendorErrorCode | string(50) | No | Vendor error code |
| Status | FaultStatus (enum) | Yes | Open, Investigating, Resolved |
| DetectedAt | DateTime | Yes | When fault was detected |
| ResolvedAt | DateTime? | No | When fault was resolved |
| MaintenanceTicketId | Guid? | No | FK to linked ticket |

### FaultStatus Enum
`Open = 0, Investigating = 1, Resolved = 2`

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/faults | List faults (filtered, paginated) | Admin, Operator, TechSupport |
| GET | /api/v1/faults/{id} | Fault detail | Admin, TechSupport |
| PUT | /api/v1/faults/{id}/status | Update fault status | TechSupport |
| GET | /api/v1/stations/{stationId}/faults | Faults for a station | Admin, Operator |

## 7. OCPP Messages
| Direction | Message | Relevance |
|-----------|---------|-----------|
| CP → CSMS | StatusNotification | errorCode field triggers fault detection |
| CSMS → CP | Reset | May be used to recover from faults |
| CSMS → CP | TriggerMessage | Request status to verify fault resolution |

## 8. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_005_001 | Fault not found | 404 |
| MOD_005_002 | Invalid status transition | 400 |

## 9. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-005-01 | OCPP Faulted StatusNotification received | Fault created, Alert triggered |
| TC-005-02 | Duplicate fault within 5 minutes | Suppressed (no duplicate record) |
| TC-005-03 | Update fault status to Resolved | Status updated, ResolvedAt set |
| TC-005-04 | Query faults for date range | Correct filtered results |
| TC-005-05 | Link fault to maintenance ticket | Ticket reference saved |
