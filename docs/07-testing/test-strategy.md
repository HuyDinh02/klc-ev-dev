# Test Strategy

> Status: APPROVED | Last Updated: 2026-03-01

---

## Testing Levels

### 1. Unit Tests
- **Scope:** Domain logic, entity business rules, value objects
- **Tools:** xUnit, ABP Test Infrastructure
- **Coverage Target:** 80%+ for domain layer
- **Focus:** Entity creation, state transitions, validation rules, calculations (tariff, billing)

### 2. Integration Tests
- **Scope:** Application services, API endpoints, database operations
- **Tools:** xUnit, ABP Integration Test Infrastructure, TestContainers
- **Coverage Target:** 70%+ for application services
- **Focus:** CRUD operations, CQRS handlers, payment flow, auth flows

### 3. E2E Tests
- **Scope:** Critical user flows end-to-end
- **Coverage Target:** 100% for critical paths
- **Focus:** Complete charging session flow (start → meter → stop → pay → invoice), user registration → first charge, station registration → monitoring

### 4. Performance Tests
- **Scope:** API latency, WebSocket concurrency, database query performance
- **Tools:** k6 or similar
- **Targets:** API response < 200ms (p95), 1000+ concurrent WebSocket connections, payment processing < 3s

### 5. OCPP Protocol Tests
- **Scope:** All OCPP message types and flows
- **Tools:** OCPP test simulator
- **Focus:** Normal flows, error handling, reconnection, message retry, idempotency

## Test Environments
- **Local:** Docker Compose (PostgreSQL + Redis)
- **CI:** GitHub Actions with TestContainers
- **Staging:** AWS environment mirroring production
