# Backend Architecture Improvement Plan

## Context

After the April 8 real-charger testing sprint, several architectural debt items surfaced: inconsistent error handling, scattered configuration, monolithic OCPP handler, and duplicated code. This plan prioritizes improvements that reduce maintenance effort and prevent recurring bugs.

---

## Priority 1: Global Exception Handler for BFF (Critical)

**Problem:** Only 3 out of 14+ BFF endpoints catch `BusinessException`. Others return 500 with stack traces.

**Solution:** Add a global exception middleware for the BFF Minimal API.

**File to create:** `src/backend/src/KLC.Driver.BFF/Middleware/GlobalExceptionMiddleware.cs`

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (BusinessException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = ex.Code ?? "BAD_REQUEST", message = ex.Message }
            });
        }
        catch (AbpAuthorizationException)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "FORBIDDEN", message = "Access denied" }
            });
        }
        catch (EntityNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "NOT_FOUND", message = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "INTERNAL_ERROR", message = "An error occurred" }
            });
        }
    }
}
```

**File to modify:** `src/backend/src/KLC.Driver.BFF/Program.cs` — add `app.UseMiddleware<GlobalExceptionMiddleware>()`

**Then remove:** All individual try/catch blocks in AuthEndpoints.cs (3 blocks)

**Effort:** 1 day | **Impact:** Eliminates all 500 errors from unhandled BusinessExceptions

---

## Priority 2: Typed Configuration (Options Pattern)

**Problem:** 43+ magic string config accesses (`_configuration["Payment:VnPay:TmnCode"]`) scattered across services. Typos cause silent runtime failures.

**Solution:** Create typed settings classes with `IOptions<T>`.

**Files to create:**
```
src/backend/src/KLC.Domain.Shared/Configuration/
  ├── JwtSettings.cs
  ├── PaymentSettings.cs      (VnPay + MoMo sub-classes)
  ├── OcppSettings.cs
  └── WalletSettings.cs
```

**Example:**
```csharp
public class JwtSettings
{
    public const string Section = "Jwt";
    public string SecretKey { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
    public string Issuer { get; set; } = "KLC";
    public string Audience { get; set; } = "KLC";
}

public class VnPaySettings
{
    public const string Section = "Payment:VnPay";
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string QueryApiUrl { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string IpnUrl { get; set; } = "";
    public string Version { get; set; } = "2.1.0";
}
```

**Registration:**
```csharp
services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));
services.Configure<VnPaySettings>(configuration.GetSection(VnPaySettings.Section));
```

**Usage in services:**
```csharp
// Before:
var tmnCode = _configuration["Payment:VnPay:TmnCode"];

// After:
private readonly VnPaySettings _vnPaySettings;
public MyService(IOptions<VnPaySettings> vnPayOptions) => _vnPaySettings = vnPayOptions.Value;
var tmnCode = _vnPaySettings.TmnCode;
```

**Effort:** 2 days | **Impact:** Compile-time safety, no more magic strings, easy to find all usages

---

## Priority 3: Crypto Utility Service

**Problem:** `ComputeHmacSha256()`, `ComputeHmacSha512()`, `ConstantTimeEquals()` are copy-pasted between VnPay and MoMo services.

**Solution:** Extract to shared utility.

**File to create:** `src/backend/src/KLC.Domain/Security/CryptoService.cs`

```csharp
public static class CryptoService
{
    public static string HmacSha256(string key, string data) { ... }
    public static string HmacSha512(string key, string data) { ... }
    public static bool ConstantTimeEquals(string a, string b) { ... }
}
```

**Files to modify:**
- `src/backend/src/KLC.Application/Payments/VnPayPaymentService.cs` — replace private methods
- `src/backend/src/KLC.Application/Payments/MoMoPaymentService.cs` — replace private methods

**Effort:** 0.5 day | **Impact:** Single source of truth for crypto, reduces 40 lines of duplication

---

## Priority 4: Split OcppMessageHandler (Strategy Pattern)

**Problem:** `OcppMessageHandler.cs` is 925 lines with 15 methods, handling parsing, business logic, persistence, and notifications.

**Solution:** Split into per-action handlers using Strategy pattern.

**Files to create:**
```
src/backend/src/KLC.HttpApi.Host/Ocpp/Handlers/
  ├── IOcppActionHandler.cs           (interface)
  ├── BootNotificationHandler.cs      (90 lines)
  ├── StartTransactionHandler.cs      (140 lines)
  ├── StopTransactionHandler.cs       (130 lines)
  ├── StatusNotificationHandler.cs    (100 lines)
  ├── MeterValuesHandler.cs           (90 lines)
  ├── HeartbeatHandler.cs             (30 lines)
  └── AuthorizeHandler.cs             (30 lines)
