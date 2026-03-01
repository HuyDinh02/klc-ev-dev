# Test Plan

> Status: APPROVED | Last Updated: 2026-03-01

## Executive Summary
Comprehensive testing strategy for EV Charging Station Management System (CSMS) covering 15 modules across Phase 1 (MVP). Targets 80% unit test coverage, 70% integration coverage, and critical E2E flow validation. Test execution aligned with milestone schedule (prototype Mar 15, station mgmt Mar 31, monitoring Apr 30, MVP May 31).

---

## 1. Test Scope & Coverage

### 1.1 Modules Under Test (15 Total)

| Module | ID | Phase | Unit Coverage | Integration | E2E |
|--------|-----|-------|---|---|---|
| Station Management | MOD-001 | Phase 1 | 80% | 70% | Yes |
| Connector Management | MOD-002 | Phase 1 | 80% | 70% | Yes |
| Real-time Monitoring | MOD-003 | Phase 1 | 75% | 65% | Yes |
| Energy Metering | MOD-004 | Phase 1 | 85% | 75% | Yes |
| Fault Management | MOD-005 | Phase 1 | 80% | 70% | Yes |
| OCPP 1.6J Integration | MOD-006 | Phase 1 | 90% | 80% | Yes |
| Tariff Configuration | MOD-007 | Phase 1 | 85% | 75% | Yes |
| Payment & Billing | MOD-008 | Phase 1 | 80% | 60% | Partial* |
| Vehicle Management | MOD-009 | Phase 1 | 85% | 75% | Yes |
| Charging Session | MOD-010 | Phase 1 | 85% | 75% | Yes |
| User Account | MOD-011 | Phase 1 | 80% | 70% | Yes |
| Notifications | MOD-012 | Phase 2 | 70% | 50% | Limited |
| Station Grouping | MOD-013 | Phase 1 | 75% | 65% | Limited |
| Audit Logging | MOD-014 | Phase 1 | 80% | 70% | Yes |
| E-Invoice Integration | MOD-015 | Phase 1 | 75% | 60% | Partial* |

*Partial: External gateway mocking required for payment/invoice testing

### 1.2 Cross-Cutting Concerns

- **OCPP Protocol**: Comprehensive testing via OCPP.Core simulator (see ocpp-test-scenarios.md)
- **Database**: PostgreSQL with test migrations
- **Caching**: Redis mocking for read replicas
- **Real-time**: SignalR hub testing
- **Localization**: VI and EN strings validation
- **Error Handling**: Standard error codes (MOD_XXX_YYY format)

---

## 2. Test Levels & Strategies

### 2.1 Unit Tests (xUnit)

**Target Coverage**: 80% of domain logic and app services
**Technology**: xUnit, Moq, FluentAssertions

#### Test Fixture Structure (per module)

```csharp
// Example: MOD-001 Station Management
namespace EVCharging.Tests.Modules.StationManagement
{
    public class CreateStationCommandHandlerTests
    {
        private readonly CreateStationCommandHandler _handler;
        private readonly Mock<IRepository<ChargingStation>> _repositoryMock;

        [Fact]
        public async Task Handle_WithValidData_CreatesStationSuccessfully()
        {
            // Arrange
            var command = new CreateStationCommand { StationCode = "ST001", Name = "Test Station" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _repositoryMock.Verify(r => r.InsertAsync(It.IsAny<ChargingStation>()), Times.Once);
        }
    }
}
```

#### Unit Test Categories per Module

| Module | Domain Tests | Command Handlers | Query Handlers | Validators |
|--------|---|---|---|---|
| MOD-001 | Station creation, decommissioning | 3 | 2 | 2 |
| MOD-002 | Connector status, enable/disable | 3 | 2 | 2 |
| MOD-003 | Status transitions, alert logic | 4 | 2 | 2 |
| MOD-004 | Meter validation, aggregation | 5 | 3 | 3 |
| MOD-005 | Fault detection, suppression | 4 | 2 | 2 |
| MOD-006 | OCPP message parsing | 6 | 0 | 3 |
| MOD-007 | Cost calculation, tax logic | 4 | 2 | 2 |
| MOD-008 | Payment flow, idempotency | 4 | 2 | 2 |
| MOD-009 | Vehicle CRUD, active selection | 3 | 2 | 1 |
| MOD-010 | Session state machine | 5 | 3 | 2 |
| MOD-011 | Auth, password validation | 4 | 2 | 2 |
| MOD-012 | Notification creation | 3 | 1 | 1 |
| MOD-013 | Grouping logic | 2 | 2 | 1 |
| MOD-014 | Log persistence | 2 | 2 | 1 |
| MOD-015 | E-invoice creation | 3 | 2 | 2 |

