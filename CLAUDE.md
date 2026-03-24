# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

EV Charging Station Management System (CSMS) — B2C platform for EV charging station management in Vietnam. Client: KLC, Developer: EmeSoft. MVP Go-live target: June 1, 2026.

## Tech Stack

- Backend: .NET 10 (SDK pinned to 10.0.101 via `global.json`), C#, ABP Framework 10.1 (LeptonX Lite theme)
- Database: PostgreSQL 16 (port 5433) + PostGIS + Read Replicas
- Cache: Redis 7 (port 6379)
- Real-time: SignalR (MonitoringHub for admin, DriverHub for mobile)
- Admin Portal: Next.js (App Router), TypeScript, TanStack Query, Zustand, Recharts, Leaflet maps
- Mobile: React Native (Expo 50), TypeScript, React Navigation, react-native-maps (Google Maps), i18next, Zustand
- OCPP: OCPP.Core (1.6J/2.0/2.1) via WebSocket at `/ocpp/{chargePointId}`. Simulator: `ocpp-simulator/` (git submodule)
- CQRS: MediatR (IQuery/ICommand pattern)
- Localization: VI (default) + EN
- Monitoring: Sentry (admin portal + Driver BFF), Serilog (backend structured logging)
- Testing: xUnit + NSubstitute + Shouldly (backend), Vitest + Testing Library (admin portal), Jest (mobile), Playwright (e2e), Maestro (mobile automation)

## Architecture

Dual-API modular monolith (Phase 1), designed for microservices evolution:

- **Admin API** (port 44305, HTTPS): Full ABP layered DDD — controllers, app services, domain, EF Core. Auth via OpenIddict (`/connect/token`, client_id: `KLC_Api`). Swagger UI client: `KLC_Swagger`.
- **Driver BFF** (port 5001, HTTP): .NET Minimal API, Redis cache-first, read replicas. Auth via JWT Bearer (not OpenIddict). Uses Scalar for API docs (not Swagger).
- **Admin Portal** (port 3001): Next.js frontend consuming Admin API. State: Zustand (auth, sidebar, alerts). Data fetching: TanStack Query (1min stale).
- **Driver App**: React Native/Expo, consuming Driver BFF. Default map region: Hanoi.
- **Shared layers**: `KLC.Domain`, `KLC.Domain.Shared`, `KLC.Application`, `KLC.EntityFrameworkCore` are shared between both APIs.

### Backend Project Structure

```
src/backend/src/
├── KLC.Domain.Shared/        # Enums, constants, localization resources (vi.json, en.json)
├── KLC.Domain/               # Entities, domain services, repo interfaces
│   ├── Stations/                 # ChargingStation, Connector, StationGroup, StatusChangeLog, FavoriteStation, StationAmenity, StationPhoto
│   ├── Sessions/                 # ChargingSession, MeterValue
│   ├── Payments/                 # PaymentTransaction, Invoice, EInvoice, UserPaymentMethod, WalletTransaction, WalletDomainService, IPaymentGatewayService
│   ├── Marketing/                # Voucher, UserVoucher, Promotion
│   ├── Support/                  # UserFeedback
│   ├── Files/                       # IFileUploadService
│   ├── Ocpp/                     # IOcppService, OcppService, Messages/
│   ├── Faults/, Notifications/ (Notification, NotificationPreference, IPushNotificationService), Tariffs/, Users/ (AppUser, DeviceToken), Vehicles/
│   └── Settings/
├── KLC.Application.Contracts/# DTOs, interfaces, permissions
├── KLC.Application/          # App services, MediatR handlers, AutoMapper profiles
│   ├── Notifications/            # FirebasePushNotificationService (FCM)
│   ├── Files/                    # GcsFileUploadService (Google Cloud Storage)
│   └── Payments/                 # StubMoMoPaymentService, StubVnPayPaymentService
├── KLC.EntityFrameworkCore/  # KLCDbContext, repos, migrations
├── KLC.HttpApi/              # API controllers
├── KLC.HttpApi.Host/         # Admin API host (OCPP WS, SignalR, OpenIddict)
│   ├── Ocpp/                     # OcppWebSocketMiddleware, ConnectionManager, MessageHandler
│   └── Hubs/                     # MonitoringHub, MonitoringNotifier
├── KLC.Driver.BFF/           # Driver BFF (Minimal API endpoints, BFF services, DriverHub)
│   ├── Hubs/                     # DriverHub, DriverHubNotifier (IDriverHubNotifier)
└── KLC.DbMigrator/           # Migration runner

src/backend/test/
├── KLC.Domain.Tests/
├── KLC.Application.Tests/
├── KLC.EntityFrameworkCore.Tests/
└── KLC.TestBase/
```

