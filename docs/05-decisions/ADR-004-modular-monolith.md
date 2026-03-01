# ADR-004: Modular Monolith Architecture for Phase 1 (Transition to Microservices in Phase 2+)

> Status: ACCEPTED | Date: 2026-03-01

## Context

Building a CSMS with evolving business requirements requires balancing:
- **Development Speed**: Phase 1 (2026) needs MVP in 3-4 months; microservices slow this down
- **Team Size**: Current team is 6-8 developers; can't maintain 10+ microservices reliably
- **Operational Complexity**: DevOps team (1-2 people) cannot manage distributed tracing, service discovery, inter-service communication for startups
- **Shared Domain Logic**: Stations, Sessions, Billing, Users are tightly coupled; splitting too early causes version hell
- **Cost**: Infrastructure for microservices (k8s, service mesh, monitoring) is expensive for startup
- **Future Scalability**: Some services will need independent scaling; architecture should support eventual extraction

**Architectural options:**
1. **Monolith** (single deployable) — Fast but rigid; hard to scale individual features
2. **Modular Monolith** (ABP modules) — Fast + modular; can extract modules later
3. **Microservices** (distributed from day 1) — Scalable but slow to develop, expensive to operate
4. **Serverless** (Lambda/Functions) — Pay-as-you-go but cold starts, vendor lock-in
5. **Event-Driven** (CQRS + async messaging) — Flexible but complex; hard to debug

## Decision

**Implement a modular monolith using ABP Framework's module system in Phase 1, with explicit design patterns that enable extraction to microservices in Phase 2 (2027+).**

### Rationale

1. **ABP Module System Enforces Boundaries**
   - Each domain module (StationModule, SessionModule, BillingModule) has separate:
     - Entities, Aggregates, Value Objects
     - Application Services, Queries, Commands
     - Infrastructure (Repositories, Event Handlers)
     - DTOs, API endpoints
   - Modules cannot directly reference each other's domain models; only DTOs
   - Clear module contracts prevent spaghetti code

2. **Faster Development (MVP in 3 months)**
   - Single deployment pipeline; no orchestration needed
   - Shared database; no eventual consistency complications
   - Transactional integrity across modules (e.g., Session + Billing in one transaction)
   - Developers can focus on business logic, not infrastructure

3. **Operational Simplicity**
   - One Docker container; one load balancer
   - One database; one backup strategy
   - Centralized logging (Serilog to file/Seq)
   - Single health check endpoint
   - 1-2 DevOps engineers can manage; no k8s complexity

4. **Phase 2+ Migration Path Built-In**
   - Each module can be extracted as a microservice with minimal refactoring
   - Module-to-service extraction is well-documented (strangler fig pattern)
   - Event-driven communication (using MassTransit or NServiceBus) replaces direct calls
   - Database split happens gradually (database-per-service)

5. **Shared Domain Logic Stays Cohesive**
   - Station data + Session data + Billing data are accessed transactionally
   - Foreign keys enforce referential integrity
   - Easier to reason about business logic (e.g., "Can a session be billed twice?" → DB constraint)
   - No distributed transaction coordination needed (Phase 1)

6. **Cost-Effective**
   - Runs on single Azure App Service or AWS EC2 instance
   - Database replication (PostgreSQL read replicas) handles read scaling
   - Redis cache handles spike traffic
   - No container orchestration, service mesh, or observability overhead
   - Startup can deploy to 2-3 production nodes with load balancer

7. **Testing is Straightforward**
   - Integration tests can spin up single in-memory database
   - End-to-end tests exercise entire CSMS with one API container
   - No mock services, contract testing, or distributed tracing complexity
   - Developers can run full stack locally in Docker Compose

## Consequences

### Positive

- ✅ **Fast MVP Delivery**: 3-4 month timeline achievable; no infrastructure delays
- ✅ **Easy Debugging**: Single process; stack traces are end-to-end; breakpoint debugging works
- ✅ **Transactional Integrity**: Sessions + Billing in one ACID transaction; no saga pattern complexity
- ✅ **Shared Code**: No code duplication; library/utility code lives in common project
- ✅ **Team Collaboration**: Developers can work on different modules without coordination
- ✅ **Cost-Effective**: Scales with 2-3 instances + load balancer; no k8s expense
- ✅ **Clear Module Boundaries**: ABP enforces separation; easy to identify extraction points later
- ✅ **Operational Simplicity**: Single deployment; centralized logging, monitoring, backup

### Negative

- ❌ **Scaling Limits**: By Q4 2026, if traffic explodes, single process becomes bottleneck
- ❌ **Deployment Coupling**: Deploying BillingModule requires restarting entire app; brief downtime
- ❌ **Resource Contention**: Heavy Reporting task steals CPU from real-time API; no independent scaling
- ❌ **Technology Stack Lock-In**: All modules must use .NET/C#; cannot adopt Go/Rust/Node.js for specific services
- ❌ **Vendor Lock-In (Database)**: PostgreSQL schema is shared; extracting a module means schema splitting (complex)
- ❌ **Debugging Distributed Concerns**: If SessionModule talks to BillingModule via API (future), cross-service debugging is hard
- ❌ **Eventual Consistency Gap**: Phase 1 modules are tightly coupled; Phase 2 migration to async events is refactoring-heavy

### Risks