**Total Unit Tests**: ~60 test classes, ~200+ test cases

### 2.2 Integration Tests (ABP Test Infrastructure)

**Target Coverage**: 70% of application services and API layers
**Technology**: xUnit, ABP TestBase, SQLite in-memory or test PostgreSQL

#### Test Setup (ABP Module)

```csharp
namespace EVCharging.Tests.Integration
{
    public class StationManagementIntegrationTests : EVChargingApplicationTestBase
    {
        private readonly IStationAppService _stationAppService;

        public StationManagementIntegrationTests()
        {
            _stationAppService = GetRequiredService<IStationAppService>();
        }

        [Fact]
        public async Task CreateStation_ShouldTriggerAuditLog()
        {
            var input = new CreateStationInput { StationCode = "ST001", Name = "Test" };
            var result = await _stationAppService.CreateAsync(input);

            var auditLogs = await UnitOfWorkManager.Current.GetOracleRepository<AuditLog>()
                .GetListAsync(x => x.EntityId == result.Id);
            auditLogs.Should().ContainSingle();
        }
    }
}
```

#### Integration Test Scenarios per Module

| Module | API Endpoint Tests | DB Interaction | Caching | Total |
|--------|---|---|---|---|
| MOD-001 | 5 | 3 | 1 | 9 |
| MOD-002 | 6 | 3 | 1 | 10 |
| MOD-003 | 4 | 3 | 1 | 8 |
| MOD-004 | 3 | 4 | 0 | 7 |
| MOD-005 | 3 | 3 | 0 | 6 |
| MOD-006 | 4 | 4 | 2 | 10 |
| MOD-007 | 5 | 3 | 1 | 9 |
| MOD-008 | 4 | 3 | 0 | 7 |
| MOD-009 | 5 | 3 | 0 | 8 |
| MOD-010 | 6 | 4 | 2 | 12 |
| MOD-011 | 5 | 3 | 0 | 8 |
| MOD-012 | 3 | 2 | 0 | 5 |
| MOD-013 | 4 | 2 | 0 | 6 |
| MOD-014 | 3 | 3 | 0 | 6 |
| MOD-015 | 4 | 3 | 0 | 7 |

**Total Integration Tests**: ~40 test classes, ~130+ test cases

### 2.3 End-to-End Tests (Critical User Flows)

**Scope**: Happy-path and critical error scenarios for high-impact flows
**Technology**: xUnit + HttpClient, SQL Server/PostgreSQL test database

#### E2E Test Scenarios

| Flow ID | Scenario | Modules Involved | Priority |
|---------|----------|------------------|----------|
| E2E-001 | Complete charging session (QR scan to payment) | MOD-001,002,010,007,008,015 | Critical |
| E2E-002 | Station onboarding and first heartbeat | MOD-001,002,006,003 | Critical |
| E2E-003 | Fault detection and alert workflow | MOD-005,003,012 | High |
| E2E-004 | Real-time session monitoring | MOD-010,004,003,012 | High |
| E2E-005 | Connector enable/disable during session | MOD-002,010,006 | High |
| E2E-006 | User registration and vehicle management | MOD-011,009 | Medium |
| E2E-007 | Tariff change and cost recalculation | MOD-007,010 | Medium |
| E2E-008 | OCPP reconnection with state recovery | MOD-006,010,003 | Critical |
| E2E-009 | Remote start/stop transaction | MOD-006,010,002 | High |
| E2E-010 | Payment failure retry | MOD-008,010 | Medium |

**Total E2E Tests**: 10 scenarios, ~30-40 test cases

---

## 3. Test Environment Setup

### 3.1 Docker Test Infrastructure

```yaml
# docker-compose.test.yml
version: '3.8'
services:
  postgres-test:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: ev_charging_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_pass
    ports:
      - "5433:5432"

  redis-test:
    image: redis:7-alpine
    ports:
      - "6380:6379"

  ocpp-simulator:
    image: ev-charging-ocpp-sim:latest
    ports:
      - "8080:8080"
    environment:
      WS_URL: ws://localhost:5000/ocpp
```

