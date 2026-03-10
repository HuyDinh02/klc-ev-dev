# MOD-018: Fleet Management

> Status: DRAFT | Priority: Phase 2 | Last Updated: 2026-03-11

## 1. Overview
B2B fleet operator module enabling organizations (taxi companies, delivery services, bus operators) to manage their vehicle fleets, assign drivers, enforce charging policies, track budgets, and generate fleet-level reports. Fleet operators access the system through the Admin Portal with a dedicated fleet role.

## 2. Actors
| Actor | Role |
|-------|------|
| Fleet Operator | Manage fleet vehicles, drivers, policies, budgets; view fleet reports |
| Admin | Approve/suspend fleet operator accounts, configure system-wide fleet settings |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-018-01 | CRUD operations for fleet organizations (name, tax code, contact, billing info) | Must |
| FR-018-02 | Assign/unassign vehicles to a fleet with bulk import (CSV) support | Must |
| FR-018-03 | Assign/unassign drivers to a fleet with role-based access | Must |
| FR-018-04 | Define fleet charging policies (allowed stations, time windows, max kWh per session, max sessions per day) | Must |
| FR-018-05 | Track fleet charging budget with monthly limits and alerts at configurable thresholds (50%, 80%, 100%) | Must |
| FR-018-06 | Generate fleet reports: total energy consumed, cost breakdown, per-driver usage, per-vehicle usage, station utilization | Must |
| FR-018-07 | Fleet dashboard showing active sessions, daily/monthly statistics, budget consumption | Should |
| FR-018-08 | Fleet-level invoicing: consolidated monthly invoice per fleet with detailed line items | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-018-01 | A driver can belong to at most one fleet at a time |
| BR-018-02 | A vehicle can belong to at most one fleet at a time |
| BR-018-03 | When monthly budget is reached (100%), new charging sessions for the fleet are blocked unless overridden by Admin |
| BR-018-04 | Fleet charging policies are enforced at session start — violations are rejected with a descriptive error |
| BR-018-05 | Fleet operator can only view data for their own fleet (data isolation) |
| BR-018-06 | Removing a driver from a fleet does not delete the user account; it revokes fleet association |
| BR-018-07 | Fleet reports retain historical data even after vehicles/drivers are removed from the fleet |
| BR-018-08 | Budget alerts are sent via push notification and email to fleet operator contacts |

## 5. Data Model
### Fleet (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| Name | string(200) | Organization name |
| TaxCode | string(20) | Business tax code |
| ContactName | string(100) | Primary contact person |
| ContactEmail | string(100) | Contact email |
| ContactPhone | string(20) | Contact phone |
| BillingAddress | string(500) | Invoice address |
| IsActive | bool | Whether fleet is active |

### FleetVehicle (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| FleetId | Guid | FK to Fleet |
| VehicleId | Guid | FK to Vehicle |
| AssignedAt | DateTime | When vehicle was assigned |

### FleetDriver (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| FleetId | Guid | FK to Fleet |
| UserId | Guid | FK to AppUser |
| Role | FleetDriverRole | Driver, Manager |
| AssignedAt | DateTime | When driver was assigned |

### FleetChargingPolicy (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| FleetId | Guid | FK to Fleet |
| AllowedStationGroupIds | string(JSON) | List of allowed station group IDs (null = all) |
| AllowedTimeWindowStart | TimeOnly? | Earliest allowed charging time |
| AllowedTimeWindowEnd | TimeOnly? | Latest allowed charging time |
| MaxKwhPerSession | decimal? | Max energy per session (null = unlimited) |
| MaxSessionsPerDay | int? | Max sessions per driver per day (null = unlimited) |

### FleetBudget (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| FleetId | Guid | FK to Fleet |
| Month | int | Budget month (1-12) |
| Year | int | Budget year |
| MonthlyLimitVnd | decimal | Monthly budget limit (VND) |
| ConsumedVnd | decimal | Amount consumed so far |
| AlertThresholds | string(JSON) | [50, 80, 100] percentages |

## 6. Policy Enforcement Flow
```
1. Driver initiates charging session (via app or RFID)
2. System resolves driver → fleet association
3. If driver belongs to a fleet:
   a. Check station is in allowed station groups
   b. Check current time is within allowed time window
   c. Check driver has not exceeded max sessions per day
   d. Check fleet budget has not reached 100%
4. If any check fails → reject session with specific error code
5. If all checks pass → start session normally
6. On session completion → update fleet budget consumed amount
7. If budget threshold crossed → send alert notification
```

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_018_001 | Fleet not found | 404 |
| MOD_018_002 | Driver already belongs to another fleet | 400 |
| MOD_018_003 | Vehicle already belongs to another fleet | 400 |
| MOD_018_004 | Fleet monthly budget exceeded — charging blocked | 403 |
| MOD_018_005 | Station not in fleet allowed stations | 403 |
| MOD_018_006 | Charging outside fleet allowed time window | 403 |
| MOD_018_007 | Driver daily session limit reached | 403 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-018-01 | Create fleet, assign 5 vehicles and 3 drivers | Fleet created; all assignments successful |
| TC-018-02 | Assign driver already in another fleet | Rejected with MOD_018_002 error |
| TC-018-03 | Driver starts session at allowed station within policy | Session starts successfully |
| TC-018-04 | Driver starts session at station not in allowed list | Rejected with MOD_018_005 error |
| TC-018-05 | Driver starts session outside allowed time window | Rejected with MOD_018_006 error |
| TC-018-06 | Fleet budget reaches 100% during active sessions | Current sessions complete; new sessions blocked with MOD_018_004 |
| TC-018-07 | Budget crosses 80% threshold | Alert notification sent to fleet operator |
| TC-018-08 | Generate monthly fleet report | Report contains per-driver, per-vehicle, and station-level breakdown |
