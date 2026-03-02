#!/bin/bash
# init-docs.sh — Initialize EV Charging CSMS Documentation Structure
# 9-layer documentation architecture for KLC CSMS project
# Run: bash init-docs.sh

set -e

echo "🚀 Initializing EV Charging CSMS docs/ structure..."
echo ""

# ============================================================
# Layer 01: Business Documents
# ============================================================
echo "📁 Layer 01: Business Documents"
mkdir -p docs/01-business/originals

cat > docs/01-business/README.md << 'EOF'
# 01 - Business Documents

Business-level documents including BRD, stakeholder analysis, and market context.

## Contents
- `brd.md` — Business Requirements Document
- `stakeholder-analysis.md` — Stakeholder mapping & analysis
- `market-context.md` — Vietnam EV market context
- `originals/` — Original .docx/.xlsx source files (gitignored)
EOF

cat > docs/01-business/brd.md << 'EOF'
# Business Requirements Document (BRD)
## EV Charging Station Management System — KLC

> Status: DRAFT | Version: 0.1 | Last Updated: 2026-03-01

### 1. Executive Summary
<!-- TODO: Extract from original BRD document -->

### 2. Business Objectives
<!-- TODO: Define measurable business objectives -->

### 3. Scope
<!-- TODO: Define in-scope and out-of-scope items -->

### 4. Stakeholders
<!-- TODO: List all stakeholders and their interests -->

### 5. Success Criteria
<!-- TODO: Define measurable success criteria -->
EOF

cat > docs/01-business/stakeholder-analysis.md << 'EOF'
# Stakeholder Analysis

> Status: DRAFT | Last Updated: 2026-03-01

## Key Stakeholders

| Role | Name/Org | Interest | Influence |
|------|----------|----------|-----------|
| Client | KLC | Product owner, business requirements | High |
| Developer | EmeSoft | Technical implementation | High |
| End Users | EV Drivers | App usability, reliability | Medium |
| Operators | Station Operators | Monitoring, management | High |

<!-- TODO: Expand with detailed analysis -->
EOF

cat > docs/01-business/market-context.md << 'EOF'
# Vietnam EV Market Context

> Status: DRAFT | Last Updated: 2026-03-01

## Market Overview
<!-- TODO: Vietnam EV adoption statistics, growth projections -->

## Competitive Landscape
<!-- TODO: Existing CSMS solutions in Vietnam -->

## Regulatory Environment
<!-- TODO: Vietnam regulations for EV charging, e-invoicing requirements -->

## Payment Landscape
<!-- TODO: ZaloPay, MoMo, OnePay market share and integration considerations -->
EOF

# ============================================================
# Layer 02: Requirements
# ============================================================
echo "📁 Layer 02: Requirements"
mkdir -p docs/02-requirements

cat > docs/02-requirements/README.md << 'EOF'
# 02 - Requirements

System requirements including functional, non-functional, and integration requirements.

## Contents
- `functional-requirements.md` — High-level functional requirements
- `non-functional-requirements.md` — NFRs (performance, security, scalability)
- `integration-requirements.md` — Third-party integration requirements
- `user-stories.md` — User stories by persona
EOF

cat > docs/02-requirements/functional-requirements.md << 'EOF'
# Functional Requirements

> Status: DRAFT | Last Updated: 2026-03-01

## FR Categories

### A. CSMS & Admin Portal
- FR-A01: Station Registration & Identity (CRUD)
- FR-A02: Connector Management
- FR-A03: Real-time Monitoring (OCPP)
- FR-A04: Energy Metering
- FR-A05: Fault Detection & Management
- FR-A06: Station Availability
- FR-A07: Station Grouping
- FR-A08: Audit Logging
- FR-A09: Tariff Configuration
- FR-A10: Interactive Prototype (UI demo)

### B. Mobile App
- FR-B01: Vehicle Management
- FR-B02: Charging Interaction (QR scan, real-time status)
- FR-B03: Payment & Billing
- FR-B04: Charging History
- FR-B05: User Profile & Account
- FR-B06: Notifications & Alerts

<!-- TODO: Detail each FR with acceptance criteria -->
EOF

cat > docs/02-requirements/non-functional-requirements.md << 'EOF'
# Non-Functional Requirements

> Status: DRAFT | Last Updated: 2026-03-01

## Performance
- NFR-P01: API response time < 200ms (p95)
- NFR-P02: WebSocket message latency < 100ms
- NFR-P03: Support 1000+ concurrent charger connections
- NFR-P04: Mobile app startup < 3 seconds

