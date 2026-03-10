# Database Design (PostgreSQL)

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. Overview

PostgreSQL is the primary database, chosen for excellent JSON support, ACID compliance, and strong Vietnam hosting ecosystem. EF Core with ABP handles ORM, migrations, and repository patterns.

## 2. Key Entities

### Charging Infrastructure
| Entity | Type | Description |
|--------|------|-------------|
| ChargingStation | Aggregate Root | Physical station with ID, name, location (lat/lng), address, status, firmware info, operating hours |
| Connector | Entity | Individual charging port: connector type (Type2/CCS/CHAdeMO), power rating, status |
| StationGroup | Aggregate Root | Logical grouping by location, region, or operator |

### Charging Operations
| Entity | Type | Description |
|--------|------|-------------|
| ChargingSession | Aggregate Root | Full lifecycle: start → metering → stop → billing. Links user, vehicle, connector |
| MeterValue | Entity | Energy readings: kWh, voltage (V), current (A), power (kW), SoC |
| SessionTransaction | Entity | Financial transaction linked to session |

### User & Vehicle
| Entity | Type | Description |
|--------|------|-------------|
| AppUser | Entity | Mobile app user (extends ABP IdentityUser) |
| Vehicle | Entity | EV: make, model, battery capacity, connector type |
| Wallet | Entity | User's prepaid balance |

### Billing & Payment
| Entity | Type | Description |
|--------|------|-------------|
| TariffPlan | Aggregate Root | Pricing: per kWh, peak/off-peak, time-based, location-based |
| PaymentTransaction | Entity | Payment records: gateway, amount, status, reference |
| Invoice | Entity | E-invoice records linked to sessions |

### Operations
| Entity | Type | Description |
|--------|------|-------------|
| Fault | Entity | Charger errors with error code, timestamp, station reference |
| MaintenanceTicket | Entity | Issue tickets with assignment, status, resolution |
| Notification | Entity | Push notification records |

## 3. Key Relationships

```
ChargingStation 1──N Connector
ChargingStation N──1 StationGroup
ChargingSession N──1 Connector
ChargingSession N──1 AppUser
ChargingSession N──1 Vehicle
ChargingSession 1──N MeterValue
ChargingSession 1──1 SessionTransaction
AppUser 1──N Vehicle
AppUser 1──1 Wallet
TariffPlan 1──N ChargingStation
PaymentTransaction N──1 ChargingSession
Invoice 1──1 PaymentTransaction
Fault N──1 ChargingStation
MaintenanceTicket N──1 Fault
```

## 4. Design Patterns
- Code-first migrations via ABP DbMigrator
- Soft delete via ABP ISoftDelete on key entities
- Indexes on: station status, connector status, session timestamps, user ID, payment status
- Audit columns via ABP FullAuditedAggregateRoot (CreationTime, CreatorId, LastModificationTime, etc.)
- Read replicas for Driver BFF queries

## 5. Phase 2 Entities

The following entities are planned for Phase 2 to support power sharing, dynamic load balancing, fleet management, and operator API access.

### Power Management

| Entity | Type | Fields | Description |
|--------|------|--------|-------------|
| PowerSharingGroup | Aggregate Root | Id (Guid), Name (string), MaxTotalPowerKw (decimal), Strategy (PowerSharingStrategy enum), IsEnabled (bool) | Defines a group of connectors that share a limited power budget. Strategy determines how power is distributed (EqualSplit, PriorityBased, FirstComeFirstServed). |
| PowerSharingGroupMember | Entity | Id (Guid), GroupId (Guid → PowerSharingGroup), StationId (Guid → ChargingStation), ConnectorNumber (int), MinPowerKw (decimal), MaxPowerKw (decimal), Priority (int) | A connector participating in a power sharing group. MinPowerKw guarantees a floor; Priority is used by PriorityBased strategy. |
| SiteLoadProfile | Aggregate Root | Id (Guid), StationGroupId (Guid → StationGroup), GridCapacityKw (decimal), ReservedForOtherLoadsKw (decimal), DistributionStrategy (LoadDistributionStrategy enum), SmartMeterEndpoint (string, nullable), IsEnabled (bool) | Configures dynamic load balancing for an entire site (station group). GridCapacityKw is the site's utility connection limit. ReservedForOtherLoadsKw holds back capacity for non-EV loads. SmartMeterEndpoint is the URL/IP for real-time grid readings. |

### Fleet Management

| Entity | Type | Fields | Description |
|--------|------|--------|-------------|
| Fleet | Aggregate Root | Id (Guid), Name (string), OperatorId (Guid → Operator), MaxMonthlyBudget (decimal, VND), ChargingPolicy (ChargingPolicyType enum), IsActive (bool) | A corporate fleet with budget caps and charging rules. ChargingPolicy controls behavior (Unrestricted, OffPeakOnly, BudgetCapped). |
| FleetVehicle | Entity | Id (Guid), FleetId (Guid → Fleet), VehicleId (Guid → Vehicle), DriverUserId (Guid → AppUser), DailyChargingLimitKwh (decimal), AllowedStationGroupIds (Guid[], nullable) | Links a vehicle and driver to a fleet with per-driver limits and optional station group restrictions. |

### Operator Integration

| Entity | Type | Fields | Description |
|--------|------|--------|-------------|
| Operator | Aggregate Root | Id (Guid), CompanyName (string), ApiKey (string, hashed), ContactEmail (string), IsActive (bool), AllowedStationIds (Guid[]), WebhookUrl (string, nullable), WebhookSecret (string, nullable), RateLimitPerMinute (int) | Third-party operator with API access. AllowedStationIds restricts which stations they can manage. WebhookUrl receives event callbacks (session start/stop, faults). |

### Phase 2 Relationships

```
PowerSharingGroup 1──N PowerSharingGroupMember
PowerSharingGroupMember N──1 ChargingStation
SiteLoadProfile N──1 StationGroup
Fleet N──1 Operator
Fleet 1──N FleetVehicle
FleetVehicle N──1 Vehicle
FleetVehicle N──1 AppUser
Operator 1──N Fleet
```
