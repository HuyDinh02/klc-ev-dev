# Architecture Roadmap — KLC EV Charging

## Current State (April 2026)

```
┌─────────────────────────────────────────────────────────────────┐
│                     Google Cloud Platform                        │
│                                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐   │
│  │  Admin   │  │  Admin   │  │  Driver  │  │    OCPP      │   │
│  │  Portal  │  │  API     │  │  BFF     │  │   Gateway    │   │
│  │ Next.js  │  │  .NET    │  │  .NET    │  │    .NET      │   │
│  │ max=3    │  │  max=5   │  │  max=5   │  │   max=1      │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬───────┘   │
│       │              │              │               │            │
│       │         ┌────┴──────────────┴───────────────┴──┐        │
│       │         │         Shared Infrastructure         │        │
│       │         │  PostgreSQL + Redis + Firebase        │        │
│       │         └───────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

### Known Limitations
1. **BFF uses raw DbContext** (168 calls) — no ABP audit, no UoW
2. **OCPP Gateway max=1** — single point of failure for charger connections
3. **No event-driven architecture** — all communication is synchronous
4. **Session logic duplicated** between BFF and Admin API
5. **Same Docker image** for Admin API and OCPP Gateway — can't deploy independently
6. **No API versioning** — breaking changes affect all clients

---

## Phase 1: Foundation Fixes (May 2026 — Post-Launch Stabilization)

**Goal:** Fix tech debt from rapid development, ensure production reliability.

### 1.1 BFF → ABP Repository Migration
**Effort:** 3 days | **Impact:** High

```
Before: _dbContext.ChargingSessions.AddAsync(session)
After:  _sessionRepository.InsertAsync(session)
```

- Replace 168 raw DbContext calls with ABP IRepository
- Eliminates: manual !IsDeleted filters, SetAuditFields, no UoW
- 15 BFF service files to update
- Tests verify behavior unchanged

### 1.2 Separate OCPP Gateway Docker Image
**Effort:** 1 day | **Impact:** Medium

```
Before: Admin API Dockerfile → both services
After:  KLC.HttpApi.Host → Admin API only
        KLC.OcppGateway  → Gateway only (slim, no OpenIddict/ABP UI)
