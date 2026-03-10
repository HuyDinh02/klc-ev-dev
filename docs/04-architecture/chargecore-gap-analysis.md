# Chargecore Global — Gap Analysis & Improvement Plan

**Date:** 2026-03-11
**Source:** "20260118 Product Proposal of Chargecore Global.pdf"
**Purpose:** Compare Chargecore's capabilities against KLC CSMS, identify gaps, and plan improvements.

---

## 1. Executive Summary

Chargecore Global is a hardware manufacturer (Australian/APAC) offering chargers + backend software. Their proposal covers hardware (AC 7-22kW, Kern DC 20-40kW, Core DC 60-240kW), advanced features (LINK & LOOP power sharing, Dynamic Load Balancing), and a backend platform (SaaS/Cloud-to-cloud/Private deployment).

**KLC CSMS is already stronger in several areas** (payment integration, mobile app, admin portal, audit logging, TOU pricing). However, Chargecore exposes **6 significant capability gaps** that KLC should address for competitive parity and to support Chargecore hardware deployment.

---

## 2. Feature-by-Feature Comparison

### 2.1 Hardware Support

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| AC Series (7-22kW, Type2) | ✅ ConnectorType.Type2 supported | None |
| Kern DC (20-40kW, CCS1/CCS2) | ✅ CCS2 + Type1 supported | CCS1 not explicitly in enum |
| Core DC (60-240kW, CCS1/CCS2) | ✅ Supported via OCPP | None |
| Smart AC Socket (eBike) | ⚠️ No eBike-specific handling | Low priority |
| NACS (Tesla) connector | ❌ Not in ConnectorType enum | **GAP-HW-01** |
| IP55/IP65 ratings, certifications | N/A (charger property, not CSMS) | None |
| MID metering (billing-grade) | ⚠️ No metering accuracy tracking | **GAP-HW-02** |

### 2.2 OCPP Protocol

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| OCPP 1.6J | ✅ Full implementation (10 CP→CSMS, 12 CSMS→CP) | None |
| OCPP 2.0.1 upgrade path | ❌ Enum exists but not implemented | **GAP-OCPP-01** |
| ISO 15118 (Plug and Charge) | ❌ Not implemented | **GAP-OCPP-02** (Phase 3) |
| Chargecore vendor profile | ✅ Implemented in VendorProfileFactory | None |

### 2.3 Advanced Charging Features

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| **LINK & LOOP** (power sharing across up to 10 chargers) | ❌ Not implemented | **GAP-ADV-01** |
| **Dynamic Load Balancing** (Average/Proportional/Dynamic) | ❌ Not implemented | **GAP-ADV-02** |
| SetChargingProfile (smart charging) | ✅ CSMS→CP command exists | Partial — no scheduling engine |
| ClearChargingProfile | ✅ CSMS→CP command exists | None |
| Hot-swappable power modules | N/A (hardware feature) | None |
| Modular design (independent operation on module failure) | N/A (hardware feature) | None |

### 2.4 Platform & Deployment

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| SaaS deployment | ✅ Cloud Run (single-tenant) | Multi-tenant SaaS not supported |
| Cloud-to-cloud (API integration) | ⚠️ Admin API exists but no external operator API | **GAP-PLT-01** |
| Privatization (on-prem) | ⚠️ Docker images exist, no on-prem guide | Low priority |
| White label service | ❌ No white-label configuration | **GAP-PLT-02** (Phase 3) |
| Customization service | N/A (consulting) | None |

### 2.5 Payment & Operations

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| Gateway payment integration | ✅ MoMo + VnPay + ZaloPay + QR + Wallet | **KLC ahead** |
| POS machine integration | ❌ Not implemented | **GAP-PAY-01** |
| Local payment customization (MoMo, NganLuong, AlePay) | ✅ MoMo + VnPay implemented | Partial — NganLuong/AlePay missing |
| RFID card management | ✅ UserIdTag, SendLocalList | None |
| Transaction history & alarms | ✅ Full admin portal + notifications | None |
| Station map | ✅ Admin portal + mobile app with PostGIS | None |
| Operational data statistics | ✅ Dashboard with real-time SignalR | **KLC ahead** |

### 2.6 Mobile App

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| Station search & navigation | ✅ Nearby stations with PostGIS | None |
| Multiple charging ways | ✅ QR code, app-initiated, RFID | None |
| Multiple payment options | ✅ Wallet, MoMo, VnPay, QR | None |
| Real-time device status | ✅ SignalR real-time updates | None |
| Price display | ✅ Tariff display in station detail | None |

### 2.7 Fleet Management (Chargecore Dual Platform)

