# MOD-001: Station Management

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Manages the lifecycle of charging stations: registration, configuration, updates, and decommissioning. Each station has a unique identity, physical location, and operational metadata.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Create, update, decommission stations |
| Operator | View station details, monitor status |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-001-01 | Create a new charging station with unique ID, name, location (lat/lng), address, description, and metadata | Must |
| FR-001-02 | Update station information: name, location, description, operating hours, assigned pricing model | Must |
| FR-001-03 | Decommission a station (mark as inactive/retired) without deleting historical data | Must |
| FR-001-04 | View list of all stations with filtering by status, region, and search by name | Must |
| FR-001-05 | View detailed station profile including connectors, current status, and operational info | Must |
| FR-001-06 | Assign a tariff/pricing plan to a station | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-001-01 | Station ID must be unique across the system |
| BR-001-02 | Decommissioned stations retain all historical data (sessions, faults, meter values) |
| BR-001-03 | Station must have at least one connector before becoming Available |
| BR-001-04 | Station location (lat/lng) is required for map display in mobile app |
| BR-001-05 | Only Admin role can create or decommission stations |

## 5. Data Model
### ChargingStation (Aggregate Root)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | Guid | Yes | ABP auto-generated |
| StationCode | string(50) | Yes | Unique station identifier |
| Name | string(200) | Yes | Display name |
| Description | string(2000) | No | Station description |
| Latitude | decimal | Yes | GPS latitude |
| Longitude | decimal | Yes | GPS longitude |
| Address | string(500) | Yes | Physical address |
| Status | StationStatus (enum) | Yes | Offline, Available, Charging, Faulted, Decommissioned |
| OperatingHoursStart | TimeOnly | No | Daily operating start |
| OperatingHoursEnd | TimeOnly | No | Daily operating end |
| TariffPlanId | Guid? | No | Assigned pricing plan |
| FirmwareVersion | string(100) | No | Reported firmware version |

### StationStatus Enum
`Offline = 0, Available = 1, Charging = 2, Faulted = 3, Decommissioned = 4`

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/stations | Create new station | Admin |
| GET | /api/v1/stations | List stations (filtered, paginated) | Admin, Operator |
| GET | /api/v1/stations/{id} | Get station detail | Admin, Operator |
| PUT | /api/v1/stations/{id} | Update station info | Admin |
| POST | /api/v1/stations/{id}/decommission | Decommission station | Admin |

## 7. UI/UX
- Station list page with status indicators (color-coded)
- Station detail page with map, connectors list, and status timeline
- Create/edit form with location picker (Google Maps integration)

## 8. OCPP Messages
| Direction | Message | Relevance |
|-----------|---------|-----------|
| CP → CSMS | BootNotification | Registers station on first connect, updates firmware version |
| CP → CSMS | StatusNotification | Updates station/connector status |

## 9. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_001_001 | Station code already exists | 409 Conflict |
| MOD_001_002 | Station not found | 404 Not Found |
| MOD_001_003 | Cannot decommission station with active sessions | 400 Bad Request |
| MOD_001_004 | Invalid location coordinates | 400 Bad Request |

## 10. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-001-01 | Create station with valid data | Station created, status = Offline |
| TC-001-02 | Create station with duplicate code | 409 error returned |
| TC-001-03 | Decommission station with no active sessions | Status → Decommissioned |
| TC-001-04 | Decommission station with active session | 400 error returned |
| TC-001-05 | Update station location | Coordinates updated, map reflects change |
| TC-001-06 | Filter stations by status | Correct filtered results |
