# ADR-003: OCPP 1.6J as Charger Communication Protocol (with OCPP 2.0.1 Migration Path)

> Status: ACCEPTED | Date: 2026-03-01

## Context

EV Charging CSMS requires standardized communication protocol to:
- **Manage chargers**: Monitor status, trigger charging sessions, collect telemetry
- **Handle edge cases**: Connection drops, offline sessions, firmware updates
- **Vietnam market compatibility**: Support existing charger hardware deployed in Vietnam
- **Future scalability**: Design for protocol evolution without breaking backward compatibility
- **Real-time operations**: Sub-second messaging for critical safety commands

**OCPP (Open Charge Point Protocol) versions available:**
1. **OCPP 1.5** (SOAP/XML) — Legacy; deprecated
2. **OCPP 1.6 Classic** (SOAP/XML) — Transitional
3. **OCPP 1.6J** (JSON/WebSocket) — Modern, widely deployed
4. **OCPP 2.0.1** (JSON/WebSocket) — Latest; improved security, real-time features; limited charger support in Vietnam (2026)

## Decision

**Implement OCPP 1.6J (JSON/WebSocket) as the primary protocol, with design patterns that enable migration to OCPP 2.0.1 in Phase 2.**

### Rationale

1. **Widespread Hardware Support in Vietnam**
   - ABB, Siemens, Schneider Electric chargers sold in Vietnam support OCPP 1.6J
   - Domestic Chinese manufacturers (BYD, Geely, etc.) default to 1.6J
   - By 2026, ~90% of deployed chargers support 1.6J; <10% support 2.0.1
   - Retrofit older chargers to 1.6J is cheaper than replacing hardware

