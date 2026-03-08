# Admin Portal Information Architecture

**Date:** 2026-03-08
**Scope:** K-Charge EV Charging Admin Portal navigation restructure
**Status:** Proposal

---

## 1. Problem Statement

The current admin portal sidebar contains **19 flat menu items** with no grouping, no section labels, and no visual hierarchy. This causes:

1. **Cognitive overload** -- Users scan 19 items to find what they need. Nielsen's research recommends 7 +/- 2 top-level items for effective navigation.
2. **No conceptual grouping** -- "Monitoring" sits next to "Analytics" but is separated from "OCPP Management" (which is its operational counterpart). "Payments" is far from "E-Invoices" even though operators use them together.
3. **Low-frequency items compete with high-frequency items** -- "Vehicles" (rarely accessed by admin users) occupies the same visual weight as "Monitoring" (viewed constantly by operators).
4. **Hidden critical tools** -- OCPP Management (remote commands for chargers) is the 19th item, below Feedback and Mobile Users.
5. **Duplicate functionality** -- Station Map is a separate page but is conceptually a view mode of Stations. Alerts appears in both sidebar footer and header.
6. **Missing from navigation** -- Settings is only accessible from the user section footer, not discoverable from the main nav.

### Current Navigation (flat list)

```
1.  Dashboard
2.  Stations
3.  Station Map
4.  Monitoring
5.  Analytics
6.  Sessions
7.  Tariffs
8.  Payments
9.  Faults
10. Maintenance
11. Station Groups
12. Audit Logs
13. E-Invoices
14. Vehicles
15. Vouchers
16. Promotions
17. Feedback
18. Mobile Users
19. OCPP Management
20. User Management
--  Alerts (sidebar footer)
--  Settings (user section footer)
```

---

## 2. Proposed Navigation

The proposed structure reduces top-level navigation from 19 items to **12 items in 5 sections**, with 2 low-frequency items moved to a secondary location. Related pages are merged where they share the same user workflow.

### Visual Sidebar Layout

```
[K-Charge Logo]

OPERATIONS
  Dashboard
  Stations                    (absorbs Station Map, Station Groups)
  Monitoring                  (absorbs OCPP Management)
  Sessions

INCIDENTS
  Faults & Alerts             (merges Faults + Alerts)
  Maintenance

BUSINESS
  Tariffs
  Payments                    (absorbs E-Invoices)
  Marketing                   (merges Vouchers + Promotions)

USERS
  Users                       (merges User Management + Mobile Users)

SYSTEM
  Reports                     (merges Analytics + Audit Logs)
  Settings

---
[User Profile]
[Logout]
```

**Items relocated to Settings sub-pages:**
- Vehicles (low admin frequency -- primarily a driver/BFF concern)
- Feedback (low frequency, can be a Settings > Support section or standalone under System if volume grows)

---

## 3. Detailed Page Specifications

### OPERATIONS Section

This section covers the core daily workflow: check the dashboard, manage stations, monitor live status, and track sessions. These are the pages operators use for 80%+ of their workday.

---

#### 3.1 Dashboard

**URL:** `/`
**Icon:** `LayoutDashboard`
**Purpose:** At-a-glance operational health of the entire charging network. The first thing an operator sees after login.

**Main Widgets:**
- **4 KPI Cards** (top row, equal width):
  - Active Sessions: count + "X connectors in use" subtext
  - Network Availability: "X/Y stations online" with percentage
  - Energy Today: kWh delivered with trend arrow (vs yesterday)
  - Revenue Today: VND formatted with trend arrow (vs yesterday)
- **Station Status Donut Chart** (left, 60% width): Available / Occupied / Offline / Faulted breakdown with click-to-filter
- **Energy Trend Line Chart** (right, 40% width): Last 7 days daily kWh with area fill
- **Recent Alerts List** (bottom): Last 5 alerts with severity badge, station name, timestamp, and link to Faults & Alerts page

**Common Actions:**
- Click any KPI card to navigate to the relevant detail page
- Click a donut segment to filter the stations list
- Click an alert to navigate to the fault detail

**Default Filters:** Last 24 hours for KPIs, last 7 days for trend chart.

**Role Visibility:** All roles (Admin, Operator, Viewer, Finance, Auditor).

**Data Refresh:** 30-second polling. SignalR pushes update KPI counts in real-time when connected.

---

#### 3.2 Stations

