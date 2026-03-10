# Functional Requirements

> Status: APPROVED | Last Updated: 2026-03-01 | Source: BRD v0.1, Features Excel

---

## FR Categories & Detail

### A. Charging Station Management (Admin Portal)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-1.1 | Station Management | Create, configure, activate, deactivate stations with unique ID, location, metadata, operating hours, pricing models | Phase 1 |
| FR-1.2 | Charger/Connector Management | Register connectors per station with type, power rating, status. Update config, enable/disable remotely | Phase 1 |
| FR-2.1 | Real-time Status Monitoring | Monitor station status (Available, Charging, Faulted, Offline) via OCPP. Display current conditions and active sessions | Phase 1 |
| FR-2.2 | Historical Status Log | Store historical status changes for audit and reporting | Phase 1 |
| FR-2.3 | Alert Management | Auto-detect abnormal conditions, generate alerts, notify Technical Support, record history | Phase 1 |
| FR-3.1 | Meter Value Collection | Collect meter values (kWh, voltage, current) during charging sessions | Phase 1 |
| FR-3.2 | Meter Data Validation | Validate and normalize metering data from chargers | Phase 1 |
| FR-3.3 | Meter Data Aggregation | Aggregate metering data for billing and analytics | Phase 1 |
| FR-4.1 | Fault Detection | Detect charger faults and error codes | Phase 1 |
| FR-4.2 | Fault Logging | Store fault events with timestamp and station reference | Phase 1 |
| FR-5.1 | Tariff Configuration | Define per kWh rates, time-of-use pricing, location-based pricing, effective dates | Phase 1 |
| FR-5.2 | Membership & Discount | Membership tiers and discount policies, auto-apply during billing | Phase 1 |
| FR-5.3 | Tax Configuration | Configure tax rates for billing and invoices | Phase 1 |
| FR-6.1 | Station Decommissioning | Mark station as inactive/retired without deleting historical data | Phase 1 |
| FR-6.2 | Firmware Version Tracking | Track firmware versions reported by chargers | Phase 1 |
| FR-7.1 | Station Availability Check | Determine if station/connector is available | Phase 2 |
| FR-7.2 | Reservation Lock | Temporarily lock connector when user starts charging | Phase 2 |
| FR-7.3 | Reservation Expiry | Release reservation if user doesn't start charging | Phase 2 |
| FR-8.1 | Station Grouping | Group stations by site, region, or operator | Phase 2 |
| FR-9.1 | Operation Audit Log | Log all station-related operations (config, start/stop) | Phase 2 |
| FR-10.1 | Idle Time Detection | Detect idle time after charging completes while connector occupied | Phase 2 |
| FR-10.2 | Idle Fee Calculation | Calculate idle fee after grace period | Phase 2 |
| FR-11.1 | Ops Unlock Connector | Manually unlock connector by operator | Phase 2 |
| FR-11.2 | Manual Status Override | Override station/connector status for recovery | Phase 2 |
| FR-12.1 | Time-Based Pricing | Configure peak/off-peak pricing | Phase 2 |
| FR-13.1 | Firmware Update Monitoring | Monitor firmware update progress and result | Phase 2 |
| FR-14.1 | Dashboard | Visual KPIs: revenue, utilization, operational statistics | Phase 1 |
| FR-14.2 | Reporting | Revenue, session trends, peak-hour analysis with filtering and export | Phase 1 |
| FR-15.1 | Maintenance Tickets | Auto/manual creation, assignment, tracking, resolution | Phase 1 |
| FR-15.2 | Maintenance Log | Historical record of issues and resolutions | Phase 1 |