| Chargecore Feature | KLC Status | Gap |
|-------------------|------------|-----|
| Fleet management dashboard | ❌ Not implemented | **GAP-FLEET-01** |
| Vehicle monitoring & telematics | ❌ Only basic Vehicle entity (make/model/battery) | **GAP-FLEET-02** |
| Fleet charging optimization | ❌ Not implemented | Phase 3 |
| Driver assignment & scheduling | ❌ Not implemented | Phase 3 |
| Local whitelist per fleet | ⚠️ SendLocalList exists, no fleet grouping | Partial |

---

## 3. Gap Priority Matrix

| Gap ID | Feature | Business Impact | Effort | Phase | Priority |
|--------|---------|----------------|--------|-------|----------|
| **GAP-ADV-01** | LINK & LOOP Power Sharing | High — Chargecore hardware differentiator | Large (2-3 weeks) | Phase 2 | **P1** |
| **GAP-ADV-02** | Dynamic Load Balancing | High — Grid optimization, cost savings | Large (2-3 weeks) | Phase 2 | **P1** |
| **GAP-HW-01** | NACS connector type | Medium — Tesla market | Small (1 day) | Phase 1 | **P2** |
| **GAP-HW-02** | Metering accuracy class tracking | Low — Billing compliance | Small (1 day) | Phase 2 | **P3** |
| **GAP-OCPP-01** | OCPP 2.0.1 support | High — Future-proofing | Very Large (4-6 weeks) | Phase 3 | **P2** |
| **GAP-OCPP-02** | ISO 15118 Plug and Charge | Medium — Premium UX | Very Large (4-6 weeks) | Phase 3 | **P3** |
| **GAP-PLT-01** | Operator API (cloud-to-cloud) | Medium — B2B integration | Medium (1-2 weeks) | Phase 2 | **P2** |
| **GAP-PLT-02** | White label configuration | Low — B2B feature | Medium (1-2 weeks) | Phase 3 | **P3** |
| **GAP-PAY-01** | POS terminal integration | Low — On-site payment | Medium (1-2 weeks) | Phase 2 | **P3** |
| **GAP-FLEET-01** | Fleet management dashboard | Medium — B2B fleet operators | Large (3-4 weeks) | Phase 2 | **P2** |
| **GAP-FLEET-02** | Vehicle telematics integration | Low — Advanced fleet ops | Large (3-4 weeks) | Phase 3 | **P3** |

---

## 4. Detailed Implementation Plan

### Phase 1 — Quick Wins (MVP, before June 1, 2026)

#### GAP-HW-01: Add NACS Connector Type
**Effort:** 1 day | **Impact:** Tesla vehicle support

**Changes:**
1. Add `NACS = 5` to `ConnectorType` enum in `KLC.Domain.Shared/Enums/ConnectorType.cs`
2. Add localization keys for Vietnamese/English display name
3. Update admin portal connector type dropdown
4. Update mobile app station detail screen

**Files:**
- `src/backend/src/KLC.Domain.Shared/Enums/ConnectorType.cs`
- `src/backend/src/KLC.Domain.Shared/Localization/KLC/en.json`
- `src/backend/src/KLC.Domain.Shared/Localization/KLC/vi.json`
- `src/admin-portal/src/lib/api.ts` (ConnectorType mapping)
- `src/driver-app/src/screens/StationDetailScreen.tsx` (icon mapping)

---

### Phase 2 — Chargecore Integration (June–August 2026)

#### GAP-ADV-01: LINK & LOOP Power Sharing
**Effort:** 2-3 weeks | **Impact:** Chargecore hardware power optimization

**Concept:** LINK & LOOP allows power modules from idle charger segments to be shared with adjacent connectors. Up to 10 chargers per system share a common DC bus.

**Domain Model:**
```
PowerSharingGroup (new entity)
├── Id: Guid
├── Name: string
├── MaxTotalPowerKw: decimal
├── AllocatedPowerKw: decimal (runtime)
├── Strategy: PowerSharingStrategy (enum: EqualDistribution, ProportionalDemand, PriorityBased)
├── Members: Collection<PowerSharingGroupMember>
│   ├── StationId: Guid
│   ├── ConnectorNumber: int
│   ├── MinPowerKw: decimal (guaranteed minimum)
│   ├── MaxPowerKw: decimal (hardware limit)
│   ├── CurrentAllocatedKw: decimal (runtime)
│   └── Priority: int (for PriorityBased strategy)
└── IsEnabled: bool
```

**Implementation Steps:**
1. Create `PowerSharingGroup` and `PowerSharingGroupMember` entities in `KLC.Domain/Stations/`
2. Create `IPowerSharingService` domain service with `RecalculateAllocation()` method
3. Add EF Core migration for new tables
4. Create `PowerSharingGroupAppService` (CRUD, member management)
5. Hook into `StopTransaction` and `StartTransaction` to trigger reallocation
6. Use `SetChargingProfile` CSMS→CP command to push power limits to chargers
7. Add admin portal page for power sharing group management
8. Add real-time allocation display to monitoring dashboard

