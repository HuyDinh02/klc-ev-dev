# User Stories

> Status: APPROVED | Last Updated: 2026-03-01 | Source: BRD, Features Excel

---

## Personas

1. **EV Driver** — Uses mobile app to find, charge, and pay
2. **Station Operator** — Monitors and manages charging stations via admin portal
3. **Administrator** — Manages system configuration, users, billing, pricing
4. **Finance Team** — Tracks revenue, validates transactions, ensures compliance
5. **Technical Support** — Handles faults, manages maintenance tickets

---

## EV Driver Stories (Mobile App)

| ID | Story | Acceptance Criteria | Phase |
|----|-------|-------------------|-------|
| US-D01 | As a driver, I want to register an account so I can use the charging network | Account created with email verification, secure login | Phase 1 |
| US-D02 | As a driver, I want to add my EV to my profile so the system knows my vehicle | Add/edit/delete vehicles with make, model, connector type | Phase 1 |
| US-D03 | As a driver, I want to select my active vehicle before charging | Choose which vehicle is being charged for session tracking | Phase 1 |
| US-D04 | As a driver, I want to find nearby charging stations on a map | GPS-based station finder with availability indicators | Phase 1 |
| US-D05 | As a driver, I want to scan a QR code to start charging | QR scan validates charger, links to account, starts session | Phase 1 |
| US-D06 | As a driver, I want to see real-time charging progress | Live display of duration, energy consumed, estimated cost | Phase 1 |
| US-D07 | As a driver, I want to pay via ZaloPay/MoMo/OnePay | Multiple payment methods, secure processing | Phase 1 |
| US-D08 | As a driver, I want to manage my payment methods | Add, remove, set default payment method | Phase 1 |
| US-D09 | As a driver, I want to view my charging history | List of past sessions with time, station, energy, cost | Phase 1 |
| US-D10 | As a driver, I want to view my payment history | Transaction history with cost breakdown | Phase 1 |
| US-D11 | As a driver, I want to manage my profile and security | Update personal info, change password, account settings | Phase 1 |
| US-D12 | As a driver, I want to receive notifications when charging is complete | Push notification for charge complete, fee alerts, issues | Phase 2 |
| US-D13 | As a driver, I want to check my invoices | View and export e-invoices for past sessions | Phase 1 |

## Station Operator Stories (Admin Portal)

| ID | Story | Acceptance Criteria | Phase |
|----|-------|-------------------|-------|
| US-O01 | As an operator, I want to see real-time status of all stations | Dashboard with Available/Charging/Faulted/Offline status | Phase 1 |
| US-O02 | As an operator, I want to receive fault alerts | Auto-alerts when abnormal conditions detected | Phase 1 |
| US-O03 | As an operator, I want to view energy consumption data | kWh, voltage, current data per session and station | Phase 1 |
| US-O04 | As an operator, I want to view historical status logs | Audit trail of all status changes | Phase 1 |
| US-O05 | As an operator, I want to unlock a connector remotely | Manual unlock for operational recovery | Phase 2 |
| US-O06 | As an operator, I want to override station status | Manual status override for recovery | Phase 2 |

## Administrator Stories (Admin Portal)

| ID | Story | Acceptance Criteria | Phase |
|----|-------|-------------------|-------|
| US-A01 | As an admin, I want to register new charging stations | Create station with ID, location, metadata | Phase 1 |
| US-A02 | As an admin, I want to manage connectors per station | Register, configure, enable/disable connectors | Phase 1 |
| US-A03 | As an admin, I want to configure tariff plans | Set per kWh, time-of-use, location-based pricing | Phase 1 |
| US-A04 | As an admin, I want to decommission a station | Mark inactive without losing historical data | Phase 1 |
| US-A05 | As an admin, I want to track firmware versions | View firmware version per charger | Phase 1 |
| US-A06 | As an admin, I want to group stations by region | Organize stations into logical groups | Phase 2 |
| US-A07 | As an admin, I want to view audit logs | All operations logged with timestamps | Phase 2 |
| US-A08 | As an admin, I want to configure peak/off-peak pricing | Time-based pricing rules | Phase 2 |

## Finance Team Stories

| ID | Story | Acceptance Criteria | Phase |
|----|-------|-------------------|-------|
| US-F01 | As finance, I want to view revenue dashboards | Total revenue, revenue per station, trends | Phase 1 |
| US-F02 | As finance, I want to generate reports | Filtered reports with export capability | Phase 1 |
| US-F03 | As finance, I want to track all transactions | Complete transaction log with details | Phase 1 |

## Technical Support Stories

| ID | Story | Acceptance Criteria | Phase |
|----|-------|-------------------|-------|
| US-T01 | As tech support, I want to manage maintenance tickets | Create, assign, track, resolve tickets | Phase 1 |
| US-T02 | As tech support, I want to view fault history | Fault events with timestamps and station references | Phase 1 |
| US-T03 | As tech support, I want to view maintenance logs | Historical record of all maintenance activities | Phase 1 |
