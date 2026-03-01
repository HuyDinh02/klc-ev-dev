# Project Plan — EV Charging CSMS

> Status: ACTIVE | Last Updated: 2026-03-01 | Source: Kickoff Document

---

## Timeline Overview
- **Kick-off:** March 1, 2026
- **Interactive Prototype:** March 15, 2026
- **MVP Go-live:** June 1, 2026
- **Total Duration:** 4 months (Phase 1)

---

## Milestone Schedule

| Date | Milestone | Deliverables | Status |
|------|-----------|-------------|--------|
| Mar 1, 2026 | Kick-off | Project setup, docs structure, team alignment | ✅ Done |
| Mar 15, 2026 | Interactive Prototype | UI demo of Admin Portal and Mobile App | 🔄 In Progress |
| Mar 31, 2026 | Station & Connector Mgmt | Station CRUD, connector registration, config, enable/disable | ⏳ Planned |
| Apr 30, 2026 | Real-time Monitoring + App Core | OCPP real-time status monitoring with history. Mobile: vehicle management, QR charging, real-time status display | ⏳ Planned |
| May 31, 2026 | MVP Complete | Energy metering (kWh collection, validation, aggregation), fault detection & logging, payment integration (ZaloPay, MoMo, OnePay), billing & invoicing | ⏳ Planned |
| Jun 1, 2026 | **MVP Go-live** | Production deployment | ⏳ Planned |
| Jun 30, 2026 | Post-MVP Phase 2a | Firmware tracking, idle fee detection & calculation, ops manual override (unlock, status override), peak/off-peak pricing config. Mobile: charging/payment history, profile & account management | ⏳ Phase 2 |
| Jul 31, 2026 | Post-MVP Phase 2b | Station availability (reservation lock & expiry), firmware update monitoring, station grouping by region | ⏳ Phase 2 |
| Aug 31, 2026 | Post-MVP Phase 2c | Audit logging, mobile notifications & alerts. AI: energy optimization, anomaly detection, predictive maintenance | ⏳ Phase 2 |

---

## Phase 1 Feature Scope (Confirmed with Client)

### Charging Station Management
1. Station Registration & Identity (create, update, decommission)
2. Connector Management (register, configure, enable/disable)
3. Real-time Status Monitoring (via OCPP)
4. Energy Metering (collection, validation, aggregation)
5. Fault Detection & Management
6. Station Availability (basic: status visibility only)
7. Station Grouping (basic: simple by region)
8. Audit Log (basic: logging & audit trail)
9. Tariff/Pricing Configuration

### Mobile Application
1. Vehicle Management & Profile
2. Charging Interaction (QR scan, real-time status)
3. Payment & Billing
4. Charging History & Payment History
5. User Profile & Account Management
6. Notifications & Alerts (audit log based)

---

## Phase 2 Features (Deferred)

### Charging Station Management
- Idle fee calculation (when user volume grows)
- Ops manual override (Phase 1 focuses on auto operations)
- Full station lifecycle & firmware management

### AI & Analytics
- Energy optimization intelligence
- Operational intelligence (anomaly detection)
- Predictive maintenance (failure risk scoring)
- Electrical meter data aggregation