2. **JSON/WebSocket Foundation**
   - OCPP 1.6J uses WebSocket + JSON (vs. 1.5's SOAP/XML bloat)
   - Lightweight messages; 80% smaller than XML equivalents
   - Native .NET WebSocket support (`WebSocketMiddleware`)
   - JSON payload easily parsed by `System.Text.Json` (no XML deserialization overhead)

3. **Protocol Maturity**
   - OCPP 1.6J specification is stable (no breaking changes since 2015)
   - Extensive vendor implementations; bug fixes well-documented
   - Community OCPP.Core library provides reference implementation
   - Production chargers are battle-tested

4. **Backward Compatibility**
   - Many legacy chargers in Vietnam still use OCPP 1.5 or Classic
   - OCPP.Core library supports 1.5, 1.6J side-by-side
   - Can add 1.5 support later without rewriting 1.6J handlers

5. **Security Considerations**
   - OCPP 1.6J supports TLS/WSS (WebSocket Secure)
   - Certificate pinning possible for charger authentication
   - Sufficient for Vietnam market threat model (Phase 1)
   - OCPP 2.0.1 adds mutual TLS, but 1.6J + WSS is adequate

6. **CSMS Implementation Simplicity**
   - OCPP.Core provides state machines for typical workflows (AuthStart → SessionStart → SessionStop)
   - Fewer message types than 2.0.1; simpler handler pipeline
   - Error recovery well-documented in 1.6 spec
   - Testing utilities available (mock chargers, protocol validators)

7. **Extensibility for Vietnam-Specific Features**
   - OCPP 1.6J supports vendor-specific extensions
   - Can add Vietnamese regulatory features (energy tracking, billing intervals) without breaking spec
   - Easier to negotiate with domestic charger manufacturers

8. **Phase 2 Migration Path Designed In**
   - Architecture separates OCPP protocol layer from domain logic
   - OCPP handler implementations inherit from `OcppMessageHandlerBase`
   - Message translation layer (`IOcppMessageAdapter`) converts 1.6J ↔ 2.0.1
   - Can run both protocols parallel during migration window

## Consequences

### Positive

- ✅ **Hardware Compatible**: 90% of Vietnam chargers support 1.6J; no costly hardware replacement needed
- ✅ **Lightweight Protocol**: JSON/WebSocket is 80% smaller than XML; lower bandwidth for remote chargers
- ✅ **Proven Stability**: Spec unchanged since 2015; vendor implementations well-tested
- ✅ **Fast Implementation**: OCPP.Core library provides handlers, state machines, serialization
- ✅ **Community Support**: Active GitHub repos (LovonLovon/OCPP, ChargeITS/OCPP), Vietnamese integrators know 1.6J
- ✅ **Easy Testing**: Mock OCPP chargers available; protocol simple enough to test manually
- ✅ **Security Sufficient**: TLS/WSS + certificate pinning adequate for Vietnam market (Phase 1)
- ✅ **Extensible**: Vendor-specific extensions support Vietnamese regulatory requirements
- ✅ **Cost-Effective**: No licensing; open specification; no royalties

### Negative

- ❌ **Feature Parity Lag**: OCPP 2.0.1 has improved real-time scheduling, but 1.6J lacks it
- ❌ **Limited Security**: No mutual TLS; certificate validation weaker than 2.0.1
- ❌ **Deprecation Path**: OCPP 1.6J will eventually be deprecated; mandatory upgrade in 5-10 years
- ❌ **Charger Firmware Bugs**: Older 1.6J implementations have quirks; requires workarounds
- ❌ **Legacy Feature Overhead**: OCPP.Core supports 1.5 compatibility; extra code to maintain
- ❌ **No Smart Grid Integration**: OCPP 2.0.1 supports ISO 15118 (V2G); 1.6J doesn't
- ❌ **Vendor Lock-In Risk**: Some charger manufacturers slow to update 1.6J drivers; stuck on old firmware

### Risks

| Risk | Mitigation |
|------|-----------|
| **Charger firmware bugs in OCPP 1.6J** | Create workaround matrix in code comments; log vendor + firmware version; escalate to manufacturer |
| **Security vulnerabilities in older chargers** | Use WSS (WebSocket Secure) + certificate pinning; implement charger authentication via MACAddress + shared secret |
| **OCPP 2.0.1 chargers arriving in 2027 require dual protocol** | Design adapter pattern now; build 2.0.1 support in parallel (Phase 2); run both protocols 6 months overlap |
| **Charger offline causes session orphans** | Implement heartbeat + session timeout rules; auto-complete sessions after 24h offline; manual recovery UI for operators |
| **WebSocket connection drops under high load** | Implement reconnection with exponential backoff; queue commands during disconnection; replay after reconnect |

## Migration Path to OCPP 2.0.1 (Phase 2)

**Design assumes future transition:**

```csharp
// Phase 1 (2026-03-01): OCPP 1.6J only
public interface IOcppMessageHandler {
    Task Handle(OcppMessage message);
}

// Phase 2 (2027+): Adapter layer supports both protocols
public interface IOcppMessageAdapter {
    OcppMessage Adapt(IOcppProtocolMessage protocolMessage); // 1.6J or 2.0.1 → unified model
    IOcppProtocolMessage Adapt(OcppMessage domainMessage); // unified model → 1.6J or 2.0.1
}

// Gradual migration: New chargers onboard via 2.0.1; old chargers stay on 1.6J
```

**Migration timeline (illustrative):**
- **2026-Q1**: OCPP 1.6J only (this decision)
- **2026-Q4**: Test OCPP 2.0.1 beta implementations with early-adopter chargers
- **2027-Q2**: Dual-protocol support; new chargers use 2.0.1
- **2028-Q4**: Deprecate OCPP 1.6J; migrate remaining chargers to 2.0.1

## Alternatives Considered

### 1. **OCPP 2.0.1 (Latest Version)**
**Pros:**
- Newer security features (mutual TLS, challenge-response)
- Better real-time scheduling (ISO 15118 V2G support)
- Improved error handling and status reporting
- Charger manufacturers phasing in support

**Cons:**
- **Only 5% of Vietnam chargers support 2.0.1 as of 2026**
- Specification is larger; more complex to implement correctly
- OCPP.Core library still adding 2.0.1 support (incomplete)
- Charger firmware updates slow in Vietnam; manufacturers lag
- Would require upgrading 90% of existing chargers → cost: 500M₫+ for 1000 chargers
- Risk of incompatibility with older charger implementations

**Verdict:** Right for Phase 2 (2027+) when hardware ecosystem matures; premature for Phase 1.

---

### 2. **Proprietary Protocol (Custom JSON/WebSocket)**
**Pros:**
- Maximum control; optimize for CSMS use cases
- No dependency on external spec changes

**Cons:**
- **Cannot interoperate with non-compliant chargers**
- Charger manufacturers refuse to implement custom protocol
- Loses years of OCPP standardization work
- Reinventing the wheel; OCPP already solved these problems
- No community support for debugging

**Verdict:** Not viable; chargers expect OCPP.

---

### 3. **OCPP 1.6 Classic (SOAP/XML)**
**Pros:**
- Wider legacy charger support
- Some older Chinese manufacturers still support XML

**Cons:**
- **SOAP is bloated**: 3-5x message size vs. JSON
- Slow to parse; higher CPU on chargers
- Deprecated by OCPP Org; no new features
- XML deserialization in .NET slower than JSON
- OCPP.Core community moving to JSON; less maintenance

**Verdict:** Legacy only; JSON is clearly superior.

---

### 4. **IEC 61851 (Hardware Standard, Not Protocol)**
**Pros:**
- Direct hardware integration; no network needed
- Works for Level 1 chargers

**Cons:**
- **Not a communication protocol**; just electrical standard
- Cannot remotely manage chargers
- No telemetry, session history, pricing
- Incompatible with CSMS goals

**Verdict:** Orthogonal to OCPP; both may be used but IEC 61851 doesn't replace OCPP.

---

### 5. **ISO 15118 (V2G Communication)**
**Pros:**
- Bidirectional charging, grid services
- Future EV-to-Grid (V2G) support

**Cons:**
- **Not a charger management protocol**; vehicle-to-charger communication only
- OCPP still required for charger-to-CSMS
- Battery swapping rare in Vietnam; not a priority
- Infrastructure (ISO 15118 hardware) not mature

**Verdict:** Complementary to OCPP; not a replacement. Possible Phase 3 addition.

---

## Implementation Architecture

**OCPP Handler Structure (1.6J):**

```
OcppWebSocketMiddleware
    ↓
OcppMessageRouter (routes by message type)
    ├─ HeartbeatHandler
    ├─ AuthorizeHandler
    ├─ StartTransactionHandler
    ├─ StopTransactionHandler
    ├─ MeterValuesHandler
    └─ StatusNotificationHandler
        ↓
Domain Layer (Charging Sessions, Transactions, Audit)
```

**Protocol Abstraction (for Phase 2 migration):**

```
IOcppHandler (interface)
    ├─ OcppV16JHandler (implements for 1.6J)
    └─ OcppV2_0_1Handler (phase 2)

IOcppMessageFactory
    ├─ OcppV16JFactory
    └─ OcppV2_0_1Factory

IOcppMessageAdapter (translates between protocol versions)
```

## Related Decisions

- **ADR-001**: ABP Framework provides hosting for OCPP WebSocket handlers
- **ADR-002**: PostgreSQL stores OCPP messages as JSONB for audit trail
- **ADR-005**: CQRS separates charger commands (write) from telemetry reads (read replicas)

## References

- [OCPP 1.6J Specification (Official)](https://www.openchargealliance.org/protocols/ocpp-16/)
- [OCPP 2.0.1 Specification (Future)](https://www.openchargealliance.org/protocols/ocpp-v201/)
- [OCPP.Core Library (GitHub)](https://github.com/LovonLovon/OCPP)
- [OCPP Errata & Clarifications](https://www.openchargealliance.org/protocols/ocpp-errata/)
- [Siemens OCPP Implementation Guide](https://new.siemens.com/global/en/products/energy/power-distribution/medium-voltage/charging-infrastructure.html)
- [ABB OCPP Charger Specs (Vietnam Market)](https://abb.com/ev-charging)
