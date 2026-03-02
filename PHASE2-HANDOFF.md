# Phase 2 Handoff — Claude Code Can Start

> **Generated:** 2026-03-01 | **From:** Cowork (documentation phase) → **To:** Claude Code (implementation phase)

---

## 1. Project Summary

**Project:** EV Charging Station Management System (CSMS) for KLC
**Client:** KLC | **Developer:** EmeSoft
**Timeline:** March 1 → June 1, 2026 (MVP Go-live) — 4 months
**CTO/Tech Lead:** Hung (hung.nguyen@emesoft.net)

A unified B2C platform to manage and operate EV charging stations in Vietnam, consisting of a CSMS backend (OCPP 1.6J), Admin portal (React/Next.js), and Driver mobile app (React Native).

---

## 2. What's Done (Phase 0 + Phase 1)

### Phase 0 — Repo & Structure ✅
- Git repo initialized with `.gitignore`
- `CLAUDE.md` — Auto-loaded AI agent context (tech stack, conventions, commands)
- `AGENTS.md` — 5 agent roles (Architect, Backend, Mobile, OCPP, QA) with read/write scopes
- `docs/` — 9-layer documentation structure (66 markdown files)
- `memory-bank/` — 10 condensed context files for AI agents

### Phase 1 — Documentation ✅ (All Complete)
Every layer is fully populated with real content — no placeholder TODOs remain (except 4 link stubs for wireframes/OpenAPI specs that will be generated during implementation).

| Layer | Path | Files | Content |
|-------|------|-------|---------|
| 01 | `docs/01-business/` | 4 | BRD, stakeholder analysis, market context |
| 02 | `docs/02-requirements/` | 5 | Functional requirements (31 admin + 10 mobile + 4 AI), NFRs (20), integration requirements, 45 user stories |
| 03 | `docs/03-functional-specs/modules/` | 15 | MOD-001 through MOD-015, each with 10 sections (overview, actors, FRs, business rules, data model, API, UI, OCPP, errors, tests) |
| 04 | `docs/04-architecture/` | 8 | System overview, backend (ABP DDD), database, OCPP, mobile, deployment, security |
| 05 | `docs/05-decisions/` | 6 | 5 ADRs: ABP Framework, PostgreSQL, OCPP 1.6J, Modular Monolith, CQRS+MediatR |
| 06 | `docs/06-project-management/` | 4 | Project plan (8 milestones), team structure, risk register (8 risks) |
| 07 | `docs/07-testing/` | 4 | Test strategy, test plan, OCPP test scenarios (36+ scenarios) |
| 08 | `docs/08-guides/` | 5 | Dev setup, coding conventions, deployment guide, API guide |
| 09 | `docs/09-ai-playbook/` | 7 | Master rules, patterns, anti-patterns, debug playbooks, lessons learned, prompts |

---

## 3. Tech Stack (Implementation Reference)

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 10, C# 13, ABP Framework (DDD layered architecture) |
| **Admin API** | Full ABP stack, port 5000 |
| **Driver BFF** | .NET Minimal API, Redis cache-first, read replicas, port 5001 |
| **Database** | PostgreSQL (EF Core + ABP), code-first migrations |
| **Cache** | Redis (ElastiCache) |
| **CQRS** | MediatR — IQuery\<T\>/ICommand handlers |
| **OCPP** | 1.6J (JSON over WebSocket), OCPP.Core library |
| **Real-time** | SignalR (portal/app updates) |
| **Admin Portal** | React.js + Next.js + TailwindCSS |
| **Mobile App** | React Native (Expo), TypeScript |
| **Auth** | ABP Identity + OpenIddict (JWT Bearer) |
| **Cloud** | AWS: Docker, ALB, RDS, ElastiCache, CloudWatch |
| **CI/CD** | GitHub Actions |
| **Payments** | ZaloPay, MoMo, OnePay |
| **E-Invoice** | MISA, Viettel, VNPT |

---

## 4. Architecture Overview

### Dual API Design
```
┌────────────────┐     ┌─────────────────────────┐
│  Admin Portal   │────▶│  Admin API (port 5000)   │
│  (React/Next)   │     │  Full ABP DDD layers     │
└────────────────┘     └──────────┬──────────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │    Shared Domain Layer      │
                    │  Entities, Domain Services  │
                    │  Repos, Domain Events       │
                    └─────────────▲──────────────┘
                                  │
┌────────────────┐     ┌──────────┴──────────────┐     ┌─────────────┐
│  Mobile App     │────▶│  Driver BFF (port 5001)  │────▶│ Redis Cache  │
│  (React Native) │     │  Minimal API, cache-first│     └─────────────┘
└────────────────┘     └──────────────────────────┘

┌────────────────┐
│  Charge Points  │───WebSocket (OCPP 1.6J)───▶ CSMS WebSocket Server
│  (Hardware)     │                              ws://host/ocpp/{cpId}
└────────────────┘
```