### Admin Portal Structure

```
src/admin-portal/src/
├── app/
│   ├── login/page.tsx            # OpenIddict password flow login
│   └── (dashboard)/              # Auth-guarded route group
│       ├── layout.tsx            # Sidebar + Header shell
│       ├── page.tsx              # Dashboard overview (stats, charts)
│       ├── stations/             # CRUD + [id] detail + new
│       ├── monitoring/           # Real-time via SignalR
│       ├── sessions/, tariffs/, payments/, faults/, maintenance/
│       ├── groups/, audit-logs/, e-invoices/, alerts/, settings/
│       └── user-management/      # RBAC users & roles
├── components/
│   ├── layout/                   # header.tsx, sidebar.tsx
│   ├── ui/                       # badge, button, card
│   └── stations/                 # Station-specific components
└── lib/
    ├── api.ts                    # Axios client, API modules, DTOs
    └── store.ts                  # Zustand stores (auth, sidebar, alerts)
```

### Infrastructure (docker-compose.yml)

| Service    | Image            | Port       | Notes                                    |
| ---------- | ---------------- | ---------- | ---------------------------------------- |
| postgres   | postgis/postgis:16-3.4 | 5433:5432 | DB: KLC, PostGIS enabled, user: postgres/postgres |
| redis      | redis:7-alpine   | 6379:6379  | appendonly persistence                   |
| pgadmin    | dpage/pgadmin4   | 8080:80    | `--profile tools` to enable              |

### Cloud Services (GCP project: klc-ev-charging)

| Service | Purpose | Config Key |
|---------|---------|------------|
| Firebase Cloud Messaging | Push notifications to mobile devices | `Firebase:CredentialPath` |
| Google Cloud Storage | File uploads (avatars, station photos) | `GoogleCloud:StorageBucket` |
| PostGIS | Spatial queries (nearby stations, clustering) | Enabled via `UseNetTopologySuite()` |

Service account: `klc-backend@klc-ev-charging.iam.gserviceaccount.com`
GCS bucket: `gs://klc-ev-charging-uploads` (asia-southeast1)
Credential file: `firebase-service-account.json` (gitignored)

## Commands

```bash
# Start infrastructure (PostgreSQL + Redis)
docker compose up -d

# Start pgAdmin (optional)
docker compose --profile tools up -d

# Run Admin API (port 44305)
cd src/backend/src/KLC.HttpApi.Host && dotnet run

# Run Driver BFF (port 5001)
cd src/backend/src/KLC.Driver.BFF && dotnet run

# Run Admin Portal (port 3001)
cd src/admin-portal && npm run dev

# Run Mobile App
cd src/driver-app && npx expo start

# Backend tests (xUnit + NSubstitute + Shouldly)
dotnet test src/backend                              # all tests
dotnet test src/backend/test/KLC.Domain.Tests        # single project
dotnet test src/backend --filter "FullyQualifiedName~ClassName.MethodName"  # single test

# Admin Portal tests (Vitest + Testing Library)
cd src/admin-portal && npm test                      # run once
cd src/admin-portal && npm run test:watch            # watch mode

# Admin Portal lint & type-check
cd src/admin-portal && npm run lint                  # ESLint
cd src/admin-portal && npx tsc --noEmit              # TypeScript check

# DB migration (apply) — startup project required
dotnet ef database update -p src/backend/src/KLC.EntityFrameworkCore -s src/backend/src/KLC.HttpApi.Host

# DB migration (create new)
dotnet ef migrations add <MigrationName> -p src/backend/src/KLC.EntityFrameworkCore -s src/backend/src/KLC.HttpApi.Host

# Seed demo data (users: admin/Admin@123, operator/Admin@123, viewer/Admin@123)
PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d KLC -f scripts/seed-demo-data.sql

# Admin Portal e2e tests (Playwright)
cd src/admin-portal && npx playwright test

# Mobile automation tests (Maestro)
cd src/driver-app && maestro test .maestro/login.yaml

# Dev environment bootstrap
./scripts/bootstrap-dev.sh

# DB backup/restore
./scripts/db-backup.sh
./scripts/db-restore.sh <backup-file>
```

### Environment Variables

Admin Portal requires `.env.local` (see `src/admin-portal/.env.example`):
- `NEXT_PUBLIC_API_URL` — Admin API base URL
- `NEXT_PUBLIC_SIGNALR_URL` — SignalR hub URL

## CI/CD