## Security
- NFR-S01: HTTPS/WSS for all communications
- NFR-S02: OAuth2/OpenID Connect authentication
- NFR-S03: Role-based access control (RBAC)
- NFR-S04: PCI DSS compliance for payment data

## Scalability
- NFR-SC01: Horizontal scaling for API servers
- NFR-SC02: Database read replicas
- NFR-SC03: Redis caching layer

## Availability
- NFR-A01: 99.9% uptime SLA
- NFR-A02: Graceful degradation for charger offline scenarios

<!-- TODO: Detail each NFR with measurable criteria -->
EOF

cat > docs/02-requirements/integration-requirements.md << 'EOF'
# Integration Requirements

> Status: DRAFT | Last Updated: 2026-03-01

## Payment Gateways
- INT-PAY-01: ZaloPay integration
- INT-PAY-02: MoMo integration
- INT-PAY-03: OnePay integration

## E-Invoice
- INT-INV-01: MISA e-invoice
- INT-INV-02: Viettel e-invoice
- INT-INV-03: VNPT e-invoice

## OCPP Protocol
- INT-OCPP-01: OCPP 1.6J (JSON over WebSocket)
- INT-OCPP-02: Charger BootNotification, Heartbeat
- INT-OCPP-03: StartTransaction / StopTransaction
- INT-OCPP-04: MeterValues
- INT-OCPP-05: Remote commands (RemoteStart, RemoteStop, Reset)

## Maps & Location
- INT-MAP-01: Google Maps API

## Notifications
- INT-NOTIF-01: Firebase Cloud Messaging (FCM)

<!-- TODO: Detail API specs, auth flows, rate limits for each -->
EOF

cat > docs/02-requirements/user-stories.md << 'EOF'
# User Stories

> Status: DRAFT | Last Updated: 2026-03-01

## Personas
1. **EV Driver** — Uses mobile app to find, charge, and pay
2. **Station Operator** — Monitors and manages charging stations via portal
3. **Administrator** — Manages system configuration, users, billing

## EV Driver Stories
- US-D01: As a driver, I want to find nearby charging stations on a map
- US-D02: As a driver, I want to scan a QR code to start charging
- US-D03: As a driver, I want to see real-time charging progress
- US-D04: As a driver, I want to pay via ZaloPay/MoMo
- US-D05: As a driver, I want to view my charging history

## Station Operator Stories
- US-O01: As an operator, I want to see real-time status of all stations
- US-O02: As an operator, I want to receive fault alerts
- US-O03: As an operator, I want to view energy consumption reports

## Administrator Stories
- US-A01: As an admin, I want to register new charging stations
- US-A02: As an admin, I want to configure tariff plans
- US-A03: As an admin, I want to manage user accounts and roles

<!-- TODO: Add acceptance criteria, story points -->
EOF

# ============================================================
# Layer 03: Functional Specifications
# ============================================================
echo "📁 Layer 03: Functional Specifications"
mkdir -p docs/03-functional-specs/modules
mkdir -p docs/03-functional-specs/wireframes/originals
mkdir -p docs/03-functional-specs/api-specs

cat > docs/03-functional-specs/README.md << 'EOF'
# 03 - Functional Specifications

Detailed functional specs per module, wireframes, and API specifications.

## Contents
- `modules/` — FRS documents per module (MOD-NNN format)
- `wireframes/` — UI wireframes and mockups
- `api-specs/` — API endpoint specifications
EOF

# Module specs
for mod in \
  "001:station-management:Station Management" \
  "002:connector-management:Connector Management" \
  "003:realtime-monitoring:Real-time Monitoring" \
  "004:energy-metering:Energy Metering" \
  "005:fault-management:Fault Detection & Management" \
  "006:ocpp:OCPP 1.6J Integration" \
  "007:tariff:Tariff Configuration" \
  "008:payment:Payment & Billing" \
  "009:vehicle-management:Vehicle Management" \
  "010:charging-session:Charging Session" \
  "011:user-account:User Account & Profile" \
  "012:notifications:Notifications & Alerts" \
  "013:station-grouping:Station Grouping" \
  "014:audit-log:Audit Logging" \
  "015:e-invoice:E-Invoice Integration"; do

  IFS=':' read -r num slug title <<< "$mod"

  cat > "docs/03-functional-specs/modules/MOD-${num}-${slug}.md" << MODEOF
