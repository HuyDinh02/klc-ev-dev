# ADR-005: CQRS (Command Query Responsibility Segregation) with MediatR for Dual API Architecture

> Status: ACCEPTED | Date: 2026-03-01

## Context

The CSMS operates two APIs with different traffic profiles and optimization needs:

**Admin API (Port 5000)**
- Operational writes: Create stations, manage users, configure pricing
- Moderate traffic (~100 req/sec peak)
- Latency tolerance: 500ms acceptable
- Complex business logic in command handlers
- Audit logging required for all mutations

**Driver BFF (Port 5001)**
- Read-heavy: Fetch stations, availability, pricing, transaction history
- High traffic potential (~10K req/sec for popular stations)
- Latency-critical: <100ms required (mobile users)
- Limited mutations (submit session request, rate charger)
- Optimized for cursor-based pagination and caching

**Requirements:**
- **Separate command/query paths** to optimize independently
- **Eventual consistency model** between write (primary) and read (replicas/cache)
- **Clear command boundaries** for audit logging, validation, authorization
- **Event-driven foundation** for Phase 2 microservices migration
- **Pipeline extensibility** to add cross-cutting concerns (validation, caching, logging)

**Architectural approaches:**
1. **Traditional N-tier** — Bloats service layer; queries are second-class citizens
2. **Repository pattern alone** — Doesn't separate read/write optimization
3. **CQRS with MediatR** — Clean separation; supports dual API architecture
4. **Event Sourcing + CQRS** — Complex; overkill for Phase 1
5. **GraphQL** — Solves read problem but doesn't optimize command path; adds complexity

## Decision

**Implement CQRS (Command Query Responsibility Segregation) using MediatR library to:**
- Separate commands (writes) from queries (reads) at application layer
- Enable independent optimization of Admin API (write-heavy) and Driver BFF (read-heavy)
- Support PostgreSQL read replicas (Driver BFF queries replica; commands go to primary)
- Prepare event-driven skeleton for Phase 2 microservices

### Rationale

1. **Command/Query Separation Mirrors Infrastructure**
   ```
   Admin API Commands → Primary Database (writes)
   Driver BFF Queries → Read Replica (eventually consistent, but low latency)
   ```
   - Commands route to primary for consistency
   - Queries route to replica for speed (100ms lag acceptable for most use cases)
   - Cache layer (Redis) bridges gap for time-sensitive reads

2. **MediatR Is the Standard .NET CQRS Library**
   - Lightweight, focused library (not framework bloat)
   - No magic; explicit `ICommand<T>`, `IQuery<T>` interfaces
   - Pipeline behaviors enable cross-cutting concerns (validation, caching, logging)
   - Integrates seamlessly with ABP, dependency injection, Autofac
   - Active community; thousands of .NET projects use it

3. **Clean Command Handlers for Business Logic**
   ```csharp
   public class StartChargingSessionCommand : ICommand<SessionStartedResponse> {
       public Guid StationId { get; set; }
       public Guid DriverId { get; set; }
       // Validation in handler, not service layer
   }

   public class StartChargingSessionHandler
       : ICommandHandler<StartChargingSessionCommand, SessionStartedResponse> {
       public async Task<SessionStartedResponse> Handle(...) {
           // 1. Validate authorization
           // 2. Fetch station state
           // 3. Check availability
           // 4. Create session entity
           // 5. Publish domain event
           // 6. Return response
       }
   }
   ```
   - No "service layer" bloat; handler IS the service
   - Explicit intent (command = action, query = data retrieval)
   - Easy to test in isolation

4. **Queries Optimized for Read Replicas & Caching**
   ```csharp
   public class GetStationDetailsQuery : IQuery<StationDetailsDto> {
       public Guid StationId { get; set; }
   }

   public class GetStationDetailsHandler
       : IQueryHandler<GetStationDetailsQuery, StationDetailsDto> {
       public async Task<StationDetailsDto> Handle(...) {
           // Fetch from read replica (not primary)
           // Cache result in Redis for 5 minutes
           // Support cursor-based pagination
       }
   }
   ```
   - Driver BFF can route queries to read replica
   - Redis caching layer is transparent
   - Denormalized read models possible (Phase 2)

