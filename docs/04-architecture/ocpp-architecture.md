# OCPP WebSocket Architecture

> Status: APPROVED | Last Updated: 2026-03-06

---

## 1. Overview

OCPP 1.6J (JSON over WebSocket) is the communication standard between charging stations and CSMS. Each charger maintains a persistent WebSocket connection for full-duplex, real-time communication.

## 2. Connection Model

```
Charger → WebSocket → ws://csms-host/ocpp/{chargePointId} → CSMS Handler
```

Each charger connects with its unique ID in the URL path. The CSMS maintains a registry of active connections for sending commands back to chargers.

## 3. Message Flows

### Charge Point → CSMS (charger-initiated)
| Message | Purpose | When |
|---------|---------|------|
| BootNotification | Register charger on startup | Charger boots/restarts |
| Heartbeat | Periodic alive signal | Every N seconds (configured) |
| StatusNotification | Connector status change | Status changes (Available → Charging, etc.) |
| Authorize | Validate user/RFID tag | Before charging starts |
| StartTransaction | Begin charging session | User initiates charge |
| StopTransaction | End charging session | Charging stops (user/system/error) |
| MeterValues | Energy readings | During charging (periodic interval) |
| DiagnosticsStatusNotification | Diagnostics upload status | After diagnostics request |
| FirmwareStatusNotification | Firmware update status | During firmware update |

### CSMS → Charge Point (server-initiated)
| Message | Purpose | When |
|---------|---------|------|
| RemoteStartTransaction | Start charging remotely | User starts from app |
| RemoteStopTransaction | Stop charging remotely | User stops from app |
| Reset | Hard/soft reset charger | Admin command |
| ChangeConfiguration | Update charger settings | Configuration change |
| GetConfiguration | Read charger settings | Status check |
| UnlockConnector | Remote unlock | Operator recovery |
| UpdateFirmware | Trigger firmware update | Admin command |
| SetChargingProfile | Smart charging profiles | Load management |
| ClearChargingProfile | Remove profiles | Reset load config |
| TriggerMessage | Request specific message | Diagnostic request |

## 4. Implementation Architecture

### Key Components

| Component | Location | Lifecycle | Purpose |
|-----------|----------|-----------|---------|
| `OcppWebSocketMiddleware` | HttpApi.Host/Ocpp | Singleton middleware | Accept WS, validate cpId, manage message loop |
| `OcppConnectionManager` | HttpApi.Host/Ocpp | Singleton | Track active WS connections |
| `OcppConnection` | HttpApi.Host/Ocpp | Per-connection | WS wrapper + correlation (SendCallAsync) |
| `OcppMessageHandler` | HttpApi.Host/Ocpp | Scoped (per message) | Route/dispatch OCPP messages |
| `OcppService` | Domain/Ocpp | Scoped | Domain logic (persist sessions, meters) |
| `OcppRemoteCommandService` | HttpApi.Host/Ocpp | Scoped | Bridge: admin commands → ConnectionManager |
| `HeartbeatMonitorService` | HttpApi.Host/Ocpp | Hosted | Background: detect stale connections (6min timeout) |
| `VendorProfileFactory` | Domain/Ocpp/Vendors | Singleton | Detect/resolve vendor profiles |

### Connection Flow

```
1. Charger → WS connect /ocpp/{cpId}
2. Middleware validates cpId regex [A-Za-z0-9-_.]{1,64}
3. Accept with subprotocol "ocpp1.6"
4. Register in ConnectionManager
5. Message loop: receive → scope → UoW → MessageHandler → respond
6. On disconnect: cleanup orphaned sessions, remove from ConnectionManager
```

### Idempotency

- `StartTransaction`: Deduplicated by `OcppTransactionId` (returns existing session)
- `StopTransaction`: Ignores duplicate if session already Completed
- `MeterValues`: Monotonic validation (rejects backward/outlier readings)

### cpId Validation