### ABP Project Structure (Target)
```
src/
├── KLC.Domain.Shared/        # Enums, constants, shared DTOs
├── KLC.Domain/                # Entities, domain services, repo interfaces
├── KLC.Application.Contracts/ # DTOs, service interfaces
├── KLC.Application/           # Service implementations, MediatR handlers
├── KLC.EntityFrameworkCore/   # DbContext, repos, migrations
├── KLC.HttpApi/               # API controllers
├── KLC.HttpApi.Host/          # Admin API host (port 5000)
├── KLC.Driver.BFF/            # Driver BFF host (port 5001)
└── KLC.DbMigrator/            # Migration tool
```

---

## 5. Data Model (Key Entities)

### Aggregate Roots (FullAuditedAggregateRoot\<Guid\>)
- **ChargingStation** — StationCode, Name, Location (lat/lng), Address, Status, FirmwareVersion, TariffPlanId
- **ChargingSession** — UserId, VehicleId, ConnectorId, StationId, OcppTransactionId, Status, StartTime, EndTime, MeterStart, MeterEnd, TotalEnergyKwh, TotalCost
- **TariffPlan** — Name, BaseRatePerKwh, TaxRatePercent, EffectiveFrom, EffectiveTo
- **StationGroup** — Name, Description, Region

### Entities
- **Connector** — StationId, ConnectorNumber, ConnectorType (Type2/CCS/CHAdeMO), MaxPowerKw, Status, IsEnabled
- **Vehicle** — UserId, Make, Model, LicensePlate, BatteryCapacityKwh, PreferredConnectorType
- **MeterValue** — SessionId, ConnectorId, Timestamp, EnergyKwh, CurrentAmps, VoltageVolts, PowerKw, SocPercent
- **Fault** — StationId, ConnectorId, ErrorCode, Status (Open/Investigating/Resolved)
- **PaymentTransaction** — SessionId, UserId, Gateway, Amount, Status, GatewayTransactionId
- **Invoice** — PaymentTransactionId, InvoiceNumber, EnergyKwh, BaseAmount, TaxAmount, TotalAmount
- **EInvoice** — InvoiceId, Provider (MISA/Viettel/VNPT), ExternalInvoiceId, Status
- **Notification**, **Alert**, **StatusChangeLog**, **AppUser** (extends ABP IdentityUser)

### Relationships
```
Station 1──N Connector
Station N──1 StationGroup
Session N──1 Connector, User, Vehicle
Session 1──N MeterValue
Session 1──1 PaymentTransaction
Payment 1──1 Invoice 1──1 EInvoice
User 1──N Vehicle
Fault N──1 Station
```

---

## 6. Phase 2 Tasks — Scaffold & Infrastructure

This is what Claude Code should implement next, in order:

### Step 2.1 — ABP Solution Scaffold
```bash
# Install ABP CLI if not present
dotnet tool install -g Volo.Abp.Cli

# Create solution from ABP template
abp new KLC -t app --no-ui -dbms PostgreSQL --connection-string "Host=localhost;Port=5432;Database=KLC;Username=postgres;Password=postgres"
```
- Rename/restructure projects to match the target structure in Section 4
- Add `KLC.Driver.BFF` project (Minimal API)
- Configure dual API hosting (Admin on 5000, BFF on 5001)
- Add MediatR, AutoMapper, Serilog NuGet packages

### Step 2.2 — Docker Compose
Create `docker-compose.yml` with:
- PostgreSQL 16 (port 5432, volume mount)
- Redis 7 (port 6379)
- pgAdmin (optional, port 8080)
- Health checks for both services

### Step 2.3 — Domain Entities
Implement all entities from Section 5 in `KLC.Domain/`:
- Follow ABP conventions: `FullAuditedAggregateRoot<Guid>` for aggregate roots
- Add enums in `KLC.Domain.Shared/`: StationStatus, ConnectorStatus, ConnectorType, SessionStatus, FaultStatus, PaymentStatus, AlertType, EInvoiceProvider, EInvoiceStatus
- Add domain events for key state changes
- Enforce business rules in entity constructors/methods (DDD principle)

### Step 2.4 — EF Core DbContext & Migrations
In `KLC.EntityFrameworkCore/`:
- Configure `KLCDbContext` with all entity mappings
- Add proper indexes (StationCode, OcppTransactionId, session lookups)
- Configure soft delete, audit fields
- Create initial migration
- Run via `KLC.DbMigrator`

