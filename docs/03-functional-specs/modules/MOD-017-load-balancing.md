# MOD-017: Dynamic Load Balancing

> Status: DRAFT | Priority: Phase 2 | Last Updated: 2026-03-11

## 1. Overview
Site-level dynamic load balancing module that prevents grid overload by managing total power consumption across all chargers at a physical site. Integrates with smart meters to read real-time site load, reserves capacity for non-EV building loads, and automatically curtails or redistributes charger power to stay within grid connection limits.

## 2. Actors
| Actor | Role |
|-------|------|
| Admin | Configure site capacity, non-EV reserve, distribution strategy, smart meter settings |
| System (CSMS) | Monitor site load, auto-balance charger power, respond to grid constraints |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-017-01 | Configure site with grid connection capacity (kW), non-EV reserve (kW), and safety margin (%) | Must |
| FR-017-02 | Integrate with smart meters via Modbus TCP or OCPP MeterValues to read real-time site consumption | Must |
| FR-017-03 | Calculate available EV capacity: GridCapacity - NonEvReserve - SafetyMargin - CurrentNonEvLoad | Must |
| FR-017-04 | Distribute available EV capacity across active sessions using configurable strategy (equal, priority, first-come-first-served) | Must |
| FR-017-05 | Push updated ChargingProfiles to affected chargers when available capacity changes | Must |
| FR-017-06 | Real-time site dashboard showing grid capacity, current load breakdown (EV vs non-EV), and per-charger allocation | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-017-01 | Total charger power draw must never exceed grid connection capacity minus non-EV reserve minus safety margin |
| BR-017-02 | Non-EV reserve must always be maintained; EV charging is curtailed first during high building load |
| BR-017-03 | System must detect load changes and push updated ChargingProfiles within 10 seconds |
| BR-017-04 | If smart meter connection is lost, system falls back to configured static limits (fail-safe) |
| BR-017-05 | Each charger retains a minimum power floor (configurable, default 6 kW) even during curtailment |
| BR-017-06 | Load balancing operates per site; a site maps to one or more station groups |
| BR-017-07 | When available capacity increases (e.g., building load drops), chargers are ramped back up automatically |

## 5. Data Model
### SiteLoadConfig (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| Name | string(100) | Site display name |
| StationGroupId | Guid | FK to StationGroup (site boundary) |
| GridCapacityKw | decimal | Maximum grid connection (kW) |
| NonEvReserveKw | decimal | Reserved for non-EV loads (kW) |
| SafetyMarginPercent | decimal | Safety margin percentage (default 10%) |
| Strategy | LoadBalancingStrategy | Equal, Priority, FirstComeFirstServed |
| MinPowerPerChargerKw | decimal | Minimum guaranteed per charger (default 6 kW) |
| SmartMeterEndpoint | string(200)? | Modbus TCP address or null for static mode |
| IsActive | bool | Whether load balancing is active |

### SiteLoadReading (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| SiteLoadConfigId | Guid | FK to SiteLoadConfig |
| TotalSiteLoadKw | decimal | Total site consumption reading |
| EvLoadKw | decimal | EV charger portion |
| NonEvLoadKw | decimal | Non-EV portion |
| AvailableEvCapacityKw | decimal | Calculated available for EV |
| Timestamp | DateTime | Reading time |

### LoadBalancingEvent (Entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Auto-generated |
| SiteLoadConfigId | Guid | FK to SiteLoadConfig |
| TriggerType | string | MeterReading, SessionStart, SessionStop, SmartMeterLost |
| PreviousAllocationsJson | string(JSON) | Allocations before adjustment |
| NewAllocationsJson | string(JSON) | Allocations after adjustment |
| Timestamp | DateTime | When adjustment occurred |

## 6. Load Balancing Flow
```
1. Smart meter reading received (or session start/stop event)
2. Calculate current non-EV load = TotalSiteLoad - EvLoad
3. AvailableEvCapacity = GridCapacity - NonEvReserve - (GridCapacity * SafetyMargin%) - NonEvLoad
4. If AvailableEvCapacity < sum of current EV allocations → curtail
5. If AvailableEvCapacity > sum of current EV allocations → ramp up
6. Distribute AvailableEvCapacity per strategy, respecting MinPowerPerCharger
7. Push SetChargingProfile to chargers with changed allocations
8. Log event and notify MonitoringHub
```

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_017_001 | Site load configuration not found | 404 |
| MOD_017_002 | Smart meter connection lost — falling back to static limits | Warning (logged) |
| MOD_017_003 | Grid capacity exceeded — emergency curtailment triggered | Warning (logged) |
| MOD_017_004 | Invalid grid capacity configuration (reserve exceeds capacity) | 400 |
| MOD_017_005 | Failed to push ChargingProfile during load adjustment | 503 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-017-01 | Site at 50% building load, 3 chargers active | EV capacity correctly calculated; power distributed per strategy |
| TC-017-02 | Building load spikes from 30% to 80% of grid capacity | EV chargers curtailed within 10 seconds to maintain grid limit |
| TC-017-03 | Building load drops back to 30% | EV chargers ramped back up to utilize available capacity |
| TC-017-04 | Smart meter connection lost | System falls back to static limits; warning logged |
| TC-017-05 | New session starts when site is at full capacity | Session receives minimum guaranteed power; other sessions reduced proportionally |
| TC-017-06 | Configure non-EV reserve greater than grid capacity | Rejected with MOD_017_004 validation error |
