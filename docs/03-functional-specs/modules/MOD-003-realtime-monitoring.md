# MOD-003: Real-time Monitoring

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Provides real-time visibility into charging station and connector status via OCPP protocol. Includes live status updates, historical status logging, and alert generation for abnormal conditions.

## 2. Actors
| Actor | Role |
|-------|------|
| Operator | Monitor real-time station status dashboard |
| Technical Support | Receive alerts, investigate abnormal conditions |
| Admin | View monitoring dashboards and configure alert rules |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-003-01 | Display real-time status of all stations/connectors (Available, Charging, Faulted, Offline) | Must |
| FR-003-02 | Receive and process OCPP StatusNotification messages to update status | Must |
| FR-003-03 | Store historical status changes with timestamps for audit and reporting | Must |
| FR-003-04 | Auto-detect abnormal conditions (Faulted status, unexpected offline) and generate alerts | Must |
| FR-003-05 | Notify Technical Support when alerts are generated | Must |
| FR-003-06 | Record alert history for tracking and resolution | Must |
| FR-003-07 | Real-time dashboard with station map view and status overview | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-003-01 | Status updates must reflect within 10 seconds of actual change |
| BR-003-02 | If no Heartbeat received within configured timeout, mark station as Offline |
| BR-003-03 | Faulted status always generates an alert |
| BR-003-04 | Historical status logs retained for minimum 12 months |

## 5. Data Model
### StatusChangeLog (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| StationId | Guid | Yes | FK to ChargingStation |
| ConnectorId | Guid? | No | FK to Connector (null = station-level) |
| PreviousStatus | string | Yes | Status before change |
| NewStatus | string | Yes | Status after change |
| Timestamp | DateTime | Yes | When change occurred |
| Source | string | Yes | OCPP, Manual, System |

### Alert (Entity)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| StationId | Guid | Yes | FK |
| ConnectorId | Guid? | No | FK |
| AlertType | AlertType (enum) | Yes | Faulted, Offline, Abnormal |
| Message | string(500) | Yes | Alert description |
| Status | AlertStatus (enum) | Yes | New, Acknowledged, Resolved |
| CreatedAt | DateTime | Yes | When alert generated |
| ResolvedAt | DateTime? | No | When resolved |

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/monitoring/dashboard | Real-time status overview | Admin, Operator |
| GET | /api/v1/monitoring/stations/{id}/status-history | Status change history | Admin, Operator |
| GET | /api/v1/alerts | List alerts (filtered) | Admin, Operator, TechSupport |
| PUT | /api/v1/alerts/{id}/acknowledge | Acknowledge alert | TechSupport |
| PUT | /api/v1/alerts/{id}/resolve | Resolve alert | TechSupport |

## 7. UI/UX
- Dashboard with map view showing all stations color-coded by status
- Station status cards with real-time updates via SignalR
- Alert panel with notification badge and alert list
- Status timeline/history chart per station

## 8. OCPP Messages
| Direction | Message | Relevance |
|-----------|---------|-----------|
| CP → CSMS | StatusNotification | Primary source of status updates |
| CP → CSMS | Heartbeat | Used to detect offline stations (timeout = offline) |
| CSMS → CP | TriggerMessage | Request StatusNotification from charger |

## 9. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_003_001 | Station not found | 404 |
| MOD_003_002 | Alert not found | 404 |
| MOD_003_003 | Alert already resolved | 400 |

## 10. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-003-01 | OCPP StatusNotification received | Status updated in DB, dashboard reflects change |
| TC-003-02 | Faulted status received | Alert auto-generated, TechSupport notified |
| TC-003-03 | No heartbeat for timeout period | Station marked Offline, alert generated |
| TC-003-04 | Acknowledge and resolve alert | Alert status transitions correctly |
| TC-003-05 | Query status history for date range | Correct historical data returned |
