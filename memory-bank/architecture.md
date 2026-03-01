# Architecture

## Pattern
CSMS-centric, modular monolith (Phase 1), designed for microservices evolution.

## Dual API Design
| API | Port | Style | Purpose |
|-----|------|-------|---------|
| Admin API | 5000 | Full ABP DDD layers | Admin portal, complex CRUD |
| Driver BFF | 5001 | .NET Minimal API | Mobile app, Redis cache-first, read replicas |

## Shared Domain Layer
Both APIs share: Domain entities (FullAuditedAggregateRoot<Guid>), Domain services, Repository interfaces, Domain events.

## ABP Project Structure
```
src/
├── KCharge.Domain.Shared/        # Enums, constants
├── KCharge.Domain/                # Entities, domain services, repo interfaces
├── KCharge.Application.Contracts/ # DTOs, service interfaces
├── KCharge.Application/           # Service implementations, MediatR handlers
├── KCharge.EntityFrameworkCore/   # DbContext, repos, migrations
├── KCharge.HttpApi/               # API controllers
├── KCharge.HttpApi.Host/          # Admin API host
├── KCharge.Driver.BFF/            # Driver BFF host
└── KCharge.DbMigrator/            # Migration tool
```

## Key Patterns
- CQRS: IQuery<T>/ICommand → MediatR handlers
- Cache-first: Redis → DB fallback (BFF)
- Cache key: "entity:{id}:field"
- Cursor-based pagination
- Domain events for cross-aggregate communication