# MOD-${num}: ${title}

> Status: DRAFT | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
<!-- TODO: Module description and purpose -->

## 2. Actors
<!-- TODO: Who interacts with this module -->

## 3. Functional Requirements
<!-- TODO: Detailed functional requirements -->

## 4. Business Rules
<!-- TODO: Business rules and validations -->

## 5. Data Model
<!-- TODO: Entities, attributes, relationships -->

## 6. API Endpoints
<!-- TODO: REST API specification -->

## 7. UI/UX
<!-- TODO: Wireframe references, interaction flows -->

## 8. OCPP Messages (if applicable)
<!-- TODO: Related OCPP messages and flows -->

## 9. Error Handling
<!-- TODO: Error codes and handling strategies -->

## 10. Testing Scenarios
<!-- TODO: Key test scenarios and acceptance criteria -->
MODEOF
done

cat > docs/03-functional-specs/wireframes/README.md << 'EOF'
# Wireframes

UI wireframes and mockups for the EV Charging CSMS.

## Admin Portal
<!-- TODO: Link to admin portal wireframes -->

## Mobile App
<!-- TODO: Link to mobile app wireframes -->

## originals/
Original wireframe files (.docx, .sketch, .figma exports)
EOF

cat > docs/03-functional-specs/api-specs/README.md << 'EOF'
# API Specifications

REST API specifications for the EV Charging CSMS.

## Admin API (port 5000)
Full ABP layered architecture API.
<!-- TODO: OpenAPI/Swagger spec -->

## Driver BFF API (port 5001)
.NET Minimal API with Redis cache-first, read replicas.
<!-- TODO: OpenAPI/Swagger spec -->
EOF

# ============================================================
# Layer 04: Architecture
# ============================================================
echo "📁 Layer 04: Architecture"
mkdir -p docs/04-architecture

cat > docs/04-architecture/README.md << 'EOF'
# 04 - Architecture

System architecture documentation.

## Contents
- `system-overview.md` — High-level system architecture
- `backend-architecture.md` — .NET/ABP backend architecture
- `database-design.md` — PostgreSQL database design
- `ocpp-architecture.md` — OCPP WebSocket architecture
- `mobile-architecture.md` — React Native app architecture
- `deployment-architecture.md` — AWS deployment architecture
- `security-architecture.md` — Security design
EOF

for arch in \
  "system-overview:System Overview" \
  "backend-architecture:Backend Architecture (.NET/ABP)" \
  "database-design:Database Design (PostgreSQL)" \
  "ocpp-architecture:OCPP WebSocket Architecture" \
  "mobile-architecture:Mobile App Architecture (React Native)" \
  "deployment-architecture:Deployment Architecture (AWS)" \
  "security-architecture:Security Architecture"; do

  IFS=':' read -r slug title <<< "$arch"

  cat > "docs/04-architecture/${slug}.md" << ARCHEOF
# ${title}

> Status: DRAFT | Last Updated: 2026-03-01

<!-- TODO: Architecture documentation -->
ARCHEOF
done

# ============================================================
# Layer 05: Architecture Decision Records
# ============================================================
echo "📁 Layer 05: Architecture Decision Records"
mkdir -p docs/05-decisions

cat > docs/05-decisions/README.md << 'EOF'
# 05 - Architecture Decision Records (ADRs)

Tracking key architecture decisions using ADR format.

## Format
Each ADR follows: Status, Context, Decision, Consequences

## ADR Index
- ADR-001: Use ABP Framework
- ADR-002: PostgreSQL as primary database
- ADR-003: OCPP 1.6J protocol version
- ADR-004: Modular monolith (Phase 1)
- ADR-005: CQRS with MediatR

<!-- TODO: Add more ADRs as decisions are made -->
EOF

for adr in \
  "001:abp-framework:Use ABP Framework:Need a robust .NET framework with DDD support:ABP Framework provides DDD layered architecture, built-in modules (identity, audit, permissions), and .NET 10 support" \
  "002:postgresql:PostgreSQL as Primary DB:Need a reliable, scalable relational database:PostgreSQL offers excellent JSON support, ACID compliance, and strong ecosystem in Vietnam hosting" \
  "003:ocpp-16j:OCPP 1.6J Protocol:Need standard protocol for charger communication:OCPP 1.6J (JSON/WebSocket) is the most widely supported version by charger manufacturers" \
  "004:modular-monolith:Modular Monolith for Phase 1:Balance between simplicity and modularity:Start as modular monolith, designed to split into microservices when needed" \
  "005:cqrs-mediatr:CQRS with MediatR:Need to separate read and write operations for performance:MediatR provides clean CQRS implementation with pipeline behaviors"; do

  IFS=':' read -r num slug title context decision <<< "$adr"

  cat > "docs/05-decisions/ADR-${num}-${slug}.md" << ADREOF
