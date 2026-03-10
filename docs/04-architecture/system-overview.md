# System Overview

> Status: APPROVED | Last Updated: 2026-03-01 | Source: Kickoff Document

---

## 1. Architecture Philosophy

EmeSoft proposes building the EV Charging Management System with a **CSMS-centric architecture**, combining modular design, cloud-based deployment, and microservices readiness. The architecture ensures stable operations, flexible scaling, and suitability for the Vietnam market.

### Design Principles
- **CSMS-centric:** CSMS is the central orchestrator for all system operations
- **Modular & Scalable:** Components are clearly separated, allowing independent scaling
- **Cloud-based:** Flexible deployment scaling with station and user growth (AWS)
- **Vietnam-market ready:** Pre-integrated payment, e-invoicing, and operations for Vietnam
- **Operational stability first:** Prioritize stable, easy-to-maintain operations in Phase 1

---

## 2. Logical Architecture

The system consists of three logical layers:

### CSMS (Core Platform)
The core platform responsible for OCPP 1.6J communication with chargers, station/session/capacity/status management, payment processing and pricing policies, and data services for all interaction channels.

### User & Operations Interaction
Users interact through the Mobile App (iOS & Android) for EV drivers and the Admin/Ops Portal for operations and technical staff. All channels communicate indirectly through CSMS for security and control.

### Enterprise Integration & Data
Built on CSMS, the system supports payment gateway integration (ZaloPay, MoMo, OnePay), e-invoice and accounting integration (MISA, Viettel, VNPT), operational and financial data collection and storage, and reporting, analytics, and future AI capabilities.

---

## 3. Dual-API Architecture

| Component | Port | Architecture | Purpose |
|-----------|------|-------------|---------|
| Admin API | 5000 | Full ABP layered (DDD) | Admin portal, full CRUD, complex operations |
| Driver BFF API | 5001 | .NET Minimal API | Mobile app, Redis cache-first, read replicas |
| Shared Domain | — | ABP Domain layer | Shared entities, domain services, business rules |

---

## 4. Technology Stack Overview

| Layer | Technology |
|-------|-----------|
| Backend & Core | .NET 10, C# 13, ABP Framework, ASP.NET Core Web API |
| OCPP Protocol | OCPP 1.6J (JSON over WebSocket) |
| Real-time | WebSocket (OCPP) + SignalR (portal/app updates) |
| Frontend & Portal | React.js, Next.js, TailwindCSS |
| Mobile App | React Native (single codebase for iOS & Android) |
| Database | PostgreSQL (EF Core + ABP), Read Replicas for BFF |
| Caching | Redis (sessions, hot data) |
| Cloud | AWS (Docker, ALB, CloudWatch) |
| CQRS | MediatR |
| Payments | ZaloPay, MoMo, OnePay |
| E-Invoice | MISA, Viettel, VNPT |
| Maps | Google Maps API |
| Push Notifications | Firebase Cloud Messaging (FCM) |

---

## 5. Future Capabilities

The architecture is designed for future expansion including operational and business data analytics, data-driven decision support, energy optimization and station scaling, and AI capabilities integrated natively into CSMS (not separate services).

---

## 6. Phase 2 Architecture Evolution

Phase 2 extends the modular monolith with new domain services, a third API surface, and a new bounded context. The core dual-API architecture remains unchanged; Phase 2 adds capabilities alongside it.

### New Domain Services

**Power Sharing and Load Balancing** are introduced as domain services within `KLC.Domain`:

- **PowerSharingDomainService**: Manages power distribution across connectors within a `PowerSharingGroup`. Triggered on session start/stop events. Calculates per-connector power limits based on the group's strategy (EqualSplit, PriorityBased, FirstComeFirstServed) and pushes `SetChargingProfile` commands via OCPP.
- **SiteLoadBalancingService**: A hosted background service that polls smart meters (via `SiteLoadProfile.SmartMeterEndpoint`) and dynamically adjusts charger power limits to keep total site consumption within grid capacity. Uses OCPP `SetChargingProfile` at a lower stack level than power sharing.

Both services integrate with the existing `OcppConnectionManager` and `OcppRemoteCommandService` to send commands to connected chargers.

### Operator API — New API Gateway

Phase 2 introduces a third API surface for third-party operators:

| Component | Port | Architecture | Purpose |
|-----------|------|-------------|---------|
| Operator API | 5002 | .NET Minimal API | External operator access, API key auth, rate-limited |

The Operator API provides:
- Station status and availability queries (scoped to operator's `AllowedStationIds`)
- Remote start/stop commands
- Session history and billing data
- Webhook registration for event callbacks (session lifecycle, faults)
- API key authentication with per-operator rate limiting (`Operator.RateLimitPerMinute`)

This API consumes the same shared domain layer (`KLC.Domain`, `KLC.Application`) as the Admin API and Driver BFF, maintaining the single-domain-model principle.

### Fleet Management — New Bounded Context

Fleet management is added as a new bounded context within the existing domain:

- **Fleet**: Corporate fleet with budget caps, charging policies, and operator ownership
- **FleetVehicle**: Links vehicles and drivers to fleets with per-driver charging limits and station group restrictions
- **Operator**: Third-party entity with API credentials, station access controls, and webhook configuration

Fleet management integrates with:
- **Sessions**: Enforces `DailyChargingLimitKwh` and `ChargingPolicy` before authorizing a charge
- **Payments**: Aggregates fleet charging costs against `MaxMonthlyBudget`
- **Stations**: Validates `AllowedStationGroupIds` during station access checks
- **Notifications**: Alerts fleet managers when budget thresholds are approached

### Phase 2 Component Diagram

```
                    ┌─────────────────┐
                    │  Admin Portal   │
                    │  (port 3001)    │
                    └────────┬────────┘
                             │
┌──────────────┐    ┌────────▼────────┐    ┌─────────────────┐
│  Driver App  ├───►│   Admin API     │◄───┤  OCPP Chargers  │
│              │    │  (port 44305)   │    │  (WebSocket)    │
└──────┬───────┘    └────────┬────────┘    └─────────────────┘
       │                     │
       │            ┌────────▼────────┐
       └───────────►│  Driver BFF     │
                    │  (port 5001)    │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐    ┌─────────────────┐
                    │  Shared Domain  │◄───┤  Operator API   │  ← Phase 2
                    │  (KLC.Domain)   │    │  (port 5002)    │
                    └─────────────────┘    └─────────────────┘
```
