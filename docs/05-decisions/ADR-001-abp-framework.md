# ADR-001: Use ABP Framework for Layered Architecture & Multi-Tenancy

> Status: ACCEPTED | Date: 2026-03-01

## Context

Building an EV Charging Station Management System (CSMS) that requires:
- **Multi-tenancy support** — Different operators manage their own charging networks independently
- **Complex domain logic** — OCPP protocol handling, charging session management, billing, real-time monitoring
- **Enterprise-grade features** — Role-based access control (RBAC), audit logging, localization, soft deletes
- **Team productivity** — Fast development velocity with proven architectural patterns
- **Long-term maintainability** — Clear separation of concerns and standardized code structure

The .NET ecosystem offers multiple architectural approaches:
1. Clean Architecture from scratch
2. Domain-Driven Design (DDD) custom implementation
3. ABP (ASP.NET Boilerplate) Framework — opinionated, batteries-included
4. Alternative frameworks (Orchard Core, NServiceBus)

## Decision

**Adopt ABP Framework v8.0+ for the backend** (Admin API and Driver BFF API).

### Rationale

ABP provides a **production-ready implementation of DDD with built-in enterprise features** that align with CSMS requirements:

1. **DDD by Default**
   - Pre-built Domain Layer, Application Layer, Infrastructure Layer
   - Aggregate Root patterns with automatic ID generation (Guid)
   - Specification patterns for complex queries
   - Unit of Work pattern with repository abstraction

2. **Multi-Tenancy**
   - Automatic tenant isolation via `ITenantProvider`
   - Database-per-tenant or shared database strategies
   - Built into all modules out-of-the-box

3. **Identity & Security**
   - IdentityServer 4 integration (user management, roles, permissions)
   - Claim-based authorization
   - Two-factor authentication support

4. **Cross-Cutting Concerns**
   - Automatic audit logging (`FullAuditedAggregateRoot`)
   - Exception handling middleware
   - Validation pipeline (FluentValidation integration)
   - Background job scheduling (Hangfire/Quartz)

5. **Localization**
   - `IStringLocalizer` for all UI strings
   - Vietnamese + English built-in
   - Resource-based or database-backed translations

6. **Module System**
   - Core, Volo.Abp modules provide identity, audit, permission management
   - Custom modules for Domain (Stations, Sessions, Billing, OCPP)
   - Enables Phase 2 transition to microservices (extract modules as services)

7. **Minimal APIs Support**
   - ABP 8.0+ supports .NET Minimal APIs (lighter weight for Driver BFF)
   - Full Dependency Injection container integration
   - Automatic OpenAPI/Swagger generation

8. **Testing Support**
   - Built-in test fixtures and mocking utilities
   - Application layer testing out-of-the-box

## Consequences

### Positive

- ✅ **Faster Development**: No need to build multi-tenancy, RBAC, audit logging, localization from scratch
- ✅ **Team Familiarity**: Growing Vietnamese .NET community uses ABP; hire developers with ABP experience
- ✅ **Proven Patterns**: DDD + CQRS + modular architecture are tested at scale
- ✅ **Enterprise Features**: Permission management, feature flags, background jobs included
- ✅ **Migration Path**: Modular design supports extraction to microservices in Phase 2
- ✅ **Documentation & Examples**: Extensive ABP documentation and Volo.Abp repositories provide reference implementations
- ✅ **Open Source**: Volo.Abp is open-source; community contributions and transparency
- ✅ **Performance**: Layered caching (2nd-level cache with Redis), query optimization included

### Negative

- ❌ **Learning Curve**: Team must learn ABP conventions, module system, and DDD patterns
- ❌ **Opinionated Structure**: Less flexibility in where to place code; must follow ABP conventions
- ❌ **Boilerplate**: Generated code for entities, DTOs, repositories can feel verbose
- ❌ **Breaking Changes**: Major version upgrades (e.g., ABP 7→8) may require refactoring
- ❌ **Vendor Lock-in**: Migrating away from ABP later is costly; switching frameworks is difficult
- ❌ **Over-Engineering**: For simple features, ABP layering may feel heavyweight
- ❌ **Support Costs**: Open-source; commercial support is optional and paid