- **Runner**: Self-hosted GCE (`[self-hosted, linux, x64, gce]`) — not GitHub-hosted
- **CI** (`.github/workflows/ci.yml`): Runs on PR to main/develop — builds backend, runs `dotnet test`, builds admin portal, runs `npm test` + `tsc --noEmit`
- **Deploy Dev** (`.github/workflows/deploy-dev.yml`): Push to `develop` → builds Docker images → deploys to Cloud Run (dev)
- **Deploy Prod** (`.github/workflows/deploy.yml`): Push to `main` → builds Docker images → deploys to Cloud Run (prod)
- **Cloud Run services**: `backend-api` (Admin API), `bff-socket` (Driver BFF), `ocpp-gateway`, `admin-portal`
- **Artifact Registry**: `asia-southeast1-docker.pkg.dev/klc-ev-charging/`
- DB migrations can be triggered manually via `workflow_dispatch` on deploy workflows
- **Dockerfiles**: `src/backend/src/KLC.HttpApi.Host/Dockerfile`, `src/backend/src/KLC.Driver.BFF/Dockerfile`, `src/admin-portal/Dockerfile` (all multi-stage builds)

## Key Conventions

- **Entity base**: ABP `FullAuditedAggregateRoot<Guid>` — private setters, validation in constructors/methods
- **CQRS**: IQuery/ICommand + MediatR handlers in Application layer
- **Cache key**: `"entity:{id}:field"` (e.g., `"station:123:status"`)
- **API error**: `{ "code": "MOD_001", "message": "...", "details": {} }`
- **Date format**: dd/MM/yyyy, Timezone: UTC+7
- **Currency**: VNĐ (9.900đ), dot separator
- **Pagination**: cursor-based (never offset-based)
- **UI strings**: Always via `IStringLocalizer` — never hardcode, Vietnamese only in localization files
- **Code language**: All code in English
- **Commits**: conventional format (`feat:`, `fix:`, `chore:`, etc.)
- **Soft delete**: Via ABP `ISoftDelete` — never hard delete
- **DB changes**: Code-first migrations only — never manual SQL DDL
- **Domain logic**: In entity/domain service — never in application services
- **DTOs**: Always map entities to DTOs for API responses — never expose domain entities

## ABP Permissions

Groups: `Stations`, `Connectors`, `Tariffs`, `Sessions`, `Faults`, `Alerts`, `Monitoring`, `StationGroups`, `Payments`, `AuditLogs`, `EInvoices`, `UserManagement`, `RoleManagement`, `MobileUsers`, `Vouchers`, `Promotions`, `Feedback`, `Notifications`. Each has `Default`, `Create`, `Update`, `Delete` + action-specific permissions. Defined in `KLCPermissions.cs`.

## Domain Entities (by bounded context)

- **Stations**: ChargingStation, Connector, StationGroup, StatusChangeLog, FavoriteStation, StationAmenity, StationPhoto
- **Sessions**: ChargingSession, MeterValue
- **Payments**: PaymentTransaction, Invoice, EInvoice, UserPaymentMethod, WalletTransaction, WalletDomainService
- **Marketing**: Voucher, UserVoucher, Promotion
- **Support**: UserFeedback
- **Notifications**: Notification, NotificationPreference
- **Users**: AppUser, DeviceToken
- **Vehicles**: Vehicle
- **Tariffs**: TariffPlan
- **Faults**: Fault
- **Power Management**: PowerSharingGroup, PowerSharingGroupMember, SiteLoadProfile, PowerSharingDomainService
- **Fleet** (Phase 2): Fleet, FleetVehicle
- **Integration** (Phase 2): Operator

## Enums (KLC.Domain.Shared/Enums/)

StationStatus, ConnectorStatus, ConnectorType (incl. NACS), SessionStatus, PaymentGateway (ZaloPay/MoMo/OnePay/Wallet/VnPay/QrPayment/Voucher/Urbox), PaymentStatus, NotificationType, FaultStatus, MembershipTier, DevicePlatform, WalletTransactionType, TransactionStatus, AmenityType, VoucherType, PromotionType, FeedbackType, FeedbackStatus, PowerSharingMode (Link/Loop), PowerDistributionStrategy (Average/Proportional/Dynamic), MeteringClass (Unknown/ClassB/ClassA/Class05S/Class02S), ChargingPolicyType (Phase 2: Unrestricted/OffPeakOnly/BudgetCapped)

## Driver BFF Endpoints (KLC.Driver.BFF)