**URL:** `/stations`
**Icon:** `MapPin`
**Purpose:** Full station lifecycle management -- create, configure, enable/disable, view status, organize into groups.

**Main Widgets:**
- **View Toggle** (top right): List View | Map View (replaces the separate Station Map page)
  - List View: Card grid with station name, address, status badge, connector summary, last heartbeat
  - Map View: Leaflet map with status-colored markers (current `/map` page embedded here)
- **Filter Bar** (top): Search by name/address, status filter (All / Available / Occupied / Offline / Faulted), Group filter dropdown
- **Station Cards** (list mode): Each card shows station name, address, status badge, connector count with mini-bar (available/charging/faulted), last heartbeat time, enable/disable toggle
- **Station Groups Tab** (tab above list): Switch between "All Stations" and "Groups" view. Groups view shows expandable group cards with member stations. (Absorbs current `/groups` page.)

**Sub-pages:**
- `/stations/[id]` -- Station Detail: Full info, connector list, recent sessions, fault history, OCPP config
- `/stations/new` -- Create Station form
- `/stations/[id]/edit` -- Edit Station form

**Common Actions:**
- Create new station (button top right)
- Enable/disable station (toggle on card)
- Filter by status or group
- Toggle between list and map view
- Click station to view detail
- Batch actions on selected stations (future)

**Default Filters:** All active stations (excluding Decommissioned), sorted by name.

**Role Visibility:**
- Admin: Full CRUD, enable/disable, group management
- Operator: View, enable/disable
- Viewer: View only

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/map` (Station Map) | Map View toggle within Stations page. Same Leaflet map, same markers, but now accessible without leaving the Stations context. |
| `/groups` (Station Groups) | "Groups" tab above the station list. Group CRUD (create, rename, assign stations) available from this tab. |

---

#### 3.3 Monitoring

**URL:** `/monitoring`
**Icon:** `Activity`
**Purpose:** Real-time operational control center. Live status of all connectors with ability to send OCPP commands directly to chargers.

**Main Widgets:**
- **Connection Indicator** (top right): SignalR status (Live / Connecting / Polling) with animated dot
- **4 KPI Cards**: Stations Online (X/Y), Connectors Charging (X/Y), Active Sessions (count), Today's Energy (kWh)
- **Live Connector Grid**: Station cards showing connector status bars (green = available, blue = charging, red = faulted, gray = offline). Updates via SignalR push.
- **OCPP Command Panel** (tab or slide-out panel): Station selector + command dropdown + execute button. Absorbs the current OCPP Management page.
  - Commands: Remote Start, Remote Stop, Reset (Soft/Hard), Unlock Connector, Change Availability, Get Configuration, Change Configuration, Trigger Message, Set Charging Profile, Clear Charging Profile, Update Firmware, Get Diagnostics, Send Local List, Get Local List Version, Set Power Limit, Sync Local List
  - Command history with timestamp, status (success/fail), response payload
- **Real-time Alerts Feed** (bottom): Live alert stream from SignalR, last 10 alerts

**Common Actions:**
- Send OCPP command to a station
- Click a station card to navigate to station detail
- Click an alert to navigate to fault detail
- Refresh all data manually

**Default Filters:** All online stations shown first, then offline.

**Role Visibility:**
- Admin: Full access including all OCPP commands
- Operator: View + limited OCPP commands (Remote Start/Stop, Reset, Unlock)
- Viewer: View only, no command execution

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/ocpp` (OCPP Management) | "Commands" tab within Monitoring page. The OCPP command panel becomes a section/tab alongside the live connector grid. Operators already use monitoring and OCPP commands in the same workflow -- separating them forced unnecessary navigation. |

---

#### 3.4 Sessions

**URL:** `/sessions`
**Icon:** `Zap`
**Purpose:** Charging session tracking, troubleshooting, and historical analysis. Both active and completed sessions.

**Main Widgets:**
- **4 Stat Cards**: Active Sessions, Energy Delivered (page total), Revenue (page total), Total Sessions
- **Filter Bar**: Search (station name, user name), status filter (All / Active / Completed / Failed), date range picker
- **Sessions Table**: Station, User, Status badge, Start Time, Duration, Energy (kWh), Cost (VND). Sortable columns. Click row to open detail.
- **Cursor-based Pagination**: Previous / Next with total count