```

- Create `KLC.OcppGateway` project (references only Domain + EF Core)
- Remove ABP UI, OpenIddict, admin controllers from Gateway
- Faster startup, smaller image, independent deploy

### 1.3 API Versioning
**Effort:** 1 day | **Impact:** Medium

```
/api/v1/stations    → current
/api/v2/stations    → future breaking changes
```

- Add `Asp.Versioning.Http` package
- Version BFF endpoints: `/api/v1/*`
- Mobile app pins to v1, new features on v2

---

## Phase 2: Event-Driven Architecture (June-July 2026)

**Goal:** Decouple services, enable async processing, improve scalability.

### 2.1 Domain Events with MediatR
**Effort:** 3 days | **Impact:** High

```
Before (procedural):
  session.RecordStop() → walletService.Deduct() → paymentService.Create() → pushService.Send()

After (event-driven):
  session.RecordStop() → publishes SessionCompletedEvent
    → WalletDeductionHandler handles deduction
    → InvoiceGenerationHandler creates invoice  
    → PushNotificationHandler sends FCM
    → WebhookDeliveryHandler sends operator webhook
```

- Add `ILocalEventBus` (ABP built-in) or MediatR notifications
- Decouple session completion from payment/notification
- Each handler is independent, testable, can fail without blocking others

### 2.2 Redis Pub/Sub for OCPP Command Routing
**Effort:** 2 days | **Impact:** High

```
Before: BFF → HTTP → OCPP Gateway (single instance only)
After:  BFF → Redis Pub/Sub → any Gateway instance
```

- Publish `RemoteStartCommand` to Redis channel
- Gateway subscribes and routes to correct WebSocket
- Enables OCPP Gateway multi-instance (max=3+)
- Eliminates single point of failure

### 2.3 Background Job Queue (Redis/Hangfire)
**Effort:** 2 days | **Impact:** Medium

```
Before: Fire-and-forget Task.Run() for push/webhook
After:  Enqueue job → worker processes reliably with retry
```

- Replace `_ = Task.Run()` with proper job queue
- Retry failed push notifications, webhooks
- Dead letter queue for investigation
- Dashboard for monitoring job health

---

## Phase 3: Scalability (August-September 2026)

**Goal:** Handle 100+ stations, 10,000+ users, geographic distribution.

### 3.1 CQRS with Read Replicas
**Effort:** 3 days | **Impact:** High

```
Write: Admin API / Gateway → Primary PostgreSQL
Read:  BFF / Portal → PostgreSQL Read Replica
```

- BFF read queries go to read replica (nearby stations, session history)
- Write operations stay on primary (session create, wallet deduct)
- Reduces primary DB load by ~70%

### 3.2 Multi-Region OCPP Gateway
**Effort:** 5 days | **Impact:** High

```
Region 1 (HCM): OCPP Gateway + chargers
Region 2 (HN):  OCPP Gateway + chargers
Shared:          PostgreSQL + Redis (cross-region)
```

- Deploy Gateway per region, close to chargers
- Reduce WebSocket latency
- Redis Pub/Sub for cross-region command routing

### 3.3 API Gateway (Cloud Endpoints / Kong)
**Effort:** 3 days | **Impact:** Medium

```
Before: Load Balancer → direct to services
After:  LB → API Gateway → rate limit, auth, logging → services
```

- Centralized rate limiting
- API key management for operators
- Request/response logging
- Circuit breaker for downstream services

---

## Phase 4: Microservices Evolution (Q4 2026+)

**Goal:** True microservices for independent scaling and team ownership.

### 4.1 Extract Payment Service
```
KLC.Payment.Service
  → VnPay, MoMo, ZaloPay integration
  → Wallet management
  → Invoice generation
  → Independent DB (payment schema)
```

### 4.2 Extract Notification Service
```
KLC.Notification.Service
  → FCM push notifications
  → SMS (eSMS/SpeedSMS)
  → Email (SendGrid)
  → In-app notifications
  → Independent queue (Redis streams)
```

### 4.3 Extract Analytics Service
```
KLC.Analytics.Service
  → Time-series DB (TimescaleDB / InfluxDB)
  → Real-time dashboards
  → Revenue reports
  → Station utilization
  → Separate from OLTP database
```

---

## Priority Matrix

| Phase | Item | Effort | Impact | Priority |
|-------|------|--------|--------|----------|
| 1.1 | BFF → ABP Repos | 3d | High | **P1** |
| 1.2 | Separate Gateway image | 1d | Medium | **P1** |
| 1.3 | API versioning | 1d | Medium | P2 |
| 2.1 | Domain Events | 3d | High | **P1** |
| 2.2 | Redis OCPP routing | 2d | High | **P1** |
| 2.3 | Background jobs | 2d | Medium | P2 |
| 3.1 | CQRS read replicas | 3d | High | P2 |
| 3.2 | Multi-region Gateway | 5d | High | P3 |
| 3.3 | API Gateway | 3d | Medium | P3 |
| 4.1 | Payment Service | 5d | Medium | P4 |
| 4.2 | Notification Service | 3d | Medium | P4 |
| 4.3 | Analytics Service | 5d | Medium | P4 |

---

## Timeline

```
Apr 15, 2026:  Production launch (10 stations, 500 users)
May 2026:      Phase 1 — Foundation fixes (BFF repos, Gateway image, API versioning)
Jun-Jul 2026:  Phase 2 — Event-driven (domain events, Redis OCPP, job queue)
Aug-Sep 2026:  Phase 3 — Scalability (read replicas, multi-region, API gateway)
Q4 2026:       Phase 4 — Microservices (payment, notification, analytics)
```

## Principles

1. **Ship fast, refactor later** — April 15 launch takes priority over architecture purity
2. **Each phase must be backward compatible** — no breaking changes for mobile app
3. **Test coverage before refactoring** — 1,328+ tests provide safety net
4. **Infrastructure as Code** — Terraform for new resources (Phase 3+)
5. **Observability first** — monitoring, alerts, logging before scaling