**Key Business Rules:**
- Sum of allocated power ≤ group's MaxTotalPowerKw
- Each member gets at least MinPowerKw when charging
- Idle connectors release their allocation to active ones
- Rebalancing triggered on: session start, session stop, every 5 minutes

#### GAP-ADV-02: Dynamic Load Balancing
**Effort:** 2-3 weeks | **Impact:** Grid protection, cost optimization

**Concept:** Manages total site power consumption by adjusting charger output based on grid capacity and uncontrollable loads (HVAC, lighting, etc.).

**Domain Model:**
```
SiteLoadProfile (new entity)
├── Id: Guid
├── StationGroupId: Guid (linked to existing StationGroup)
├── GridCapacityKw: decimal (max power from grid)
├── ReservedForOtherLoadsKw: decimal (non-EV loads)
├── AvailableForChargingKw: decimal (computed = Grid - Reserved)
├── CurrentSiteLoadKw: decimal (runtime, from smart meter)
├── DistributionStrategy: LoadDistributionStrategy (Average, Proportional, Dynamic)
├── MeterIntegrationEnabled: bool
├── SmartMeterEndpoint: string? (Modbus/REST URL for site meter)
└── IsEnabled: bool
```

**Distribution Strategies:**
- **Average:** Equal split across all active connectors
- **Proportional:** Based on each connector's max power rating
- **Dynamic:** Based on vehicle demand (SoC-aware — give more to low-SoC vehicles)

**Implementation Steps:**
1. Create `SiteLoadProfile` entity in `KLC.Domain/Stations/`
2. Create `ILoadBalancingService` domain service
3. Hook into MeterValues (for SoC awareness) and StatusNotification
4. Background service: poll site smart meter (if integrated) every 30 seconds
5. Push updated ChargingProfiles via `SetChargingProfile` when load changes
6. Admin portal: Site load management page with real-time visualization
7. Monitoring dashboard: Add site power utilization chart

#### GAP-PLT-01: Operator API (Cloud-to-Cloud)
**Effort:** 1-2 weeks | **Impact:** B2B integration with external operators

**Concept:** Expose a REST API for external operators to manage their stations, view sessions, and receive webhooks — enabling Chargecore's "Cloud-to-cloud" deployment model.

**Endpoints:**
```
POST /api/v1/operator/auth/token        — OAuth2 client credentials
GET  /api/v1/operator/stations           — List operator's stations
GET  /api/v1/operator/stations/{id}      — Station detail + connectors
GET  /api/v1/operator/sessions           — Session history
GET  /api/v1/operator/sessions/active    — Active sessions
POST /api/v1/operator/commands/start     — Remote start
POST /api/v1/operator/commands/stop      — Remote stop
GET  /api/v1/operator/analytics/summary  — Usage analytics
POST /api/v1/operator/webhooks           — Register webhook URL
```

**Implementation:**
1. Create `Operator` entity (company, API key, allowed stations)
2. Add API key authentication middleware
3. Create Minimal API endpoints in new `KLC.Operator.API` project (or extend Admin API)
4. Webhook system: POST events (session_started, session_completed, fault_detected) to registered URLs
5. Rate limiting: 1000 req/min per operator
6. Documentation: OpenAPI/Swagger spec for external developers

#### GAP-FLEET-01: Fleet Management Dashboard
**Effort:** 3-4 weeks | **Impact:** B2B fleet operators (taxis, delivery, buses)

**Concept:** Add a fleet management layer allowing fleet operators to manage drivers, vehicles, and charging policies.

**Domain Model:**
```
Fleet (new entity)
├── Id: Guid
├── Name: string
├── OperatorId: Guid (company)
├── MaxMonthlyBudget: decimal
├── ChargingPolicy: ChargingPolicyType (AnytimeAnywhere, ScheduledOnly, ApprovedStationsOnly)
├── Vehicles: Collection<FleetVehicle>
│   ├── VehicleId: Guid (ref to existing Vehicle)
│   ├── DriverUserId: Guid
│   ├── DailyChargingLimitKwh: decimal?
│   └── AllowedStationGroupIds: List<Guid>?
└── IsActive: bool
```

**Admin Portal Pages:**
- Fleet list & CRUD
- Fleet vehicle assignment
- Fleet charging report (cost, energy, sessions by driver/vehicle)
- Fleet policy configuration
- Fleet budget tracking with alerts