**Sub-pages:**
- `/sessions/[id]` -- Session Detail: Full timeline, meter values chart (power, energy, SoC), connector info, payment info, OCPP transaction log

**Common Actions:**
- Filter by active/completed/failed
- Search by station or user name
- Click session to view meter values and timeline
- Export session data (future)

**Default Filters:** Active sessions first (status = InProgress or Starting), then most recent.

**Role Visibility:**
- Admin: Full access
- Operator: View all sessions
- Viewer: View only
- Finance: View completed sessions with payment data

---

### INCIDENTS Section

This section groups all reactive/incident-response workflows. When something goes wrong, operators come here.

---

#### 3.5 Faults & Alerts

**URL:** `/faults`
**Icon:** `AlertTriangle`
**Purpose:** Unified incident management -- triage faults, respond to alerts, track resolution.

**Main Widgets:**
- **Severity Summary Cards** (top row): Critical (count, red), High (count, orange), Medium (count, amber), Low (count, gray). Each is clickable to filter.
- **Tabs**: Faults | Alerts
  - **Faults Tab**: Table with station, connector, fault code, severity badge, status badge (Open/Investigating/Resolved/Closed), reported time, assigned operator. Click to open fault detail.
  - **Alerts Tab**: Table with alert type, station, message, severity, timestamp, status (Active/Acknowledged/Resolved). Real-time updates via SignalR.
- **Filter Bar**: Search, severity filter, status filter (Unresolved / All), date range

**Sub-pages:**
- `/faults/[id]` -- Fault Detail: Timeline, connector info, OCPP error codes, resolution notes, related sessions

**Common Actions:**
- Acknowledge an alert
- Change fault status (Open -> Investigating -> Resolved -> Closed)
- Assign fault to operator (future)
- Filter by severity or status
- Navigate to station from fault
- Send OCPP reset command from fault detail (quick action)

**Default Filters:** Unresolved (Open + Investigating), sorted by severity (Critical first), then by time (newest first).

**Role Visibility:**
- Admin: Full access, can close faults
- Operator: View, change status to Investigating/Resolved
- Viewer: View only

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/faults` (Faults) | Faults tab (primary) |
| `/alerts` (Alerts) | Alerts tab within the same page. Removes sidebar footer alerts link. Header bell icon links here. |

---

#### 3.6 Maintenance

**URL:** `/maintenance`
**Icon:** `Wrench`
**Purpose:** Scheduled and reactive maintenance task tracking.

**Main Widgets:**
- **Stat Cards**: Open Tasks, Overdue Tasks, Completed This Month, Average Resolution Time
- **Task List/Board**: Table view with station, task type, status (Scheduled/InProgress/Completed/Overdue), assigned technician, due date, priority
- **Filter Bar**: Status filter, priority filter, assigned filter, date range

**Common Actions:**
- Create maintenance task
- Update task status
- Link task to a fault
- Filter by status or assignee
- Calendar view (future enhancement)

**Default Filters:** Open + Overdue tasks, sorted by due date (soonest first).

**Role Visibility:**
- Admin: Full CRUD
- Operator: View, update status
- Viewer: View only

---

### BUSINESS Section

This section covers revenue-generating configurations and financial records. Primarily used by Admin and Finance roles.

---

#### 3.7 Tariffs

**URL:** `/tariffs`
**Icon:** `DollarSign`
**Purpose:** Pricing configuration -- create and manage tariff plans, assign to stations or groups.

**Main Widgets:**
- **Tariff List**: Table with tariff name, rate type (flat/TOU/tiered), base rate (VND/kWh), status (Active/Draft/Archived), assigned stations count
- **Tariff Editor** (slide-out or sub-page): Name, rate type, pricing tiers, time-of-use schedule, effective dates
- **Assignment Panel**: Assign tariff to station groups or individual stations

**Common Actions:**
- Create new tariff plan
- Edit existing tariff
- Clone tariff as draft
- Assign tariff to stations/groups
- Archive tariff

**Default Filters:** Active tariffs, sorted by name.

**Role Visibility:**
- Admin: Full CRUD
- Finance: View + create drafts
- Operator, Viewer: View only

---

#### 3.8 Payments

**URL:** `/payments`
**Icon:** `Receipt`
**Purpose:** Financial visibility -- transaction history, revenue tracking, invoice management, and e-invoice compliance.

**Main Widgets:**
- **4 Stat Cards**: Today's Revenue, Today's Transactions, Monthly Revenue, Pending/Failed Transactions
- **Tabs**: Transactions | Invoices | E-Invoices
  - **Transactions Tab**: Table with reference code, station, amount (VND), gateway (MoMo/VnPay/ZaloPay/Wallet/etc.), status badge, timestamp. Click to view detail. Refund action for Completed transactions.
  - **Invoices Tab**: Invoice list with download/print actions
  - **E-Invoices Tab**: E-invoice list with issuance status, VietInvoice integration status. (Absorbs current `/e-invoices` page.)
- **Filter Bar**: Search (reference code, station), status filter, gateway filter, date range picker

**Sub-pages:**
- `/payments/[id]` -- Payment Detail: Full transaction info, linked session, refund history

**Common Actions:**
- Search transactions by reference code
- Filter by gateway or status
- Initiate refund (Admin only)
- Download invoice
- Re-issue e-invoice
- Export transaction data (future)

**Default Filters:** Last 30 days, all statuses, sorted by newest first.

**Role Visibility:**
- Admin: Full access including refunds
- Finance: View all + export + invoice management
- Operator: View transactions only
- Viewer: No access

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/payments` (Payments) | Transactions tab (primary) |
| `/e-invoices` (E-Invoices) | E-Invoices tab within Payments page. These are financially linked -- operators check e-invoice status while reviewing transactions. |

