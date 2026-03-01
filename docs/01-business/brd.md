# Business Requirements Document (BRD)
## EV Charging Station Management System — K-Charge

> Status: APPROVED | Version: 0.1 | Last Updated: 2026-02-26 | PIC: Nam Mai

---

## 1. Executive Summary

The client (K-Charge) plans to establish a subsidiary company in Vietnam to operate an independent EV charging station network. The strategic goal is to contribute to a more open EV ecosystem, allowing multiple electric vehicle brands to access charging infrastructure rather than being limited to a single manufacturer ecosystem.

To support this vision, the client requires a centralized software system consisting of:

- **A Web Admin Portal** for internal management and operational control
- **A Mobile Application (iOS & Android)** for EV drivers

The system will enable real-time charger monitoring, flexible pricing configuration, secure digital payments, and performance analytics. This platform will serve as the digital foundation for building a scalable, reliable, and commercially sustainable EV charging network across Vietnam.

---

## 2. Project Objectives

1. **Build an Open Charging Ecosystem** — Allow various EV brands and partners to access a shared infrastructure.
2. **Maximize Station Utilization** — Monitor usage, optimize pricing, and reduce idle time.
3. **Ensure Operational Efficiency** — Provide centralized monitoring and management tools to minimize downtime and optimize station utilization.
4. **Deliver Seamless User Experience** — Enable users to find stations easily, see real-time availability, start charging seamlessly, and pay digitally.
5. **Enable Data-Driven Decisions** — Provide dashboards for revenue by location, peak-hour analysis, utilization rate, and customer behavior insights.

---

## 3. Project Scope

### 3.1. Web Admin Portal (Internal System)

The Web Portal supports: station and charger management, real-time charger monitoring, pricing and tariff configuration, user account management, charging session tracking, payment and transaction monitoring, reporting and dashboard, maintenance and alert management.

### 3.2. Mobile Application (Customer System)

The Mobile App supports: user registration and authentication, profile and vehicle management, search nearby charging stations (GPS-based), view real-time charger availability, start and stop charging sessions (QR-based), digital payment processing, charging history and invoice access, push notifications.

---

## 4. High-level Business Requirements

| ID | Requirement | Description |
|----|-------------|-------------|
| BR-1 | Infrastructure Management | Manage charging stations and chargers, including configuration, activation, and performance monitoring. |
| BR-2 | Real-Time Monitoring | Real-time visibility into charger status (Available, Charging, Offline, Faulted) and generate alerts when issues occur. |
| BR-3 | Charging Session Management | Manage complete lifecycle of charging sessions: initiation, tracking energy, calculating cost, termination. |
| BR-4 | Pricing & Tariff Management | Support flexible pricing: per kWh, time-of-use, location-based, membership discounts, tax configuration. |
| BR-5 | Payment & Billing | Secure digital payments and automatic invoice generation for completed sessions. |
| BR-6 | Customer Management | EV drivers register, manage profiles, and store payment methods securely. |
| BR-7 | Reporting & Analytics | Dashboards and reports: revenue per station, utilization rate, session trends, peak-hour analysis. |
| BR-8 | Maintenance & Support | Issue logging and maintenance tracking for chargers. |

---

## 5. System Components

### 5.1. Web Admin Portal

**Actors:** Admin (highest authority), Operator (daily operations), Finance Team (financial tracking), Technical Support (maintenance).

**Core Use Cases:**

| Use Case | Description |
|----------|-------------|
| Manage Stations & Chargers | Configure stations and chargers. Scalability for new locations, control over hardware configuration. |
| Monitor Charger Status | Real-time visibility into availability, health, connectivity. Alerts for technical intervention. |
| Pricing & Tariff Management | Configure per kWh, peak-hour, dynamic pricing and tax rules. |
| Transactions Tracking | Monitor all sessions and payments. Validate revenue, detect anomalies. |
| Reports & Dashboard | Aggregated data on revenue, utilization, peak-hour demand. Data-driven decisions. |
| Maintenance & Alerts | Receive alerts, manage maintenance tickets for faulty chargers. |

### 5.2. Mobile Application

**Actor:** Driver / Customer — EV owners who locate stations, start/stop sessions, pay, track activity.

**Core Use Cases:**

| Use Case | Description |
|----------|-------------|
| Register / Login & Profile | Create accounts, manage personal and payment information. |
| Find Station & View Availability | Locate nearby stations, check real-time availability. |
| Start Charging (QR Scan) | Scan QR code to initiate. Links charger to account, tracks consumption. |
| Stop Charging | End session, calculate energy usage and cost. |
| Make Payment | Digital payment processed after session completion. |
| View History & Invoice | Review past sessions and export invoices. |

