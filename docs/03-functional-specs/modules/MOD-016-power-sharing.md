# MOD-016: Power Sharing (LINK & LOOP)

> Status: DRAFT | Priority: Phase 2 | Last Updated: 2026-03-11

## 1. Overview
Power sharing module enabling groups of chargers that share a common electrical supply to dynamically distribute available power. Supports both LINK (static allocation) and LOOP (dynamic rebalancing) strategies, ensuring no group exceeds its maximum power pool while maximizing utilization across active sessions.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Create/manage power sharing groups, configure strategies and limits |
| System (CSMS) | Auto-rebalance power on session start/stop, push ChargingProfiles to chargers |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-016-01 | CRUD operations for power sharing groups (name, max power kW, strategy) | Must |
| FR-016-02 | Add/remove charger members to a power sharing group | Must |
| FR-016-03 | Set distribution strategy per group: LINK (equal static split) or LOOP (dynamic priority-based) | Must |
| FR-016-04 | Auto-rebalance power allocation when a session starts or stops within the group | Must |
| FR-016-05 | Push OCPP SetChargingProfile to each member charger after rebalance calculation | Must |
| FR-016-06 | Real-time monitoring dashboard showing group power usage, per-member allocation, and headroom | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-016-01 | Sum of power allocated across all active members must never exceed the group max power (kW) |
| BR-016-02 | Each member has a configurable minimum guaranteed power (default: 6 kW / ~1.4 kW single-phase minimum) |
| BR-016-03 | Rebalance calculation must complete and ChargingProfiles must be pushed within 5 seconds of trigger event |
| BR-016-04 | A power sharing group may contain a maximum of 10 charger members |
| BR-016-05 | A charger can belong to at most one power sharing group at a time |
| BR-016-06 | When a member charger goes offline, its allocated power is redistributed to remaining active members |
| BR-016-07 | LINK strategy: available power divided equally among active sessions |
| BR-016-08 | LOOP strategy: power allocated proportionally based on session priority (first-come or SoC-based) |

## 5. Data Model
### PowerSharingGroup (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| Name | string(100) | Group display name |
| MaxPowerKw | decimal | Maximum power pool for the group (kW) |
| Strategy | PowerSharingStrategy | Link, Loop |
| MinGuaranteedPerMemberKw | decimal | Minimum power per member (default 6 kW) |
| IsActive | bool | Whether group is active |

### PowerSharingMember (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| GroupId | Guid | FK to PowerSharingGroup |
| StationId | Guid | FK to ChargingStation |
| ConnectorId | Guid | FK to Connector |
| AllocatedPowerKw | decimal | Currently allocated power (kW) |
| Priority | int | Priority order within group (lower = higher priority) |

### PowerSharingLog (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| GroupId | Guid | FK to PowerSharingGroup |
| TriggerEvent | string | SessionStart, SessionStop, MemberOffline |
| AllocationsJson | string(JSON) | Snapshot of allocations after rebalance |
| Timestamp | DateTime | When rebalance occurred |

## 6. Rebalance Flow
```
1. Session starts/stops on a member charger → trigger rebalance
2. System queries all active sessions within the group
3. Calculate allocation per strategy:
   - LINK: MaxPowerKw / ActiveSessionCount (floor to MinGuaranteed)
   - LOOP: Proportional by priority weight, respecting MinGuaranteed
4. For each member with changed allocation → OCPP SetChargingProfile
5. Log rebalance event with allocation snapshot
6. Notify MonitoringHub with updated group status
```

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_016_001 | Power sharing group not found | 404 |
| MOD_016_002 | Group has reached maximum member count (10) | 400 |
| MOD_016_003 | Charger already belongs to another power sharing group | 400 |
| MOD_016_004 | Allocated power exceeds group maximum | 400 |
| MOD_016_005 | Failed to push ChargingProfile to member charger | 503 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-016-01 | Create power sharing group with 3 members, start 1 session | Full group power allocated to single active session (capped at charger max) |
| TC-016-02 | Second session starts in LINK group with 2 active | Power split equally between both sessions |
| TC-016-03 | Session stops in 3-member LOOP group | Remaining sessions receive redistributed power within 5 seconds |
| TC-016-04 | Add 11th member to group | Rejected with MOD_016_002 error |
| TC-016-05 | Member charger goes offline during active sharing | Offline member power redistributed; ChargingProfiles pushed to remaining members |
| TC-016-06 | All sessions end in group | All allocations reset to zero; no ChargingProfiles pushed |