---

#### 3.9 Marketing

**URL:** `/marketing`
**Icon:** `Megaphone`
**Purpose:** Marketing campaign management -- vouchers and promotions for driver acquisition and retention.

**Main Widgets:**
- **Tabs**: Vouchers | Promotions
  - **Vouchers Tab**: Table with code, type (Percentage/Fixed/FreeCharge), value, usage (used/limit), expiry date, status (Active/Expired/Depleted). CRUD actions.
  - **Promotions Tab**: Table with name, type, target audience, date range, status. CRUD actions.
- **Stat Cards**: Active Vouchers, Total Redemptions This Month, Active Promotions, Estimated Revenue Impact

**Common Actions:**
- Create voucher with code generation
- Create promotion campaign
- Deactivate voucher/promotion
- View redemption analytics
- Filter by status or type

**Default Filters:** Active items, sorted by creation date (newest first).

**Role Visibility:**
- Admin: Full CRUD
- Marketing role (future): Full CRUD
- Operator, Viewer: View only
- Finance: View with redemption data

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/vouchers` (Vouchers) | Vouchers tab |
| `/promotions` (Promotions) | Promotions tab. These are operationally related -- a promotion campaign often includes vouchers. Managing them together enables better campaign oversight. |

---

### USERS Section

---

#### 3.10 Users

**URL:** `/users`
**Icon:** `Users`
**Purpose:** User lifecycle management and access control for both admin portal users and mobile app users.

**Main Widgets:**
- **Tabs**: Admin Users | Mobile Users | Roles & Permissions
  - **Admin Users Tab**: Table with username, email, role, status (Active/Locked), last login. CRUD actions. Password reset.
  - **Mobile Users Tab**: Table with phone number, name, membership tier, wallet balance, registered date, last active. View profile detail. (Absorbs current `/mobile-users` page.)
  - **Roles & Permissions Tab**: Role list with permission matrix. Create/edit roles, assign permissions from permission groups (Stations, Sessions, Payments, etc.).
- **Filter Bar**: Search (name, email, phone), role filter, status filter

**Common Actions:**
- Create admin user with role assignment
- Lock/unlock admin user
- Reset admin user password
- View mobile user profile and session history
- Create/edit roles and permissions
- Export user list (future)

**Default Filters:** Active users, sorted by username.

**Role Visibility:**
- Admin: Full CRUD on all tabs
- Operator: View Admin Users and Mobile Users (no edit)
- Viewer: No access

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/user-management` (User Management) | Admin Users tab + Roles & Permissions tab |
| `/mobile-users` (Mobile Users) | Mobile Users tab. Admin and mobile user management share RBAC context -- viewing them together helps admins understand the full user ecosystem. |

---

### SYSTEM Section

---

#### 3.11 Reports

**URL:** `/reports`
**Icon:** `BarChart3`
**Purpose:** Historical analysis, operational intelligence, and compliance audit trail.