5. **Pipeline Behaviors Enable Cross-Cutting Concerns**
   ```csharp
   public class ValidationBehavior<TRequest, TResponse>
       : IPipelineBehavior<TRequest, TResponse> {
       // Validates all commands
   }

   public class CachingBehavior<TRequest, TResponse>
       : IPipelineBehavior<TRequest, TResponse> {
       // Caches query results
   }

   public class AuditingBehavior<TRequest, TResponse>
       : IPipelineBehavior<TRequest, TResponse> {
       // Logs all commands for compliance
   }
   ```
   - No duplication of validation logic
   - Consistent cross-cutting behavior across all handlers
   - Easy to add new concerns (auth, rate limiting, telemetry)

6. **Event-Driven Skeleton (Phase 1 → Phase 2)**
   - Commands publish domain events (after aggregates mutate)
   - Events are handled in-process (Phase 1)
   - Same handlers route to message bus (RabbitMQ/MassTransit) in Phase 2
   - Phase 2 microservices subscribe to events; no code changes to handlers

7. **Admin API + Driver BFF Distinction Clear**
   ```
   Admin API (Port 5000)
   ├── ICommand handlers (mutation validation, business rules)
   ├── IQuery handlers (admin reporting, complex filters)
   └── Domain events (published for operator audit trail)

   Driver BFF (Port 5001)
   ├── IQuery handlers only (no mutations except edge cases)
   ├── Redis caching pipeline behavior
   └── Read replica routing
   ```

8. **Testing Is Straightforward**
   - Handler unit tests: Inject mocks, call handler
   - Command validation tests: Create command, expect validation exception
   - Query performance tests: Measure response time against replica

## Consequences

### Positive

- ✅ **Clear Command Semantics**: Explicit `ICommand` = intent to mutate; no ambiguity
- ✅ **Read Optimization Independence**: Driver BFF queries use replicas/cache without affecting commands
- ✅ **Pipeline Extensibility**: Cross-cutting concerns (validation, caching, logging) applied uniformly
- ✅ **Event Foundation**: Commands publish events; Phase 2 microservices consume them
- ✅ **Testing Simplicity**: Unit test handlers in isolation; no service layer mocking
- ✅ **Audit Trail**: All commands logged via pipeline behavior; compliance requirement met
- ✅ **Team Clarity**: Command = change, Query = read; developers understand responsibility separation
- ✅ **Dual API Support**: Admin API and Driver BFF have optimized paths (command vs. query)
- ✅ **Reduced N+1 Queries**: Explicit query handlers encourage data loading discipline
- ✅ **Scalability**: Read replicas support high-volume queries; primary handles manageable write volume

### Negative

- ❌ **More Classes**: Command, Handler, Response classes per use case; verbose compared to traditional service
- ❌ **Learning Curve**: Team must understand CQRS concepts (different from traditional N-tier)
- ❌ **Over-Engineering Simple Queries**: Simple "get by ID" queries still need IQuery + Handler boilerplate
- ❌ **Eventual Consistency Complexity**: Queries may see stale data from replica; must document acceptable lag
- ❌ **Debugging Distributed Concerns**: Error across multiple handlers requires tracing through pipeline
- ❌ **Command Validation**: Complex validation logic scattered across handlers; no centralized view
- ❌ **MediatR Dependency**: Switching away from MediatR later is refactoring-heavy

### Risks

| Risk | Mitigation |
|------|-----------|
| **Query handlers access stale replica data** | Document replica lag (100ms); cache critical reads in Redis; implement eventual consistency in UI |
| **Command handlers become too complex** | Domain events decouple handlers; delegate side effects to event handlers |
| **Cross-cutting concerns applied inconsistently** | Pipeline behaviors enforce order; code review checklist for all handlers |
| **Command validation scattered across handlers** | Create shared validators; inject via pipeline behavior |
| **Too much boilerplate for simple operations** | Code generation templates (T4, Roslyn) can reduce ceremony |
| **Debugging errors in nested pipeline behaviors** | Structured logging at each pipeline stage; MiniProfiler integration for visibility |

## Dual API Implementation Pattern