| Risk | Mitigation |
|------|-----------|
| **Module boundaries blur; spaghetti code emerges** | Code review checklist: "No cross-module entity references, only DTOs"; ADR-005 (CQRS) enforces separation |
| **Traffic spike in Q4 2026 overloads monolith** | Plan Phase 2 microservices extraction in Q3 2026; start with SessionModule (heaviest) extraction |
| **Shared database becomes bottleneck** | Implement read replicas (ADR-002) for read-heavy queries; cache in Redis; denormalize reporting tables |
| **Monolith deployment takes 10 min; downtime unacceptable** | Implement zero-downtime deployment (blue-green) in Q2 2026; use database migrations that are backward-compatible |
| **Adding new feature requires touching 5 modules** | Module design review in Phase 1; establish "BoundaryContexts" map showing feature dependencies |
| **Extracting a module requires major refactoring** | Build event-driven skeleton in Phase 1 (inactive); activate in Phase 2 when extracting |

## Phase 1 Module Architecture

```
EVCharging.Monolith
├── EVCharging.Core                    [Shared: Domain models, interfaces]
│
├── EVCharging.Station.Module           [Station management]
│   ├── Domain
│   ├── Application (CQRS handlers)
│   ├── Infrastructure
│   └── HttpApi
│
├── EVCharging.Session.Module           [Charging session lifecycle]
│   ├── Domain
│   ├── Application (CQRS handlers)
│   ├── Infrastructure
│   └── HttpApi
│
├── EVCharging.Billing.Module           [Payment, invoicing, transactions]
│   ├── Domain
│   ├── Application (CQRS handlers)
│   ├── Infrastructure
│   └── HttpApi
│
├── EVCharging.Identity.Module          [Users, roles, permissions]
│   ├── Domain
│   ├── Application (CQRS handlers)
│   ├── Infrastructure
│   └── HttpApi
│
├── EVCharging.OCPP.Module              [OCPP charger communication]
│   ├── Domain
│   ├── Application (CQRS handlers)
│   ├── Infrastructure (WebSocket handlers)
│   └── HttpApi
│
├── EVCharging.Reporting.Module         [Analytics, dashboards]
│   ├── Domain
│   ├── Application (read models)
│   ├── Infrastructure (async jobs)
│   └── HttpApi
│
├── EVCharging.Admin.HttpApi.Host       [Admin API - ABP layered]
│   └── Merges all modules
│
└── EVCharging.Driver.BFF               [Driver mobile BFF - Minimal API]
    └── Queries only; calls Admin via HTTP
```

## Phase 2+ Microservices Transition (Illustrative)

**Timeline: Q3 2026 onwards**

```
Monolith (Phase 1)         Transition (Phase 2)           Microservices (Phase 3)
┌─────────────┐            ┌─────────────────┐           ┌────────────────────┐
│ Monolith    │            │ Extract         │           │ Microservices      │
│ (All)       │  ──────→   │ SessionModule   │  ──────→  │ SessionService     │
│             │            │ as service      │           │ BillingService     │
│             │            │ (stranger fig)  │           │ ReportingService   │
└─────────────┘            │                 │           │ (etc.)             │
                           │ Keep Monolith   │           │ + CSMS Monolith    │
                           │ for other       │           │   (stations, auth)  │
                           │ modules         │           │                     │
                           └─────────────────┘           └────────────────────┘

                           RabbitMQ / MassTransit
                           (async events between services)
```

**Extraction pattern:**
1. Build SessionService alongside Session.Module (both read/write same DB table)
2. New operations route to SessionService; old code stays in monolith
3. Gradually migrate monolith code to ServiceB
4. Once 100% routed to service, remove from monolith
5. Move SessionService database schema to separate DB

## Module Communication Patterns (Phase 1)

```csharp
// DIRECT CALLS (Phase 1 - in-process)
public class BillingAppService {
    public async Task ChargeSession(ChargingSessionDto session) {
        // Direct call to SessionModule
        var sessionState = await _sessionService.GetSessionState(session.Id);
        // Create invoice directly
        await _invoiceRepository.InsertAsync(...);
    }
}

// PHASE 2 PREPARATION (event-driven, inactive in Phase 1)
public class BillingAppService {
    private readonly IDistributedEventBus _eventBus;

    public async Task ChargingSessionCompleted(ChargingSessionCompletedEto evt) {
        // Async event from SessionModule
        // When SessionModule extracts to microservice, this event is published via RabbitMQ
        await CreateInvoiceAsync(evt.SessionId, evt.Energy, evt.Duration);
    }
}
```

## When to Transition to Microservices

**Trigger criteria for Phase 2 extraction (Q3-Q4 2026):**
- SessionModule receives >1000 req/sec; monolith CPU maxes out
- Reporting module's heavy job (daily analytics) blocks API responses
- Team grows to 12+ developers; coordination overhead high
- Multiple business units (different billing models) demand independent deployments

**Modules ranked by extraction priority:**
1. **SessionModule** (heaviest traffic) → SessionService
2. **ReportingModule** (compute-intensive) → AnalyticsService
3. **BillingModule** (separate team) → BillingService
4. **Remaining** (low volume) → Stay in monolith

## Related Decisions

- **ADR-001**: ABP Framework's module system enables this architecture
- **ADR-002**: Shared PostgreSQL database; replicas for read scaling
- **ADR-005**: CQRS + MediatR enforces module separation; event-driven skeleton ready for Phase 2

## References

- [ABP Module Development](https://docs.abp.io/en/abp/latest/Module-Development-Basics)
- [Monolith to Microservices (Sam Newman)](https://samnewman.io/books/monolith-to-microservices/)
- [Strangler Fig Pattern (Martin Fowler)](https://martinfowler.com/bliki/StranglerFigApplication.html)
- [Building Microservices (Sam Newman) - Chapter 2: Microservices Architecture](https://learning.oreilly.com/library/view/building-microservices-2nd/9781492034018/)
- [Modular Monoliths (Vaughn Vernon, Domain-Driven Design)](https://vaughnvernon.com/)