# ADR-${num}: ${title}

> Status: ACCEPTED | Date: 2026-03-01

## Context
${context}

## Decision
${decision}

## Consequences
### Positive
<!-- TODO: List positive consequences -->

### Negative
<!-- TODO: List negative consequences / trade-offs -->

### Risks
<!-- TODO: List risks and mitigations -->
ADREOF
done

# ============================================================
# Layer 06: Project Management
# ============================================================
echo "📁 Layer 06: Project Management"
mkdir -p docs/06-project-management/originals

cat > docs/06-project-management/README.md << 'EOF'
# 06 - Project Management

Project plans, timelines, team structure, and management documents.

## Contents
- `project-plan.md` — Project plan and milestones
- `team-structure.md` — Team roles and responsibilities
- `risk-register.md` — Risk identification and mitigation
- `originals/` — Original project files (.docx, .xlsx)
EOF

cat > docs/06-project-management/project-plan.md << 'EOF'
# Project Plan — EV Charging CSMS

> Status: ACTIVE | Last Updated: 2026-03-01

## Timeline Overview
- **Kick-off:** March 1, 2026
- **MVP Go-live:** June 1, 2026
- **Total Duration:** 4 months

## Milestones

| Date | Milestone | Status |
|------|-----------|--------|
| Mar 1, 2026 | Kick-off | ✅ Done |
| Mar 15, 2026 | Interactive prototype / UI demo | 🔄 In Progress |
| Mar 31, 2026 | Station registration, connector management | ⏳ Planned |
| Apr 30, 2026 | Real-time monitoring (OCPP), App: vehicle + QR | ⏳ Planned |
| May 31, 2026 | Energy metering, fault mgmt, payment/billing | ⏳ Planned |
| Jun 1, 2026 | **MVP Go-live** | ⏳ Planned |
| Jun 30, 2026 | Firmware tracking, idle fee, pricing | ⏳ Phase 2 |
| Jul 31, 2026 | Station availability, firmware update | ⏳ Phase 2 |
| Aug 31, 2026 | Audit log, notifications, AI features | ⏳ Phase 2 |
EOF

cat > docs/06-project-management/team-structure.md << 'EOF'
# Team Structure

> Last Updated: 2026-03-01

## EmeSoft Development Team

| Role | Name | Responsibility |
|------|------|---------------|
| CTO / Tech Lead | Hung | Project owner, technical decisions |
| Developers | 5 devs | Implementation |
| DevOps | TBD | Infrastructure, CI/CD |
| Technical Architect | TBD | Architecture review |
| QC | TBD | Quality assurance |
| IT Helpdesk | TBD | Support |
| BA | TBD | Business analysis |
| CE | TBD | Customer engagement |
EOF

cat > docs/06-project-management/risk-register.md << 'EOF'
# Risk Register

> Status: DRAFT | Last Updated: 2026-03-01

| ID | Risk | Probability | Impact | Mitigation |
|----|------|-------------|--------|------------|
| R01 | OCPP charger compatibility issues | Medium | High | Test with multiple charger brands early |
| R02 | Payment gateway integration delays | Medium | High | Start integration early, have fallback |
| R03 | Timeline pressure (4 months to MVP) | High | High | Strict Phase 1 scope, no scope creep |
| R04 | Performance under load | Medium | Medium | Load test early, Redis caching |
| R05 | Vietnam regulatory changes | Low | Medium | Monitor regulations, flexible design |

<!-- TODO: Expand with detailed mitigation plans -->
EOF

# ============================================================
# Layer 07: Testing
# ============================================================
echo "📁 Layer 07: Testing"
mkdir -p docs/07-testing

cat > docs/07-testing/README.md << 'EOF'
# 07 - Testing

Test strategy, test plans, and testing documentation.

## Contents
- `test-strategy.md` — Overall testing strategy
- `test-plan.md` — Detailed test plan
- `ocpp-test-scenarios.md` — OCPP-specific test scenarios
EOF

cat > docs/07-testing/test-strategy.md << 'EOF'
# Test Strategy