### B. Mobile Application

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-M01 | Add & Manage Vehicles | Add, edit, manage EVs with basic vehicle information | Phase 1 |
| FR-M02 | Active Vehicle Selection | Select active vehicle for charging sessions | Phase 1 |
| FR-M03 | QR Scan to Start Charging | Scan QR codes at stations to start sessions | Phase 1 |
| FR-M04 | Real-time Charging Status | Display duration, energy consumed, estimated cost in real time | Phase 1 |
| FR-M05 | Charging Session Payment | Pay for sessions using supported payment methods | Phase 1 |
| FR-M06 | Payment Method Management | Add, update, manage preferred payment methods | Phase 1 |
| FR-M07 | Charging History | List past sessions with time, station, energy details | Phase 1 |
| FR-M08 | Payment History | Payment transaction history with cost breakdown | Phase 1 |
| FR-M09 | User Profile Management | Manage personal info, account settings, security preferences | Phase 1 |
| FR-M10 | Charging & Fee Notifications | Notifications for completion, fee alerts, charging issues | Phase 2 |

### C. AI & Analytics (Phase 2)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-AI01 | Site Power Capacity Awareness | Analyze charging data to infer site power constraints and peak load | Phase 2 |
| FR-AI02 | Power Optimization Recommendation | Recommend per-connector power adjustments to avoid overload | Phase 2 |
| FR-AI03 | Abnormal Pattern Detection | Detect abnormal charging behaviors from historical data | Phase 2 |
| FR-AI04 | Failure Risk Scoring | Estimate charger failure risks based on fault frequency and usage | Phase 2 |

### D. Power Management (Phase 2)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-PM-01 | Power Sharing Groups | System SHALL support Power Sharing Groups where up to 10 chargers share a common power pool (LINK & LOOP) | Phase 2 |
| FR-PM-02 | Dynamic Load Balancing | System SHALL implement Dynamic Load Balancing with three strategies: Average Distribution, Proportional Distribution, Dynamic Distribution | Phase 2 |
| FR-PM-03 | Power Rebalancing | System SHALL automatically rebalance power allocation when sessions start/stop within a Power Sharing Group | Phase 2 |
| FR-PM-04 | Charging Profile Push | System SHALL push updated ChargingProfiles to chargers via OCPP SetChargingProfile when power allocation changes | Phase 2 |
| FR-PM-05 | Grid Capacity Tracking | System SHALL track site-level grid capacity and reserve power for non-EV loads | Phase 2 |
| FR-PM-06 | Smart Meter Integration | System SHALL support smart meter integration (Modbus/REST) for real-time site load monitoring | Phase 2 |

### E. Fleet Management (Phase 2)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-FL-01 | Fleet Entities | System SHALL support Fleet entities with operator association, vehicles, drivers, and charging policies | Phase 2 |
| FR-FL-02 | Fleet Charging Policies | System SHALL enforce fleet charging policies (AnytimeAnywhere, ScheduledOnly, ApprovedStationsOnly) | Phase 2 |
| FR-FL-03 | Fleet Budget Tracking | System SHALL track fleet charging budgets with monthly limits and alerts | Phase 2 |
| FR-FL-04 | Fleet Reporting | System SHALL generate fleet charging reports (cost, energy, sessions by driver/vehicle) | Phase 2 |
| FR-FL-05 | Vehicle-Driver Assignment | System SHALL support fleet-level vehicle-to-driver assignment | Phase 2 |

### F. Operator API (Phase 2)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-OP-01 | External Operator API | System SHALL expose REST API for external operators (cloud-to-cloud integration) | Phase 2 |
| FR-OP-02 | Operator Authentication | System SHALL support OAuth2 client credentials authentication for operator API | Phase 2 |
| FR-OP-03 | Webhook Notifications | System SHALL support webhook registration for real-time event notifications to operators | Phase 2 |
| FR-OP-04 | Operator Rate Limiting | System SHALL rate-limit operator API to 1000 requests/minute per operator | Phase 2 |

### G. Hardware Support (Phase 2)

| FR ID | Function | Description | Phase |
|-------|----------|-------------|-------|
| FR-HW-01 | NACS Connector Support | System SHALL support NACS (Tesla) connector type | Phase 2 |
| FR-HW-02 | Metering Accuracy Class | System SHALL track metering accuracy class per connector (e.g., Class 1.0, MID) | Phase 2 |