### 3.2 Test Database & Migrations

- **Provider**: PostgreSQL 15 (production-parity)
- **Strategy**: Entity Framework Core migrations in test setup
- **Seed Data**: Minimal fixtures (5 test stations, 10 connectors, 3 users)
- **Cleanup**: Fresh database per test run or T-SQL truncate

```csharp
// Test DbContext setup
public class EVChargingTestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=ev_charging_test;Username=test_user;Password=test_pass");
    }
}
```

### 3.3 OCPP Simulator

**Purpose**: Simulate charger connections and message flows
**Technology**: .NET WebSocket server or external simulator (OCPP.Core test utilities)

```csharp
// OCPP test client
public class OcppTestClient
{
    private ClientWebSocket _socket;

    public async Task ConnectAsync(string stationCode)
    {
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(new Uri($"ws://localhost:5000/ocpp/{stationCode}"), CancellationToken.None);
    }

    public async Task SendBootNotificationAsync()
    {
        var message = new BootNotificationRequest { ChargePointModel = "TestCharger" };
        // Serialize and send via WebSocket
    }
}
```

### 3.4 Mocking & Fixtures

- **Payment Gateway Mocks**: Moq for ZaloPay, MoMo, OnePay
- **Email Service Mock**: For invoice delivery testing
- **FCM Mock**: For notification delivery
- **Redis Cache**: Testcontainers or in-memory mock

---

## 4. Test Execution Schedule & Entry/Exit Criteria

### 4.1 Phase-Based Execution

| Phase | Dates | Modules | Entry Criteria | Exit Criteria |
|-------|-------|---------|---|---|
| Prototype | Feb 28 - Mar 15 | MOD-001,002,006 | Code complete, dev tested | 80% unit pass, 0 P0 bugs |
| Station Mgmt | Mar 16 - Mar 31 | MOD-001,002,003,005,013 | Feature freeze, unit tests done | 80% unit, 70% int, 0 P0/P1 |
| Monitoring & Metering | Apr 1 - Apr 30 | MOD-003,004,005,012 | Unit tests merged | 85% coverage, critical flows pass |
| MVP Integration | May 1 - May 31 | All MOD-001..015 | All features code-complete | 80% unit, 70% int, E2E green |
| Beta & Load | Jun 1 - Jun 30 | Performance & Stress | MVP stable | k6 results acceptable |

### 4.2 Entry Criteria per Test Level

#### Unit Test Entry
- Feature code peer-reviewed
- Code builds without errors
- No compile-time warnings

#### Integration Test Entry
- All unit tests passing
- Database schema migrated
- Test data seed script verified

#### E2E Test Entry
- Both APIs running (Admin & BFF)
- PostgreSQL test instance online
- OCPP simulator ready
- Payment gateway mocks configured

#### Load Test Entry
- All E2E tests passing
- Staging environment provisioned
- k6 scripts reviewed

### 4.3 Exit Criteria per Test Level

#### Unit Tests
- Pass Rate: >= 95%
- Coverage: >= 80% (per module target)
- No P0 defects
- Code review approval

#### Integration Tests
- Pass Rate: >= 90%
- Coverage: >= 70% (per module target)
- No P0/P1 defects blocking E2E
- DB state clean between runs

#### E2E Tests
- Pass Rate: >= 95% for critical flows
- All error scenarios handled
- SignalR real-time updates verified
- No hanging or orphaned resources

#### Load Tests (Performance SLA)
- Concurrent Users: 1000+ sessions
- Response Time: p95 < 500ms for API calls
- Error Rate: < 0.1%
- OCPP message throughput: > 10,000 msg/sec

---

## 5. Test Data Management Strategy

### 5.1 Test Data Fixtures