> Status: DRAFT | Last Updated: 2026-03-01

## Testing Levels
1. **Unit Tests** — Domain logic, application services
2. **Integration Tests** — API endpoints, database, external services
3. **E2E Tests** — Critical user flows
4. **Performance Tests** — Load testing, WebSocket concurrency
5. **OCPP Protocol Tests** — Charger communication scenarios

## Tools
- xUnit (unit & integration tests)
- ABP Test Infrastructure
- k6 (performance testing)
- OCPP test simulator

## Coverage Targets
- Domain layer: 80%+
- Application services: 70%+
- Critical paths: 100% E2E coverage
EOF

cat > docs/07-testing/test-plan.md << 'EOF'
# Test Plan

> Status: DRAFT | Last Updated: 2026-03-01

<!-- TODO: Detailed test plan per module -->
EOF

cat > docs/07-testing/ocpp-test-scenarios.md << 'EOF'
# OCPP Test Scenarios

> Status: DRAFT | Last Updated: 2026-03-01

## Boot & Connection
- TC-OCPP-01: Charger BootNotification (accepted)
- TC-OCPP-02: Charger BootNotification (rejected/pending)
- TC-OCPP-03: Heartbeat cycle
- TC-OCPP-04: Connection lost & reconnect

## Charging Flow
- TC-OCPP-10: Normal charging session (Start → MeterValues → Stop)
- TC-OCPP-11: Remote start transaction
- TC-OCPP-12: Remote stop transaction
- TC-OCPP-13: Session interrupted (power loss)

## Status & Faults
- TC-OCPP-20: StatusNotification transitions
- TC-OCPP-21: Fault detection and reporting
- TC-OCPP-22: Connector availability changes

## Configuration & Management
- TC-OCPP-30: GetConfiguration
- TC-OCPP-31: ChangeConfiguration
- TC-OCPP-32: Reset (soft/hard)
- TC-OCPP-33: UnlockConnector

<!-- TODO: Detail each test case with steps, expected results -->
EOF

# ============================================================
# Layer 08: Guides
# ============================================================
echo "📁 Layer 08: Guides"
mkdir -p docs/08-guides

cat > docs/08-guides/README.md << 'EOF'
# 08 - Guides

Developer guides, setup instructions, and operational runbooks.

## Contents
- `dev-setup.md` — Local development environment setup
- `coding-conventions.md` — Code style and conventions
- `deployment-guide.md` — Deployment procedures
- `api-guide.md` — API usage guide
EOF

cat > docs/08-guides/dev-setup.md << 'EOF'
# Development Environment Setup

> Status: DRAFT | Last Updated: 2026-03-01

## Prerequisites
- .NET 10 SDK
- Node.js 20+ (for frontend)
- Docker & Docker Compose
- PostgreSQL 16 (or via Docker)
- Redis (or via Docker)
- IDE: Visual Studio 2022 / Rider / VS Code

## Quick Start
```bash
# 1. Clone repo
git clone git@github.com:EMESOFT/ev-charging-csms.git
cd ev-charging-csms

# 2. Start infrastructure
docker compose up -d  # PostgreSQL + Redis

# 3. Run migrations
dotnet ef database update -p src/EVCharging.EntityFrameworkCore

# 4. Run Admin API
cd src/EVCharging.Admin.HttpApi.Host && dotnet run

# 5. Run Driver BFF
cd src/EVCharging.Driver.BFF && dotnet run
```

<!-- TODO: Detailed setup instructions -->
EOF

cat > docs/08-guides/coding-conventions.md << 'EOF'
# Coding Conventions

> Status: DRAFT | Last Updated: 2026-03-01

## General
- All code in English (comments, variable names, API endpoints)
- Vietnamese only for user-facing strings (via ABP localization)
- Follow ABP Framework naming conventions

## C# / .NET
- Use C# 13 features where appropriate
- Entity base: `FullAuditedAggregateRoot<Guid>`
- CQRS: `IQuery<T>` / `ICommand` + MediatR handlers
- Cache key format: `"entity:{id}:field"`
- Cursor-based pagination (not offset)
- All UI strings via `IStringLocalizer`

## API
- RESTful design with proper HTTP methods
- Error format: `{ "code": "MOD_001", "message": "...", "details": {} }`
- API versioning from the start
- Swagger/OpenAPI documentation