Format: `[A-Za-z0-9-_.]{1,64}` — validated at middleware layer before WS accept.

## 5. Vendor Profile System

### Problem

Different charger vendors deviate from OCPP 1.6J in subtle but impactful ways:
- Energy units: Some report Wh, others kWh, some omit the unit field
- Timestamps: Non-standard formats (space separator, missing timezone)
- Missing fields: `transactionId` may be omitted in MeterValues
- Duplicate messages: Some vendors retry StartTransaction on reconnection

### Solution: Strategy Pattern

```
IVendorProfile (interface)
├── VendorProfileBase (abstract, standard OCPP behavior)
│   ├── GenericProfile (default fallback)
│   ├── ChargecoreGlobalProfile (AU/APAC vendor)
│   └── JuhangProfile (Chinese vendor, common in Vietnam)
```

`VendorProfileFactory` auto-detects the profile from `chargePointVendor`/`chargePointModel` strings during BootNotification.

### Supported Vendors

| Vendor | Profile | Energy Default | Key Quirks |
|--------|---------|---------------|------------|
| Generic | `VendorProfileType.Generic` | Wh (inferred) | Standard OCPP 1.6J |
| Chargecore Global | `VendorProfileType.ChargecoreGlobal` | **kWh** (even when unit missing) | May retry StartTransaction |
| JUHANG | `VendorProfileType.Juhang` | Wh (small values → kWh) | Omits transactionId, space-separated timestamps |

### Adding a New Vendor Profile

1. Create class in `KLC.Domain/Ocpp/Vendors/` extending `VendorProfileBase`
2. Add value to `VendorProfileType` enum (`KLC.Domain.Shared/Enums/`)
3. Override `MatchesVendor()` with vendor string detection rules
4. Override `NormalizeEnergyToWh()`, `NormalizePowerToW()`, `ParseTimestamp()` as needed
5. Register in `ConfigureOcppServices()` in `KLCHttpApiHostModule.cs`
6. Create EF migration if adding a new enum value (not needed for code-only changes)

### Energy Unit Inference (when unit field is missing)

| Value Range | Inferred Unit | Rationale |
|-------------|--------------|-----------|
| > 100 | Wh | Typical session = 5000-50000 Wh |
| ≤ 100, > 0 | kWh (×1000 → Wh) | Typical session = 5-50 kWh |

Vendor profiles can override this with vendor-specific logic (e.g., Chargecore always assumes kWh).

## 6. OCPP Raw Event Storage

All incoming OCPP Call messages are persisted to `OcppRawEvents` table:
- JSONB payload column for vendor-agnostic storage
- Indexed by `(ChargePointId, ReceivedAt)` for efficient querying
- Includes processing latency (ms) and active vendor profile
- Used for audit, debugging, and replay