### Admin API (Full CQRS)

```csharp
[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase {
    private readonly IMediator _mediator;

    // WRITE: Fully validating command
    [HttpPost]
    public async Task<ActionResult<CreateStationResponse>> CreateStation(
        CreateStationCommand command) {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetStation), result);
    }

    // READ: Complex query (operator reports)
    [HttpGet("revenue/monthly")]
    public async Task<ActionResult<MonthlyRevenueDto>> GetMonthlyRevenue(
        GetMonthlyRevenueQuery query) {
        return await _mediator.Send(query);
    }
}
```

### Driver BFF (Query-Heavy)

```csharp
[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase {
    private readonly IMediator _mediator;

    // READ: Optimized for mobile (cached, replica)
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300)] // 5-minute cache
    public async Task<ActionResult<StationDetailsDto>> GetStationDetails(
        Guid id) {
        var query = new GetStationDetailsQuery { StationId = id };
        return await _mediator.Send(query);
    }

    // LIMITED WRITE: Session request (minimal validation)
    [HttpPost("sessions/start")]
    public async Task<ActionResult<SessionStartResponse>> StartSession(
        StartSessionCommand command) {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSessionStatus), result);
    }
}
```

## Pipeline Behavior Order (Execution Order)

```
Request
  ↓
[1] Validation Pipeline Behavior
  ├─ Validate command/query syntax
  ├─ Check authorization
  └─ If invalid → return error
  ↓
[2] Caching Pipeline Behavior (queries only)
  ├─ Check Redis for cached result
  ├─ If hit → return cached
  └─ If miss → continue
  ↓
[3] Audit Pipeline Behavior (commands only)
  ├─ Log command received
  ├─ Capture user context
  └─ Continue
  ↓
[4] Handler Execution
  ├─ Command handler → Primary DB
  ├─ Query handler → Read Replica
  └─ Return result
  ↓
[5] Caching Pipeline Behavior (queries only)
  ├─ Cache result in Redis
  └─ Return to caller
  ↓
Response
```

## Migration Path to Event-Driven (Phase 2)

**Phase 1 (In-Process Events):**
```csharp
public class StartChargingSessionHandler : ICommandHandler<StartChargingSessionCommand> {
    private readonly IMediator _mediator;

    public async Task<Unit> Handle(StartChargingSessionCommand request, CancellationToken ct) {
        // Create session
        // Publish event (in-process)
        await _mediator.Publish(new SessionStartedDomainEvent(sessionId, driverId));
        return Unit.Value;
    }
}

// Event handler runs immediately, in same transaction
public class SessionStartedEventHandler : INotificationHandler<SessionStartedDomainEvent> {
    public async Task Handle(SessionStartedDomainEvent evt, CancellationToken ct) {
        // Update read model, send notification, etc.
    }
}
```

**Phase 2 (Distributed Events via MassTransit):**
```csharp
// Same domain event, but routed via RabbitMQ
await _mediator.Publish(new SessionStartedDomainEvent(...));

// Event published to message bus
// SessionService microservice subscribes:
public class SessionStartedConsumer : IConsumer<SessionStartedEvent> {
    public async Task Consume(ConsumeContext<SessionStartedEvent> context) {
        // Handle in separate service
    }
}
```

## Related Decisions

- **ADR-001**: ABP Framework integrates MediatR seamlessly
- **ADR-002**: Queries route to PostgreSQL read replicas; commands go to primary
- **ADR-004**: Modular Monolith modules are organized around CQRS command/query handlers

## References

- [CQRS Pattern (Martin Fowler)](https://martinfowler.com/bliki/CQRS.html)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [MediatR Pipeline Behaviors](https://github.com/jbogard/MediatR/wiki/Behaviors)
- [MediatR with ABP Framework Integration](https://docs.abp.io/en/abp/latest/Domain-Driven-Design)
- [Event-Driven Architecture (Chris Richardson)](https://microservices.io/patterns/data/event-driven-architecture.html)
- [Read Models in CQRS (Greg Young)](https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf)
- [Domain Events (Vaughn Vernon, Domain-Driven Design)](https://vaughnvernon.com/)