- Auth: `/api/v1/auth/*` — register, verify-phone, login, refresh-token, logout, etc.
- Profile: `/api/v1/profile/*` — get/update, avatar, change-phone
- Stations: `/api/v1/stations/*` — nearby, search, detail
- Sessions: `/api/v1/sessions/*` — start, stop, active, history
- Wallet: `/api/v1/wallet/*` — balance, topup, transactions, voucher
- Favorites: `/api/v1/favorites/*` — list, add, remove
- Vehicles: `/api/v1/vehicles/*` — CRUD
- Notifications: `/api/v1/notifications/*` — list, read, preferences, devices
- Feedback: `/api/v1/feedback/*` — submit, list, FAQ
- Vouchers/Promotions: `/api/v1/vouchers/*`, `/api/v1/promotions/*`

## OCPP Integration

- WebSocket endpoint: `ws://localhost:44305/ocpp/{chargePointId}` (subprotocol: `ocpp1.6`)
- Key classes: `OcppWebSocketMiddleware`, `OcppConnectionManager` (singleton), `OcppMessageHandler` (scoped), `HeartbeatMonitorService` (hosted)
- Messages must be handled idempotently (chargers may retry)
- Persist transaction data immediately for billing accuracy
- Browser simulators available in `ocpp-simulator/Simulators/`

## Documentation (9-Layer Architecture)

| Layer | Path                            | Purpose                          |
| ----- | ------------------------------- | -------------------------------- |
| 01    | `docs/01-business/`            | BRD, stakeholders, market        |
| 02    | `docs/02-requirements/`        | Functional/non-functional reqs   |
| 03    | `docs/03-functional-specs/`    | Per-module FRS (MOD-001–015)     |
| 04    | `docs/04-architecture/`        | System, backend, DB, OCPP design |
| 06    | `docs/06-project-management/`  | Timeline, team, risks            |
| 07    | `docs/07-testing/`             | Test strategy                    |
| 08    | `docs/08-guides/`              | Dev setup, coding, deployment    |
| 09    | `docs/09-ai-playbook/`         | AI rules, patterns, anti-patterns|

`memory-bank/` contains condensed versions for AI agent context.

## AI Playbook — READ BEFORE CODING

- **Rules (MUST):** `docs/09-ai-playbook/rules/_master-rules.md`
- **Anti-patterns (NEVER):** `docs/09-ai-playbook/anti-patterns/_index.md`
- **Patterns (USE):** `docs/09-ai-playbook/patterns/_index.md`
- **Lessons (LEARN):** `docs/09-ai-playbook/lessons-learned/_index.md`
- **Debug (FIX):** `docs/09-ai-playbook/debug-playbooks/_index.md`

### Before implementing ANY feature:

1. Read FRS module: `docs/03-functional-specs/modules/MOD-{NNN}.md`
2. Read rules: `docs/09-ai-playbook/rules/_master-rules.md`
3. Check anti-patterns: `docs/09-ai-playbook/anti-patterns/`
4. Use prompt template if available: `docs/09-ai-playbook/prompts/`

### When you encounter an error:

1. Check `docs/09-ai-playbook/debug-playbooks/` FIRST
2. If new error → fix → create LL-NNN lesson learned
3. If pattern found → create PAT-NNN pattern
4. Update `_master-rules.md`

### Key Anti-Patterns (NEVER do these):

- **AP-001**: Business logic in Application Service → put in Domain Entity/Service
- **AP-002**: Exposing domain entities via API → map to DTOs
- **AP-003**: Offset-based pagination → use cursor-based
- **AP-004**: Hardcoded UI strings → use `L["Key"]` via `IStringLocalizer`
- **AP-005**: Manual database changes → use code-first migrations

## Functional Modules (MOD-001 to MOD-015)

Station Management, Connector Management, Real-time Monitoring, Energy Metering, Fault Management, OCPP Integration, Tariff Management, Payment & Billing, Vehicle Management, Charging Session, User Account, Notifications, Station Grouping, Audit Log, E-Invoice. Phase 1 critical: MOD-001, 002, 003, 006, 008, 010.

Phase 2 modules:
- **MOD-016 Power Sharing**: Manage PowerSharingGroups, assign connectors, configure strategies (EqualSplit/PriorityBased/FirstComeFirstServed), real-time rebalancing via SetChargingProfile on session start/stop
- **MOD-017 Dynamic Load Balancing**: Site-level load management via SiteLoadProfile, smart meter polling, grid capacity enforcement, automatic power distribution across chargers
- **MOD-018 Fleet Management**: Fleet CRUD, vehicle-driver assignment, daily charging limits, station group restrictions, monthly budget caps, charging policy enforcement
- **MOD-019 Operator API**: Third-party operator onboarding, API key auth, rate limiting, scoped station access, webhook event delivery, session/billing data export