### Step 2.5 — OCPP WebSocket Server
Create OCPP 1.6J WebSocket endpoint:
- Listen at `ws://host/ocpp/{chargePointId}`
- Handle BootNotification, Heartbeat, StatusNotification
- Strongly-typed OCPP message models
- Connection management (track connected chargers)
- Heartbeat timeout → mark station Offline
- Integration with domain layer (update station/connector status)

---

## 7. Key Conventions (MUST Follow)

| Convention | Rule |
|-----------|------|
| Entity base | `FullAuditedAggregateRoot<Guid>` for aggregates, `FullAuditedEntity<Guid>` for entities |
| CQRS | `IQuery<T>` / `ICommand` with MediatR handlers |
| Cache key | `"entity:{id}:field"` (e.g., `"station:123:status"`) |
| API error | `{ "code": "MOD_001", "message": "...", "details": {} }` |
| Pagination | Cursor-based (never offset) |
| Date format | dd/MM/yyyy display, ISO 8601 storage, UTC+7 timezone |
| Currency | VNĐ with dấu chấm separator (9.900đ) |
| Localization | All UI strings via `IStringLocalizer` (VI default + EN) |
| API versioning | `/api/v1/` prefix |
| Auth | Bearer JWT (ABP Identity + OpenIddict) |

---

## 8. Workflow for Claude Code

Before implementing ANY feature:
1. Read `CLAUDE.md` (auto-loaded)
2. Read relevant `memory-bank/` file for condensed context
3. Read the FRS module: `docs/03-functional-specs/modules/MOD-{NNN}.md`
4. Read rules: `docs/09-ai-playbook/rules/_master-rules.md`
5. Check anti-patterns: `docs/09-ai-playbook/anti-patterns/_index.md`
6. Implement
7. Run tests (`dotnet test`)
8. If error → check `docs/09-ai-playbook/debug-playbooks/_index.md`
9. If new lesson → create `LL-NNN` in `docs/09-ai-playbook/lessons-learned/`
10. Commit with conventional message

---

## 9. Key Files to Read First

| Priority | File | Why |
|----------|------|-----|
| 1 | `CLAUDE.md` | Auto-loaded context, commands, conventions |
| 2 | `memory-bank/progress.md` | Current status, what's next |
| 3 | `memory-bank/architecture.md` | Dual API design, project structure |
| 4 | `memory-bank/tech-stack.md` | All technologies and versions |
| 5 | `memory-bank/data-model.md` | All entities and relationships |
| 6 | `memory-bank/ocpp-flows.md` | OCPP message flows and rules |
| 7 | `docs/09-ai-playbook/rules/_master-rules.md` | Coding rules (MUST follow) |
| 8 | `docs/04-architecture/backend-architecture.md` | Detailed ABP/DDD architecture |
| 9 | `docs/04-architecture/database-design.md` | DB schema design and patterns |
| 10 | `docs/08-guides/dev-setup.md` | Development environment setup |

---

## 10. 15 Modules Reference

Each module has a complete FRS in `docs/03-functional-specs/modules/`:

| Module | File | Phase | Priority |
|--------|------|-------|----------|
| Station Management | MOD-001 | Phase 1 | Critical |
| Connector Management | MOD-002 | Phase 1 | Critical |
| Real-time Monitoring | MOD-003 | Phase 1 | Critical |
| Energy Metering | MOD-004 | Phase 1 | High |
| Fault Management | MOD-005 | Phase 1 | High |
| OCPP Integration | MOD-006 | Phase 1 | Critical |
| Tariff Configuration | MOD-007 | Phase 1 | High |
| Payment & Billing | MOD-008 | Phase 1 | Critical |
| Vehicle Management | MOD-009 | Phase 1 | Medium |
| Charging Session | MOD-010 | Phase 1 | Critical |
| User Account | MOD-011 | Phase 1 | High |
| Notifications | MOD-012 | Phase 2 | Medium |
| Station Grouping | MOD-013 | Phase 2 | Low |
| Audit Logging | MOD-014 | Phase 2 | Medium |
| E-Invoice | MOD-015 | Phase 2 | Medium |

---

## 11. Milestone Schedule

| Date | Milestone | Key Deliverables |
|------|-----------|-----------------|
| Mar 15, 2026 | Interactive Prototype | UI demo (admin + mobile wireframes) |
| Mar 31, 2026 | Station & Connector | MOD-001, MOD-002 CRUD complete |
| Apr 30, 2026 | OCPP + Monitoring | MOD-003, MOD-006 (OCPP WebSocket live), App: vehicle mgmt + QR charging |
| May 31, 2026 | MVP Complete | MOD-004, MOD-005, MOD-008 (metering, faults, payments) |
| Jun 1, 2026 | **MVP Go-live** | Production deployment |

---

**Status: Phase 1 Documentation is COMPLETE. Claude Code is clear to begin Phase 2 — Scaffold & Infrastructure.**