## 7. Admin OCPP Management API

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/ocpp/connections` | GET | List all connected chargers |
| `/api/v1/ocpp/connections/{cpId}` | GET | Get charger detail (vendor, model, status) |
| `/api/v1/ocpp/connections/{cpId}/remote-start` | POST | Send RemoteStartTransaction |
| `/api/v1/ocpp/connections/{cpId}/remote-stop` | POST | Send RemoteStopTransaction |
| `/api/v1/ocpp/events` | GET | Query raw OCPP event log |

## 8. Implementation Requirements

- Separate OCPP WebSocket handler service
- Strongly-typed message serialization/deserialization
- Idempotent message handling (chargers may retry)
- Immediate transaction data persistence for billing accuracy
- Reconnection handling with session state recovery
- MeterValues measurands: Energy.Active.Import.Register, Current.Import, Voltage, Power.Active.Import, SoC
- Vendor-aware energy/power normalization via VendorProfileFactory
- Structured logging: cpId, vendorProfile, action, uniqueId, latencyMs

## 9. Phase 2 — Power Management

Phase 2 introduces power sharing and dynamic load balancing via OCPP's `SetChargingProfile` command. These features allow the CSMS to control how much power each connector draws, enabling sites to operate within grid limits without expensive electrical upgrades.

### SetChargingProfile for Power Sharing

`SetChargingProfile` sends a `ChargingProfile` to a charger with a `ChargingSchedule` specifying power limits (in kW or A) over time periods. The CSMS uses this to enforce real-time power caps on individual connectors.

Key OCPP fields used:
- `chargingProfileId`: Unique ID per profile (CSMS-generated)
- `stackLevel`: Priority layering (site load = 0, power sharing = 1, user override = 2)
- `chargingProfilePurpose`: `TxProfile` (per-transaction) or `ChargePointMaxProfile` (station-wide)
- `chargingSchedule.chargingRateUnit`: `W` (watts) or `A` (amps)
- `chargingSchedule.chargingSchedulePeriod`: Array of `{ startPeriod, limit }` entries

### Power Sharing — Rebalancing Flow

When multiple connectors share a limited power budget (PowerSharingGroup), the CSMS rebalances on every session lifecycle event:

```
1. Session starts on Connector C in PowerSharingGroup G
2. PowerSharingDomainService.Rebalance(G):
   a. Load all active sessions in group G
   b. Get total available power: G.MaxTotalPowerKw
   c. Apply strategy:
      - EqualSplit: Divide equally among active connectors
      - PriorityBased: Allocate by member Priority, respecting MinPowerKw floors
      - FirstComeFirstServed: Earlier sessions get higher allocation
   d. Enforce MinPowerKw floor and MaxPowerKw ceiling per member
3. For each affected connector, send SetChargingProfile:
   - stackLevel: 1 (power sharing layer)
   - chargingProfilePurpose: TxProfile
   - limit: calculated kW for this connector
4. On session stop → rebalance again (remaining sessions get more power)
```

### Dynamic Load Balancing — Smart Meter Flow

SiteLoadProfile enables the CSMS to respond to real-time grid readings, ensuring total EV load never exceeds the site's electrical capacity:

```
1. SiteLoadBalancingService (hosted background service) polls SmartMeterEndpoint
   - Interval: configurable (default 15 seconds)
   - Reads: total site consumption (kW) from smart meter
2. Calculate available EV capacity:
   availableKw = GridCapacityKw - ReservedForOtherLoadsKw - nonEvLoadKw
3. Distribute availableKw across active connectors using DistributionStrategy:
   - ProRata: Proportional to each connector's max power rating
   - EqualShare: Divide equally
   - PriorityFirst: Fleet/premium users get priority allocation
4. Send SetChargingProfile to each active connector:
   - stackLevel: 0 (site load layer — lowest priority, overridden by power sharing)
   - chargingProfilePurpose: ChargePointMaxProfile
   - limit: calculated kW
5. If smart meter is unreachable → fallback to static allocation (GridCapacityKw - ReservedForOtherLoadsKw)
```

### Interaction Between Power Sharing and Load Balancing

When both are active at a site, OCPP's stack levels ensure correct layering:
- Stack level 0 (site load): Sets the absolute ceiling per charger based on grid capacity
- Stack level 1 (power sharing): Further constrains within the group budget
- The charger applies the **minimum** of all active profile limits at each stack level

### OCPP 2.0.1 Upgrade Roadmap (Phase 3)

Phase 3 targets OCPP 2.0.1 adoption for enhanced smart charging:
- **Device Model**: Richer charger capability reporting (replaces GetConfiguration)
- **Smart Charging**: `NotifyChargingLimit`, `ClearedChargingLimit` for bidirectional communication of power constraints
- **ISO 15118**: Plug & Charge support (certificate-based auth, no RFID needed)
- **Transaction Events**: Replaces Start/StopTransaction with granular `TransactionEvent` messages
- **Cost Updates**: Real-time cost display on charger screen during session
- Migration path: Dual-protocol support (1.6J + 2.0.1) via subprotocol negotiation at WebSocket handshake