## Database
- Code-first migrations (ABP DbMigrator)
- Proper indexing on frequently queried columns
- Soft delete via ABP `ISoftDelete`

## Formatting
- Date: dd/MM/yyyy
- Timezone: UTC+7
- Currency: VNĐ (9.900đ, dấu chấm phân cách)

<!-- TODO: Expand with specific examples -->
EOF

cat > docs/08-guides/deployment-guide.md << 'EOF'
# Deployment Guide

> Status: DRAFT | Last Updated: 2026-03-01

## Infrastructure
- AWS cloud
- Docker containers
- Docker Compose (Phase 1) → ECS/EKS (future)

## CI/CD
- GitHub Actions or AWS CodePipeline
- Automated testing on PR
- Staging → Production deployment

<!-- TODO: Detailed deployment procedures -->
EOF

cat > docs/08-guides/api-guide.md << 'EOF'
# API Usage Guide

> Status: DRAFT | Last Updated: 2026-03-01

## Authentication
- OAuth2 / OpenID Connect via ABP Identity + OpenIddict
- Bearer token authentication

## Admin API (port 5000)
Base URL: `https://api.klc.vn/admin/`

## Driver BFF API (port 5001)
Base URL: `https://api.klc.vn/driver/`

<!-- TODO: Detailed API usage examples -->
EOF

# ============================================================
# Layer 09: AI Playbook
# ============================================================
echo "📁 Layer 09: AI Playbook"
mkdir -p docs/09-ai-playbook/rules
mkdir -p docs/09-ai-playbook/anti-patterns
mkdir -p docs/09-ai-playbook/patterns
mkdir -p docs/09-ai-playbook/lessons-learned
mkdir -p docs/09-ai-playbook/debug-playbooks
mkdir -p docs/09-ai-playbook/prompts

cat > docs/09-ai-playbook/README.md << 'EOF'
# 09 - AI Playbook

Knowledge base for AI agents working on the EV Charging CSMS project.

## Structure
- `rules/` — MUST-follow rules for AI agents
- `anti-patterns/` — NEVER-do patterns (learned from mistakes)
- `patterns/` — Proven implementation patterns
- `lessons-learned/` — Specific lessons from development
- `debug-playbooks/` — Step-by-step debugging guides
- `prompts/` — Reusable prompt templates

## How to Use
1. **Before coding:** Read `rules/_master-rules.md`
2. **Before implementing:** Check `anti-patterns/_index.md`
3. **While implementing:** Reference `patterns/_index.md`
4. **When debugging:** Check `debug-playbooks/_index.md`
5. **After learning:** Add to `lessons-learned/`
EOF

# Rules
cat > docs/09-ai-playbook/rules/_master-rules.md << 'EOF'
# Master Rules — AI Agent Guidelines

> ⚠️ MUST READ before any implementation work

## Rule Categories

### R1: ABP Framework Rules
- R1.1: Always use ABP conventions for entities, services, repositories
- R1.2: Business logic belongs in Domain layer, NOT in Application Services
- R1.3: Use `FullAuditedAggregateRoot<Guid>` for aggregate roots
- R1.4: Use ABP's built-in features before building custom solutions
- R1.5: Never expose domain entities through API — always use DTOs

### R2: OCPP Rules
- R2.1: Handle OCPP messages idempotently (chargers may retry)
- R2.2: Persist transaction data immediately for billing accuracy
- R2.3: Handle charger reconnection gracefully
- R2.4: Use strongly-typed OCPP message models

### R3: API Rules
- R3.1: All APIs must have Swagger documentation
- R3.2: Use cursor-based pagination (not offset)
- R3.3: Standard error format: `{ "code": "MOD_001", "message": "...", "details": {} }`
- R3.4: API versioning on all endpoints

### R4: Database Rules
- R4.1: Use code-first migrations (never manual DB changes)
- R4.2: Add indexes on frequently queried columns
- R4.3: Use soft delete via ABP `ISoftDelete`
- R4.4: Cache hot data in Redis

### R5: Testing Rules
- R5.1: Unit tests for all domain logic
- R5.2: Integration tests for application services
- R5.3: Test OCPP message flows end-to-end
- R5.4: Use ABP test infrastructure

### R6: Code Quality Rules
- R6.1: All code in English
- R6.2: Vietnamese only in localization files
- R6.3: Conventional commits (`feat:`, `fix:`, `chore:`, etc.)
- R6.4: No hardcoded strings in UI — use IStringLocalizer

<!-- This file will grow as we learn more -->
EOF