```

**Interface:**
```csharp
public interface IOcppActionHandler
{
    string Action { get; }  // "BootNotification", "StartTransaction", etc.
    Task<string> HandleAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser);
}
```

**OcppMessageHandler becomes coordinator (~200 lines):**
```csharp
public class OcppMessageHandler
{
    private readonly Dictionary<string, IOcppActionHandler> _handlers;

    public OcppMessageHandler(IEnumerable<IOcppActionHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Action);
    }

    private async Task<string> HandleCallAsync(OcppConnection connection, ParsedOcppMessage parsed, IOcppMessageParser parser)
    {
        if (_handlers.TryGetValue(parsed.Action, out var handler))
            return await handler.HandleAsync(connection, parsed.UniqueId, parsed.Payload, parser);

        return parser.SerializeCallError(parsed.UniqueId, OcppErrorCode.NotImplemented, $"Action {parsed.Action} not implemented");
    }
}
```

**Auto-discovery in DI:**
```csharp
var handlerTypes = typeof(IOcppActionHandler).Assembly.GetTypes()
    .Where(t => !t.IsAbstract && typeof(IOcppActionHandler).IsAssignableFrom(t));
foreach (var type in handlerTypes)
    services.AddScoped(typeof(IOcppActionHandler), type);
```

**Effort:** 2-3 days | **Impact:** Each handler is testable independently, easy to add new OCPP actions

---

## Priority 5: BFF Repository Layer

**Problem:** BFF services use raw `_dbContext` (38+ queries per service), making them hard to test and tightly coupled to EF Core.

**Solution:** Introduce thin repository interfaces for BFF-specific queries.

**Files to create:**
```
src/backend/src/KLC.Driver.BFF/Repositories/
  ├── ISessionQueryRepository.cs
  ├── IStationQueryRepository.cs
  ├── IWalletQueryRepository.cs
  └── EfCore/
      ├── SessionQueryRepository.cs
      ├── StationQueryRepository.cs
      └── WalletQueryRepository.cs
```

**Example:**
```csharp
public interface ISessionQueryRepository
{
    Task<ChargingSession?> GetActiveSessionAsync(Guid userId);
    Task<Connector?> GetConnectorWithStationAsync(Guid connectorId);
    Task<PagedResult<SessionHistoryDto>> GetHistoryAsync(Guid userId, Guid? cursor, int pageSize);
}
```

**Note:** These are READ-ONLY query repositories (CQRS read side). Write operations stay in domain services.

**Effort:** 3-4 days | **Impact:** Testable services, CQRS alignment, can swap to read replicas

---

## Priority 6: Standardize Logging

**Problem:** Mixed logging approaches — ILogger with various prefixes, IAuditEventLogger underutilized, no consistent format.

**Solution:** Define logging conventions.

**Convention:**
```
[Component] Action: Details
Examples:
  [OCPP] StatusNotification from {ChargePointId}: {Status}
  [BFF:Auth] Login: phone={Phone}, result={Result}
  [BFF:Session] StartSession: userId={UserId}, connector={ConnectorId}
  [Payment:VnPay] CreateTopUp: ref={Ref}, amount={Amount}
  [AUDIT] PasswordReset: userId={UserId}, method=Firebase
```

**IAuditEventLogger usage:** ALL security-sensitive operations:
- Login success/failure
- Password change/reset
- Token refresh
- Account suspension
- Payment operations

**Effort:** 1-2 days | **Impact:** Consistent log parsing, better debugging, security audit trail

---

## Implementation Roadmap

| Phase | Tasks | Effort | When |
|-------|-------|--------|------|
| **Phase 1** (Pre go-live) | Global exception handler | 1 day | Before April 15 |
| **Phase 2** (Post go-live) | Typed configuration | 2 days | Week of April 21 |
| **Phase 2** | Crypto utility service | 0.5 day | Week of April 21 |
| **Phase 3** (Sprint 2) | Split OCPP handler | 2-3 days | May |
| **Phase 3** | BFF repository layer | 3-4 days | May |
| **Phase 3** | Standardize logging | 1-2 days | May |

**Total effort:** ~12 days across 3 phases

---

## Design Patterns Applied

| Pattern | Where | Benefit |
|---------|-------|---------|
| **Strategy** | OCPP action handlers | Add new actions without touching coordinator |
| **Options** | Typed configuration | Compile-time safety, validation |
| **Repository** | BFF query layer | Testability, CQRS alignment |
| **Middleware** | Global exception handler | DRY error handling |
| **Factory** | Vendor profiles (already done) | Add vendors with 1 file |