```csharp
public class TestDataBuilder
{
    // Stations
    public static ChargingStation TestStation(string code = "TEST-ST-001")
        => new() {
            StationCode = code,
            Name = "Test Station",
            Latitude = 21.0285,
            Longitude = 105.8542,
            Address = "Hanoi, Vietnam"
        };

    // Connectors (Type2, CCS, CHAdeMO)
    public static Connector TestConnector(Guid stationId, int connectorNum = 1, ConnectorType type = ConnectorType.Type2)
        => new() {
            StationId = stationId,
            ConnectorNumber = connectorNum,
            ConnectorType = type,
            MaxPowerKw = 11,
            Status = ConnectorStatus.Available
        };

    // Users
    public static AppUser TestUser(string email = "test@example.com")
        => new() { Email = email, FullName = "Test User" };

    // Vehicles
    public static Vehicle TestVehicle(Guid userId)
        => new() { UserId = userId, Make = "Tesla", Model = "Model 3" };

    // Tariff Plans
    public static TariffPlan TestTariff(decimal ratePerKwh = 3500)
        => new() {
            Name = "Standard",
            BaseRatePerKwh = ratePerKwh,
            TaxRatePercent = 10
        };
}
```

### 5.2 Data Cleanup Strategy

- **Per-Test Isolation**: New database transaction rolled back after each test
- **Shared Test DB**: Truncate tables in fixed order (reverse FK dependency)
- **Seed Data Restoration**: Re-seed 5 base stations after cleanup

### 5.3 Test Data Privacy

- No real customer PII in test fixtures
- Hashed passwords only
- Mock payment gateway tokens (non-sensitive)
- Test email addresses: test-N@ev-charging.test

---

## 6. Defect Management Process

### 6.1 Bug Classification

| Severity | Description | Example | Fix Timeline |
|----------|-------------|---------|---|
| P0 (Critical) | System crash, data loss, payment failure | App service exception | Same day |
| P1 (High) | Feature broken, auth failure, E2E blocked | Can't start session | 2 days |
| P2 (Medium) | Partial functionality, bad UX | Slow UI response | 5 days |
| P3 (Low) | Minor UX, cosmetic | Typo in error message | Sprint |

### 6.2 Bug Reporting Template

```markdown
## Bug: [MOD-NNN] Title

**Severity**: P1
**Steps to Reproduce**:
1. Create station via API
2. Send StatusNotification
3. Expected: Status updates
4. Actual: No update

**Error Code**: MOD_001_005
**Stack Trace**: [Full trace]
**Attachment**: [Screenshot/logs]
**Assigned To**: @dev-name
**Status**: Open → In Progress → Testing → Closed
```

### 6.3 Regression Test for Bug Fixes

- Each P0/P1 bug requires a new unit or integration test
- Test cases in `/Tests/{ModuleName}/Regression_[BugId].cs`
- Must pass before bug marked as fixed

---

## 7. Performance Testing (Load Testing with k6)

### 7.1 Load Test Scenarios

#### Scenario 1: Concurrent Station Connection
```javascript
// k6 script: ocpp-concurrent-stations.js
export let options = {
    vus: 500,  // 500 concurrent chargers
    duration: '5m',
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01'],
    }
};

export default function() {
    let stationCode = `station-${__VU}`;
    ws.connect(`ws://localhost:5000/ocpp/${stationCode}`, {}, (socket) => {
        // Send BootNotification, Heartbeat, StartTransaction
        socket.sendBinary(ocppBootNotification);
    });
}
```

#### Scenario 2: Meter Values Throughput
- 1000 active sessions
- Each sends MeterValues every 10 seconds
- Expected: 100 MeterValues/sec, DB insert latency < 100ms

#### Scenario 3: API Query Load
- 100 concurrent users querying session history
- Cursor-based pagination
- Expected: p95 response < 300ms, cache hit rate > 80%

### 7.2 Performance SLA Targets

| Metric | Target | Tool |
|--------|--------|------|
| API Response Time (p95) | < 500ms | k6, AppInsights |
| OCPP Message Processing | < 100ms | OCPP logs |
| DB Query (95th percentile) | < 50ms | PostgreSQL slow logs |
| Cache Hit Rate | > 80% | Redis stats |
| Error Rate | < 0.1% | k6 |
| Concurrent OCPP Connections | > 1000 | WebSocket metrics |

---

## 8. Test Execution & Reporting

### 8.1 CI/CD Integration

```yaml
# .github/workflows/test.yml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0'
      - run: dotnet test --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"
      - uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: '**/test-results.trx'
          reporter: 'dotnet trx'
      - uses: codecov/codecov-action@v3
        with:
          files: './coverage.xml'