**Main Widgets:**
- **Tabs**: Analytics | Audit Logs
  - **Analytics Tab**: (Current analytics page content)
    - Date range picker (7d / 30d / 90d)
    - 5 KPI cards: Total Revenue, Total Energy, Total Sessions, Avg Session Duration, Network Uptime
    - Energy Trend Chart (area chart, daily kWh)
    - Sessions Bar Chart (daily session count)
    - Revenue Line Chart (daily VND)
    - MTBF (Mean Time Between Failures) metric
    - Peak Hour indicator
    - Station Utilization Table (sortable by sessions, energy, revenue, utilization %, uptime %)
  - **Audit Logs Tab**: (Current audit-logs page content)
    - Table with timestamp, user, action, entity type, entity name, changes (diff view)
    - Filter by user, action type, entity type, date range
    - Useful for compliance and debugging

**Common Actions:**
- Change date range for analytics
- Sort utilization table
- Export analytics report (future)
- Search audit logs by user or action
- Filter audit logs by date range

**Default Filters:** Analytics: last 30 days. Audit Logs: last 7 days, all actions.

**Role Visibility:**
- Admin: Full access to both tabs
- Auditor role (future): Full access to Audit Logs, view Analytics
- Finance: View Analytics (revenue data)
- Operator: View Analytics only
- Viewer: No access

**What This Absorbs:**
| Previous Page | How It's Integrated |
|---------------|-------------------|
| `/analytics` (Analytics) | Analytics tab |
| `/audit-logs` (Audit Logs) | Audit Logs tab. Both are historical/retrospective views -- grouping them under "Reports" matches the mental model of "I need to look something up." |

---

#### 3.12 Settings

**URL:** `/settings`
**Icon:** `Settings`
**Purpose:** System-wide configuration and preferences.

**Main Widgets:**
- **Sections** (vertical nav or accordion):
  - **General**: System name, timezone, default locale, date format, currency format
  - **Notifications**: Email notification preferences, alert thresholds (e.g., "alert when station offline > 5 min"), notification channels
  - **OCPP**: Default OCPP settings, heartbeat interval, meter value sample interval, test idTag configuration
  - **Payments**: Payment gateway configuration, wallet settings, refund policies
  - **Security**: Password policy, session timeout, API rate limits, CORS origins
  - **Vehicles** (relocated): Vehicle list management (low frequency for admin users, primarily a driver-app concern)
  - **Feedback** (relocated): View submitted feedback, FAQ management

**Common Actions:**
- Update system configuration
- Configure notification thresholds
- Manage OCPP default settings
- View and respond to user feedback

**Default View:** General section expanded.

**Role Visibility:**
- Admin: Full access to all sections
- Operator: View only (no edit)
- All other roles: No access

**Relocated Items:**
| Previous Page | New Location | Rationale |
|---------------|-------------|-----------|
| `/vehicles` (Vehicles) | Settings > Vehicles section | Vehicle management is low-frequency for admin users. Vehicles are primarily created and managed through the mobile app. Admin access is for edge cases (manual corrections, viewing fleet data). |
| `/feedback` (Feedback) | Settings > Feedback section | Feedback review is periodic, not a daily workflow. If feedback volume grows significantly, it can be promoted back to its own nav item under a "Support" section. |

---

## 4. Navigation Comparison

### Before vs After

| # | Before (Flat) | After (Grouped) | Change |
|---|---------------|-----------------|--------|
| 1 | Dashboard | Dashboard | Unchanged |
| 2 | Stations | Stations (+ Map + Groups) | Expanded scope |
| 3 | Station Map | _(merged into Stations)_ | Removed |
| 4 | Monitoring | Monitoring (+ OCPP) | Expanded scope |
| 5 | Analytics | _(merged into Reports)_ | Moved |
| 6 | Sessions | Sessions | Unchanged |
| 7 | Tariffs | Tariffs | Unchanged |
| 8 | Payments | Payments (+ E-Invoices) | Expanded scope |
| 9 | Faults | Faults & Alerts (+ Alerts) | Merged |
| 10 | Maintenance | Maintenance | Unchanged |
| 11 | Station Groups | _(merged into Stations)_ | Removed |
| 12 | Audit Logs | _(merged into Reports)_ | Moved |
| 13 | E-Invoices | _(merged into Payments)_ | Removed |
| 14 | Vehicles | _(moved to Settings)_ | Demoted |
| 15 | Vouchers | _(merged into Marketing)_ | Merged |
| 16 | Promotions | _(merged into Marketing)_ | Merged |
| 17 | Feedback | _(moved to Settings)_ | Demoted |
| 18 | Mobile Users | _(merged into Users)_ | Merged |
| 19 | OCPP Management | _(merged into Monitoring)_ | Merged |
| 20 | User Management | Users (+ Mobile Users + Roles) | Expanded scope |
| -- | Alerts (footer) | _(merged into Faults & Alerts)_ | Promoted and merged |
| -- | Settings (footer) | Settings | Promoted to main nav |
| -- | -- | Reports (new, Analytics + Audit Logs) | New combined page |
| -- | -- | Marketing (new, Vouchers + Promotions) | New combined page |