---

## 6. Functional Requirements

| BR | FR ID | Function | Description |
|----|-------|----------|-------------|
| BR-1 | FR-1.1 | Station Management | Create, configure, activate, deactivate stations with location, hours, pricing. |
| BR-1 | FR-1.2 | Charger Management | Manage chargers: configuration, enable/disable, maintenance mode, monitoring. |
| BR-2 | FR-2.1 | Charger Status Monitoring | Real-time status (Available, Charging, Offline, Faulted), active sessions. |
| BR-2 | FR-2.2 | Alert Management | Auto-detect abnormal conditions, generate alerts, notify Technical Support. |
| BR-3 | FR-3.1 | Session Initiation | QR code scan, validate availability, link to account, record start time. |
| BR-3 | FR-3.2 | Session Tracking | Real-time energy consumption, duration, charging status. |
| BR-3 | FR-3.3 | Session Termination | End session, record completion, finalize energy data. |
| BR-3 | FR-3.4 | Cost Calculation | Calculate cost based on energy, tariff, discounts, tax. |
| BR-4 | FR-4.1 | Tariff Configuration | Per kWh rates, time-of-use, location-based pricing, effective dates. |
| BR-4 | FR-4.2 | Membership & Discount | Membership tiers, discount policies, auto-apply during billing. |
| BR-4 | FR-4.3 | Tax Configuration | Configure tax rates, correct display in billing and invoices. |
| BR-5 | FR-5.1 | Payment Processing | Payment gateway integration, handle responses, success/failure scenarios. |
| BR-5 | FR-5.2 | Invoice Management | Auto-generate invoices with session details and tax breakdown. |
| BR-6 | FR-6.1 | User Registration & Auth | Account creation, identity verification, secure login. |
| BR-6 | FR-6.2 | Profile Management | Update personal info, manage vehicles, view history. |
| BR-6 | FR-6.3 | Payment Method Management | Add, remove, manage stored payment methods. |
| BR-7 | FR-7.1 | Dashboard | Visual KPIs: revenue, utilization rate, operational statistics. |
| BR-7 | FR-7.2 | Reporting | Revenue, session trends, peak-hour analysis with filtering and export. |
| BR-8 | FR-8.1 | Maintenance Tickets | Auto/manual ticket creation, assignment, tracking, resolution. |
| BR-8 | FR-8.2 | Maintenance Log | Historical record of issues, maintenance, resolutions. |

---

## 7. Non-Functional Requirements

### 7.1. Performance
- User action response time: within 5 seconds under normal conditions
- Payment confirmation: within 3 seconds after gateway response
- Charger status updates: within 10 seconds of actual change

### 7.2. Availability
- Minimum 99.5% uptime per month
- Planned maintenance communicated in advance

### 7.3. Scalability
- Support growing stations and concurrent users
- Horizontal scaling architecture

### 7.4. Security
- Data encrypted in transit (HTTPS/TLS) and at rest
- PCI-DSS compliance for payments
- RBAC for Web Admin Portal
- Secure password policies

### 7.5. Reliability
- 100% transactional integrity for session data
- No duplicate sessions or billing records
- Session data buffered and synced on network restoration

### 7.6. Audit & Logging
- All admin actions logged (create, update, delete, pricing changes)
- Logs retained minimum 12 months

---

## 8. Assumptions & Constraints

### Assumptions
- All charging stations support compatible OCPP version
- Real-time data transmission without major delay
- Stable internet connectivity at station locations
- All payments are cashless (digital only)
- User registration mandatory before charging

### Constraints
- **Regulatory:** Data protection laws, financial audit storage, tax compliance, encrypted personal data
- **Network:** Performance depends on internet quality at stations
- **Time:** Deliver within timeline; Phase 1 prioritizes core features
- **Budget:** Operate within approved budget; balance cost and scalability
- **Technical:** Integrate with existing hardware; OCPP version depends on firmware

---

## 9. Key Business Processes

### 9.1. Charging Vehicle Flow
1. Driver opens app → selects nearby station
2. System checks charger availability
3. Driver scans QR code → system validates → starts charging
4. System tracks real-time energy consumption
5. Charging ends → system calculates cost (tariff-based)
6. Driver proceeds with payment (integrated gateway)
7. Success → generate invoice, send via email
8. Failure → prompt retry

### 9.2. Operations Monitoring Flow
1. System continuously monitors charger status
2. Normal → continue monitoring
3. Abnormal → trigger alert, notify Technical Support
4. Create maintenance ticket → diagnose on-site
5. Resolved → update status, close ticket, record maintenance log
6. Return to continuous monitoring