### Risks

| Risk | Mitigation |
|------|-----------|
| **Team resists ABP conventions** | Invest in 1-2 week ABP bootcamp; create internal coding standards guide (docs/08-guides/) |
| **Version upgrade breaks compatibility** | Pin ABP version; plan major version upgrades as sprint stories; test thoroughly in CI/CD |
| **Performance bottlenecks in multi-tenant queries** | Use EF Core query optimization; implement caching layers; profile with tools like MiniProfiler |
| **Difficult to extract modules to microservices** | Design modules with clear boundaries from Phase 1; avoid cross-module dependencies; use event-driven communication |
| **Insufficient documentation for custom features** | Contribute to Volo.Abp GitHub; create internal wiki (memory-bank/) for domain-specific patterns |

## Alternatives Considered

### 1. **Clean Architecture from Scratch**
**Pros:**
- Maximum flexibility; design patterns exactly as needed
- No learning curve; every layer is custom-built
- No vendor lock-in

**Cons:**
- 6-12 months to build multi-tenancy, RBAC, audit, localization reliably
- High risk of architectural mistakes (e.g., layering violations)
- Team must maintain custom patterns; harder to onboard new developers
- No battle-tested library for DDD

**Verdict:** Too slow for startup; reinvents the wheel.

---

### 2. **Entity Framework Core + Minimal APIs (No Framework)**
**Pros:**
- Lightweight; maximum control
- Easy to learn for junior developers
- Minimal dependencies

**Cons:**
- Must manually implement multi-tenancy, RBAC, audit logging
- No identity management layer
- Inconsistent patterns across team
- No module system for future extraction

**Verdict:** Works for simple projects; insufficient for enterprise CSMS.

---

### 3. **Orchard Core CMS**
**Pros:**
- Built-in multi-tenancy
- Module system + theme support
- Enterprise features out-of-the-box

**Cons:**
- Designed for CMS use cases; overhead for EV charging domain
- Limited CQRS support; forced ORM patterns
- Not ideal for real-time APIs (WebSocket, SignalR)
- Smaller community; fewer integration examples

**Verdict:** Over-optimized for content management; not suited for charging infrastructure.

---

### 4. **NServiceBus + Domain-Driven Design (Custom)**
**Pros:**
- Excellent for distributed systems and event-driven architecture
- Strong messaging patterns

**Cons:**
- Licensing costs for commercial features
- Overkill for Phase 1 (not yet distributed)
- Requires expertise in saga patterns and distributed transactions

**Verdict:** Better for Phase 2 when migrating to microservices; premature for Phase 1.

---

### 5. **ASP.NET Core with Custom Middleware (Minimal Abstraction)**
**Pros:**
- Total control; no opinions
- Lighter footprint

**Cons:**
- Reinvents ABP; best to use ABP instead
- No multi-tenancy, RBAC, audit logging built-in
- Higher maintenance burden

**Verdict:** Defeats the purpose of using a framework.

---

## Decision Makers & Review

- **Approval:** CTO, Architecture Lead
- **Implementation Owner:** Backend Team Lead
- **Review Cycle:** Quarterly; ADR-002 (PostgreSQL), ADR-004 (Modular Monolith), ADR-005 (CQRS) depend on this decision

## Related Decisions

- **ADR-004**: Modular Monolith for Phase 1 (ABP modules support this)
- **ADR-005**: CQRS with MediatR (integrates with ABP DDD)

## References

- [ABP Framework Official Docs](https://docs.abp.io/)
- [Volo.Abp GitHub](https://github.com/abpframework/abp)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [ABP Community Slack](https://community.abp.io/)