**Result:** 19 flat items + 2 footer items --> 12 grouped items in 5 sections.

---

## 5. URL Routing Plan

| New URL | Old URL(s) | Notes |
|---------|-----------|-------|
| `/` | `/` | Dashboard, unchanged |
| `/stations` | `/stations` | Add Map/Groups as tabs/toggles |
| `/stations?view=map` | `/map` | Query param controls view mode |
| `/stations?tab=groups` | `/groups` | Query param controls tab |
| `/stations/[id]` | `/stations/[id]` | Unchanged |
| `/stations/new` | `/stations/new` | Unchanged |
| `/stations/[id]/edit` | `/stations/[id]/edit` | Unchanged |
| `/monitoring` | `/monitoring` | Add OCPP tab |
| `/monitoring?tab=commands` | `/ocpp` | Query param controls tab |
| `/sessions` | `/sessions` | Unchanged |
| `/sessions/[id]` | `/sessions/[id]` | Unchanged |
| `/faults` | `/faults` | Add Alerts tab |
| `/faults?tab=alerts` | `/alerts` | Query param controls tab |
| `/faults/[id]` | `/faults/[id]` | Unchanged |
| `/maintenance` | `/maintenance` | Unchanged |
| `/tariffs` | `/tariffs` | Unchanged |
| `/payments` | `/payments` | Add E-Invoices tab |
| `/payments?tab=e-invoices` | `/e-invoices` | Query param controls tab |
| `/payments/[id]` | `/payments/[id]` | Unchanged |
| `/marketing` | -- | New page |
| `/marketing?tab=vouchers` | `/vouchers` | Default tab |
| `/marketing?tab=promotions` | `/promotions` | Query param controls tab |
| `/users` | `/user-management` | Renamed and expanded |
| `/users?tab=mobile` | `/mobile-users` | Query param controls tab |
| `/users?tab=roles` | -- | New tab (was inline in user-management) |
| `/reports` | -- | New page |
| `/reports?tab=analytics` | `/analytics` | Default tab |
| `/reports?tab=audit-logs` | `/audit-logs` | Query param controls tab |
| `/settings` | `/settings` | Unchanged, expanded sections |
| `/settings?section=vehicles` | `/vehicles` | Relocated |
| `/settings?section=feedback` | `/feedback` | Relocated |

**Redirect strategy:** Old URLs should redirect (308 Permanent Redirect) to new URLs for bookmarks and shared links. Implement via Next.js `next.config.ts` redirects.

---

## 6. Sidebar Implementation

### Data Structure

```typescript
interface NavSection {
  label: string;
  items: NavItem[];
}

interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  badge?: number;        // for unread counts
  roles?: string[];      // role-based visibility
  children?: NavItem[];  // future: expandable sub-nav
}

const navigation: NavSection[] = [
  {
    label: "OPERATIONS",
    items: [
      { href: "/", label: "Dashboard", icon: LayoutDashboard },
      { href: "/stations", label: "Stations", icon: MapPin },
      { href: "/monitoring", label: "Monitoring", icon: Activity },
      { href: "/sessions", label: "Sessions", icon: Zap },
    ],
  },
  {
    label: "INCIDENTS",
    items: [
      { href: "/faults", label: "Faults & Alerts", icon: AlertTriangle, badge: unreadAlerts },
      { href: "/maintenance", label: "Maintenance", icon: Wrench },
    ],
  },
  {
    label: "BUSINESS",
    items: [
      { href: "/tariffs", label: "Tariffs", icon: DollarSign },
      { href: "/payments", label: "Payments", icon: Receipt },
      { href: "/marketing", label: "Marketing", icon: Megaphone },
    ],
  },
  {
    label: "USERS",
    items: [
      { href: "/users", label: "Users", icon: Users, roles: ["admin"] },
    ],
  },
  {
    label: "SYSTEM",
    items: [
      { href: "/reports", label: "Reports", icon: BarChart3 },
      { href: "/settings", label: "Settings", icon: Settings, roles: ["admin"] },
    ],
  },
];
```