```

### 8.2 Test Report Dashboard

- **Weekly Summary**: Pass rate, coverage trend, P0/P1 count
- **Module Health**: Coverage %, test count per module
- **Regression Risk**: Failed tests count, new failures
- **Performance Trend**: API latency, OCPP throughput over time

### 8.3 Test Metrics to Track

```
Total Tests: [unit + integration + e2e]
├── Unit: XYZ tests, 80% coverage
├── Integration: ABC tests, 70% coverage
└── E2E: 10 scenarios

Pass Rate: 95.5%
├── Passed: ABC
├── Failed: XY (with priority tags)
└── Skipped: Z (blockers noted)

Coverage by Module:
├── MOD-001: 82%
├── MOD-002: 78%
...

Performance Baselines (k6):
├── API p95: 280ms
├── OCPP throughput: 8,500 msg/sec
└── DB query p95: 35ms
```

---

## 9. Special Test Considerations

### 9.1 OCPP Protocol Testing
See `/docs/07-testing/ocpp-test-scenarios.md` for detailed OCPP test matrix

### 9.2 Multi-Tenancy (Future)
- Not required in Phase 1
- Placeholder for organization isolation testing

### 9.3 Localization Testing
- VI strings: Correct Vietnamese characters, tone marks
- EN strings: Grammar, terminology alignment
- Date/Currency formatting: dd/MM/yyyy, VNĐ 9.900đ format

### 9.4 Security Testing
- **Scope**: Phase 2 (penetration testing)
- **Phase 1 Focus**:
  - HTTPS-only API endpoints
  - JWT token validation
  - Role-based auth checks
  - SQL injection prevention (EF Core parameterized)

### 9.5 Accessibility (Mobile App)
- Phase 2 focus
- Use React Native testing library
- Screen reader validation for critical flows

---

## 10. Test Artifacts & Documentation

### 10.1 Required Documentation

| Artifact | Owner | Location |
|----------|-------|----------|
| Test Plan (this doc) | QA Lead | docs/07-testing/test-plan.md |
| OCPP Scenarios | OCPP Specialist | docs/07-testing/ocpp-test-scenarios.md |
| Test Case Repository | QA | Excel/TestRail (link in README) |
| Performance Baseline | DevOps | docs/performance-baseline.md |
| Known Issues Register | QA Lead | docs/known-issues.md |

### 10.2 Test Code Organization

```
tests/
├── EVCharging.Tests.Unit/
│   ├── Modules/
│   │   ├── StationManagement/
│   │   ├── ConnectorManagement/
│   │   └── [other modules]
│   └── Common/
│       └── TestDataBuilder.cs
├── EVCharging.Tests.Integration/
│   ├── Modules/
│   └── EVChargingApplicationTestBase.cs
├── EVCharging.Tests.E2E/
│   ├── Flows/
│   │   ├── ChargingSessionFlow.cs
│   │   └── [other flows]
│   └── Fixtures/
└── EVCharging.Tests.Performance/
    └── k6-scripts/
```

---

## 11. Team Responsibilities

| Role | Responsibility |
|------|---|
| **Developer** | Write unit tests (min 80% coverage), follow test fixtures |
| **QA Engineer** | Integration & E2E tests, defect logging, test maintenance |
| **QA Lead** | Test plan execution, metrics tracking, release decision |
| **DevOps** | CI/CD pipeline, test env setup, k6 load testing |
| **Tech Lead** | OCPP protocol testing, architecture review |

---

## 12. Sign-Off & Approval

| Role | Sign-Off | Date |
|------|----------|------|
| QA Lead | [ ] | _____ |
| Tech Lead | [ ] | _____ |
| Project Manager | [ ] | _____ |
| Product Owner | [ ] | _____ |

---

## Appendix: Test Execution Commands

```bash
# Unit tests only
dotnet test --filter "Category=Unit" -l "trx" --logger "console;verbosity=detailed"

# Integration tests
dotnet test --filter "Category=Integration" --collect:"XPlat Code Coverage"

# E2E tests (requires running APIs)
dotnet test --filter "Category=E2E" -l "trx"

# All tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# k6 load test
k6 run tests/performance/ocpp-concurrent-stations.js --vus 500 --duration 5m

# Generate HTML coverage report
dotnet reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```
