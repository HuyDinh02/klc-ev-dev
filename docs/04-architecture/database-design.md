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