# Anti-patterns
cat > docs/09-ai-playbook/anti-patterns/_index.md << 'EOF'
# Anti-Patterns Index

> ❌ NEVER do these things

## AP-001: Business Logic in Application Service
**Wrong:** Putting validation/business rules in AppService
**Right:** Put them in Domain Entity or Domain Service

## AP-002: Exposing Domain Entities via API
**Wrong:** Returning `ChargingStation` entity from API
**Right:** Map to `ChargingStationDto` using AutoMapper

## AP-003: Offset-based Pagination
**Wrong:** `?page=5&pageSize=20`
**Right:** `?cursor=abc123&limit=20`

## AP-004: Hardcoded UI Strings
**Wrong:** `"Trạm sạc không khả dụng"` in code
**Right:** `L["StationNotAvailable"]` via IStringLocalizer

## AP-005: Manual Database Changes
**Wrong:** Running ALTER TABLE directly
**Right:** Create migration via ABP DbMigrator

<!-- Add more anti-patterns as discovered -->
EOF

# Patterns
cat > docs/09-ai-playbook/patterns/_index.md << 'EOF'
# Patterns Index

> ✅ USE these proven patterns

## PAT-001: ABP Entity Creation
```csharp
public class ChargingStation : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public StationStatus Status { get; private set; }

    protected ChargingStation() { } // EF Core

    public ChargingStation(Guid id, string name) : base(id)
    {
        SetName(name);
        Status = StationStatus.Offline;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }
}
```

## PAT-002: CQRS Query Handler
```csharp
public class GetStationByIdQuery : IQuery<StationDto>
{
    public Guid StationId { get; set; }
}

public class GetStationByIdHandler : IRequestHandler<GetStationByIdQuery, StationDto>
{
    // Implementation with Redis cache-first pattern
}
```

## PAT-003: Redis Cache-First Pattern
```csharp
var cached = await _cache.GetAsync<StationDto>($"station:{id}");
if (cached != null) return cached;

var station = await _repository.GetAsync(id);
var dto = ObjectMapper.Map<StationDto>(station);
await _cache.SetAsync($"station:{id}", dto, TimeSpan.FromMinutes(5));
return dto;
```

<!-- Add more patterns as developed -->
EOF

# Lessons Learned
cat > docs/09-ai-playbook/lessons-learned/_index.md << 'EOF'
# Lessons Learned Index

> 📝 Real lessons from development

## Format
Each lesson: LL-NNN: Title
- **Problem:** What happened
- **Root Cause:** Why it happened
- **Solution:** How we fixed it
- **Prevention:** How to prevent in future

<!-- Add lessons as they occur during development -->
<!-- Example:
## LL-001: OCPP Message Deserialization Failure
- **Problem:** Charger messages failing to parse
- **Root Cause:** Null handling in optional fields
- **Solution:** Added nullable annotations and default values
- **Prevention:** Always use strongly-typed models with nullable props
-->
EOF

# Debug Playbooks
cat > docs/09-ai-playbook/debug-playbooks/_index.md << 'EOF'
# Debug Playbooks Index

> 🔧 Step-by-step debugging guides

## DB-001: Charger Not Connecting
1. Check WebSocket URL: `ws://host/ocpp/{chargePointId}`
2. Verify charger is registered in database
3. Check network/firewall rules
4. Review OCPP handshake logs
5. Test with OCPP simulator

## DB-002: Charging Session Not Starting
1. Check charger status (must be Available)
2. Verify user authorization
3. Check OCPP StartTransaction message flow
4. Review connector status
5. Check transaction ID generation

## DB-003: Payment Processing Failure
1. Check payment gateway connectivity
2. Verify API credentials
3. Review request/response logs
4. Check amount calculation
5. Verify callback URL configuration

<!-- Add more debug playbooks as issues are discovered -->
EOF

# Prompts
cat > docs/09-ai-playbook/prompts/_index.md << 'EOF'
# Prompt Templates Index

> 📋 Reusable prompt templates for AI agents

## PT-001: Implement New Module
```
Read the FRS: docs/03-functional-specs/modules/MOD-{NNN}.md
Read rules: docs/09-ai-playbook/rules/_master-rules.md
Check anti-patterns: docs/09-ai-playbook/anti-patterns/_index.md

Implement:
1. Domain entities (Domain layer)
2. Repository interfaces (Domain layer)
3. Application DTOs (Application.Contracts)
4. Application services (Application layer)
5. EF Core mapping (EntityFrameworkCore layer)
6. API controllers (HttpApi layer)
7. Unit tests
8. Integration tests
```