### Section Rendering

```
OPERATIONS               <-- text-[10px] font-semibold uppercase tracking-wider text-muted-foreground px-3 pt-4 pb-1
  Dashboard              <-- nav item (same as current)
  Stations
  Monitoring
  Sessions

INCIDENTS                <-- section label with top border separator
  Faults & Alerts [3]    <-- badge shows unread alert count
  Maintenance
```

Section labels are only visible when the sidebar is expanded. In collapsed mode, a thin horizontal divider replaces the section label.

### Active State Logic

Current: `pathname === item.href || pathname.startsWith(item.href + "/")`

Updated (for merged pages): The active state should also match query param-based tabs. For example, `/faults?tab=alerts` should highlight "Faults & Alerts". Since all merged content lives under the same base path, the current logic already handles this correctly.

---

## 7. Role-Based Visibility Matrix

| Page | Admin | Operator | Viewer | Finance | Auditor |
|------|-------|----------|--------|---------|---------|
| Dashboard | Full | Full | Read | Revenue KPIs only | -- |
| Stations | Full CRUD | View + Enable/Disable | View | -- | -- |
| Monitoring | Full + All OCPP | View + Limited OCPP | View | -- | -- |
| Sessions | Full | View | View | View (with payments) | -- |
| Faults & Alerts | Full | View + Update Status | View | -- | -- |
| Maintenance | Full CRUD | View + Update Status | View | -- | -- |
| Tariffs | Full CRUD | View | View | View + Draft | -- |
| Payments | Full + Refund | View Transactions | -- | Full + Export | -- |
| Marketing | Full CRUD | View | View | View | -- |
| Users | Full CRUD | View | -- | -- | -- |
| Reports | Full | Analytics Only | -- | Analytics Only | Full |
| Settings | Full | View | -- | -- | -- |

Items hidden from navigation when the user's role has no access (e.g., Viewer does not see Users or Settings).

---

## 8. Migration Plan

### Phase 1: Sidebar Restructuring (2-3 days)

1. Update `sidebar.tsx`:
   - Replace flat `menuItems` array with `NavSection[]` structure
   - Add section label rendering with dividers
   - Move alerts badge to "Faults & Alerts" nav item
   - Remove alerts link from sidebar footer
   - Move Settings from footer to SYSTEM section
2. Add redirects in `next.config.ts` for old URLs
3. No page content changes -- just navigation structure

### Phase 2: Page Merges (1-2 weeks)

Merge in order of operational impact:

1. **Stations + Map + Groups** -- Add view toggle and groups tab to stations page
2. **Monitoring + OCPP** -- Add commands tab to monitoring page
3. **Faults + Alerts** -- Add alerts tab to faults page
4. **Payments + E-Invoices** -- Add e-invoices tab to payments page
5. **User Management + Mobile Users** -- Add mobile users tab and roles tab
6. **Analytics + Audit Logs** -- Create reports page with two tabs
7. **Vouchers + Promotions** -- Create marketing page with two tabs
8. **Vehicles + Feedback** -- Move to settings sub-sections

### Phase 3: Polish (3-5 days)

1. Add role-based visibility filtering to navigation
2. Implement breadcrumbs on all detail/sub-pages
3. Update header: remove duplicate alerts bell, implement global search
4. Test all redirects from old URLs
5. Update any documentation or bookmarks referencing old URLs

---

## 9. Success Criteria

| Metric | Before | After |
|--------|--------|-------|
| Top-level nav items | 19 + 2 footer | 12 in 5 sections |
| Clicks to reach OCPP commands | 1 (but buried at position 19) | 2 (Monitoring > Commands tab) |
| Clicks to reach Station Map | 1 (separate page, position 3) | 1 (Stations > Map toggle) |
| Duplicate UI elements | 2 (alerts bell in header + sidebar) | 0 |
| Pages with no nav context (Settings) | 1 | 0 |
| Distinct route files | 29 | 21 (8 merged) |
| Cognitive load (items to scan) | 19 | 4-5 per section (progressive disclosure) |
