# Backend Architecture (.NET/ABP)

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. ABP Framework Structure

The backend follows ABP Framework's DDD layered architecture:

```
src/
├── KLC.Domain.Shared/        # Constants, enums, shared DTOs
├── KLC.Domain/                # Entities, Aggregate Roots, Domain Services, Repository interfaces
├── KLC.Application.Contracts/ # Application service interfaces, DTOs
├── KLC.Application/           # Application service implementations
├── KLC.EntityFrameworkCore/   # EF Core DbContext, Repositories, Migrations
├── KLC.HttpApi/               # API Controllers
├── KLC.HttpApi.Host/          # Admin API Host (port 5000)
├── KLC.Driver.BFF/            # Driver BFF API Host (port 5001)
└── KLC.DbMigrator/            # Database migration console app
```

## 2. Layer Responsibilities

### Domain Layer
- Entities with `FullAuditedAggregateRoot<Guid>` base
- Business rules enforced in entity methods (not in application services)
- Domain services for cross-aggregate operations
- Repository interfaces (IRepository<T>)
- Domain events for inter-aggregate communication

### Application Layer
- CQRS with MediatR: IQuery<T> and ICommand handlers
- DTOs for all API input/output (never expose entities)
- AutoMapper for entity-to-DTO mapping
- ABP ApplicationService base class
- Orchestration only — no business logic

### Infrastructure Layer
- EF Core with PostgreSQL
- Repository implementations
- Redis caching integration
- OCPP WebSocket handler service
- Payment gateway client services
- E-invoice integration services

### Presentation Layer
- Admin API: Full ABP REST controllers (port 5000)
- Driver BFF: .NET Minimal API with Redis cache-first (port 5001)
- Swagger/OpenAPI documentation on both

## 3. CQRS Pattern

```
Request → MediatR Pipeline → Handler → Repository/Cache → Response
```

All queries and commands go through MediatR pipeline behaviors for validation, logging, and caching.

## 4. Key Conventions
- Cache key format: `"entity:{id}:field"`
- Error format: `{ "code": "MOD_001", "message": "...", "details": {} }`
- Pagination: cursor-based (not offset)
- API versioning on all endpoints
- All UI strings via IStringLocalizer