## PT-002: Add OCPP Message Handler
```
Read: docs/03-functional-specs/modules/MOD-006-ocpp.md
Read: docs/09-ai-playbook/rules/_master-rules.md (R2: OCPP Rules)

Implement:
1. Message model (strongly-typed)
2. Handler service
3. Idempotency check
4. Persistence
5. Response generation
6. Unit tests
```

<!-- Add more prompt templates as needed -->
EOF

# ============================================================
# Assets & Templates
# ============================================================
echo "📁 Assets & Templates"
mkdir -p docs/assets/templates

cat > docs/assets/templates/module-frs-template.md << 'EOF'
# MOD-{NNN}: {Module Title}

> Status: DRAFT | Priority: Phase {N} | Last Updated: YYYY-MM-DD

## 1. Overview
Brief description of the module's purpose and scope.

## 2. Actors
| Actor | Description |
|-------|-------------|
| | |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-{NNN}-01 | | Must |

## 4. Business Rules
| ID | Rule | Description |
|----|------|-------------|
| BR-{NNN}-01 | | |

## 5. Data Model
### Entities
### Relationships
### Attributes

## 6. API Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/v1/{resource} | | Yes |

## 7. UI/UX
### Wireframe References
### User Flows

## 8. OCPP Messages (if applicable)
| Direction | Message | When |
|-----------|---------|------|
| CP → CSMS | | |

## 9. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_{NNN}_001 | | 400 |

## 10. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-{NNN}-01 | | |
EOF

cat > docs/assets/templates/adr-template.md << 'EOF'
# ADR-{NNN}: {Title}

> Status: PROPOSED | Date: YYYY-MM-DD

## Context
What is the issue that we're seeing that is motivating this decision?

## Decision
What is the change that we're proposing and/or doing?

## Consequences

### Positive
-

### Negative
-

### Risks
-

## Alternatives Considered
1. **Alternative A:** Description — rejected because...
2. **Alternative B:** Description — rejected because...
EOF

cat > docs/assets/templates/lesson-learned-template.md << 'EOF'
# LL-{NNN}: {Title}

> Date: YYYY-MM-DD | Module: {module} | Severity: {Low|Medium|High}

## Problem
What happened?

## Root Cause
Why did it happen?

## Solution
How was it fixed?

## Prevention
How to prevent this in the future?

## Related
- Anti-pattern: AP-{NNN} (if applicable)
- Pattern: PAT-{NNN} (if applicable)
- Debug playbook: DB-{NNN} (if applicable)
EOF

# ============================================================
# Root README for docs/
# ============================================================
echo "📁 Creating docs/ root README"

cat > docs/README.md << 'EOF'
# EV Charging CSMS — Documentation

9-layer documentation architecture for the KLC EV Charging Station Management System.

## Layers

| # | Layer | Description |
|---|-------|-------------|
| 01 | Business | BRD, stakeholder analysis, market context |
| 02 | Requirements | Functional, non-functional, integration requirements |
| 03 | Functional Specs | Module FRS, wireframes, API specs |
| 04 | Architecture | System, backend, DB, OCPP, mobile, deployment |
| 05 | Decisions | Architecture Decision Records (ADRs) |
| 06 | Project Management | Plans, timelines, team, risks |
| 07 | Testing | Strategy, plans, OCPP test scenarios |
| 08 | Guides | Dev setup, coding conventions, deployment |
| 09 | AI Playbook | Rules, patterns, anti-patterns, debug playbooks |

## Quick Links
- [DOCS-GUIDE.md](./DOCS-GUIDE.md) — How to use this docs structure
- [AI Playbook](./09-ai-playbook/README.md) — AI agent knowledge base
- [Templates](./assets/templates/) — Document templates

## For AI Agents
Start with `CLAUDE.md` at repo root → then read relevant module FRS → then implement.
EOF

echo ""
echo "✅ Documentation structure initialized!"
echo ""

# Count results
FOLDERS=$(find docs -type d | wc -l)
FILES=$(find docs -type f | wc -l)
echo "📊 Created: ${FOLDERS} folders, ${FILES} files"
echo ""
echo "Next steps:"
echo "  1. Move guide files to correct locations"
echo "  2. Create CLAUDE.md + AGENTS.md"
echo "  3. Commit to git"
