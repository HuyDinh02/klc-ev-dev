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
├── KLC.Domain.Shared/        # Enums, constants
├── KLC.Domain/                # Entities, domain services, repo interfaces
├── KLC.Application.Contracts/ # DTOs, service interfaces
├── KLC.Application/           # Service implementations, MediatR handlers
├── KLC.EntityFrameworkCore/   # DbContext, repos, migrations
├── KLC.HttpApi/               # API controllers
├── KLC.HttpApi.Host/          # Admin API host
├── KLC.Driver.BFF/            # Driver BFF host
└── KLC.DbMigrator/            # Migration tool
```

## Key Patterns
- CQRS: IQuery<T>/ICommand → MediatR handlers
- Cache-first: Redis → DB fallback (BFF)
- Cache key: "entity:{id}:field"
- Cursor-based pagination
- Domain events for cross-aggregate communication