#### GAP-HW-02: Metering Accuracy Class
**Effort:** 1 day | **Impact:** Billing compliance documentation

**Changes:**
1. Add `MeteringAccuracyClass` field to `Connector` entity (string, e.g., "Class 1.0", "MID")
2. Display in admin portal station detail
3. Migration for new column

#### GAP-PAY-01: POS Terminal Integration
**Effort:** 1-2 weeks | **Impact:** On-site contactless payment

**Concept:** Support physical POS terminals at charging stations for card-tap payment.

**Implementation:**
1. Add `PaymentGateway.PosTerminal` enum value
2. Create `IPosTerminalService` interface
3. OCPP DataTransfer message for POS transaction data
4. Payment reconciliation between POS and CSMS
5. Admin portal: POS terminal management per station

---

### Phase 3 — Future (September 2026+)

#### GAP-OCPP-01: OCPP 2.0.1 Support
- New message parser for OCPP 2.0.1 JSON format
- Variable framework (replaces fixed configuration keys)
- CSMS-initiated transactions
- Display messages
- Certificate management
- Security profiles (1-3)

#### GAP-OCPP-02: ISO 15118 Plug and Charge
- TLS certificate chain management
- Contract certificate installation
- Automated authorization (no RFID/app needed)

#### GAP-PLT-02: White Label Configuration
- Configurable branding (logo, colors, domain)
- Per-operator mobile app skinning
- Custom email/notification templates

#### GAP-FLEET-02: Vehicle Telematics
- Integration with vehicle OBD/telematics APIs
- Real-time SoC monitoring (independent of charger)
- Route optimization with charging stops
- Predictive charging scheduling

---

## 5. KLC Competitive Advantages (Already Ahead of Chargecore)

| Area | KLC Advantage |
|------|---------------|
| **Payment diversity** | 5 gateways (MoMo, VnPay, ZaloPay, Wallet, QR) vs. Chargecore's basic gateway integration |
| **Real-time monitoring** | SignalR WebSocket dashboard with live station/session/alert updates |
| **TOU pricing** | 3-tier Vietnam EVN schedule with time-of-use pricing |
| **Audit logging** | Structured audit trail for payments, OCPP, sessions, auth events |
| **Admin portal** | Full-featured Next.js dashboard with 24+ pages, i18n, a11y |
| **Mobile app** | 10 screens with wallet, favorites, notifications, vehicle management |
| **Test coverage** | 1,030 tests across backend + frontend |
| **Vietnamese market** | Localized (VI+EN), VND currency, Vietnam-specific payment gateways |
| **PostGIS spatial** | Geospatial station search, clustering, distance calculation |
| **Vendor profiles** | Auto-detection for Chargecore + JUHANG with quirk normalization |

---

## 6. Recommended Roadmap

```
Phase 1 (May 2026) — MVP Launch
  ├── GAP-HW-01: NACS connector type (1 day)
  └── Existing MVP completion tasks

Phase 2 (Jun-Aug 2026) — Chargecore Integration
  ├── GAP-ADV-01: LINK & LOOP Power Sharing (3 weeks)
  ├── GAP-ADV-02: Dynamic Load Balancing (3 weeks)
  ├── GAP-PLT-01: Operator API (2 weeks)
  ├── GAP-FLEET-01: Fleet Management (3 weeks)
  ├── GAP-HW-02: Metering accuracy (1 day)
  └── GAP-PAY-01: POS Terminal (2 weeks)

Phase 3 (Sep 2026+) — Future
  ├── GAP-OCPP-01: OCPP 2.0.1
  ├── GAP-OCPP-02: ISO 15118
  ├── GAP-PLT-02: White Label
  └── GAP-FLEET-02: Vehicle Telematics
```

---

## 7. Impact on Existing Documentation

| Document | Required Update |
|----------|----------------|
| `docs/02-requirements/functional-requirements.md` | Add FR for power sharing, load balancing, fleet management, operator API |
| `docs/02-requirements/integration-requirements.md` | Add Chargecore LINK & LOOP protocol, POS terminal, operator API |
| `docs/03-functional-specs/modules/` | New MOD-016 (Power Sharing), MOD-017 (Load Balancing), MOD-018 (Fleet Mgmt), MOD-019 (Operator API) |
| `docs/04-architecture/ocpp-architecture.md` | Add power sharing flow, load balancing architecture, OCPP 2.0.1 roadmap |
| `docs/04-architecture/database-design.md` | Add PowerSharingGroup, SiteLoadProfile, Fleet, Operator entities |
| `docs/04-architecture/system-overview.md` | Add Phase 2 components diagram |
| `CLAUDE.md` | Add new domain entities, enums, modules |
| `memory-bank/` | Update with Phase 2 plan |
