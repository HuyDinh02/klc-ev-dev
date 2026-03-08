# K-Charge EV Charging Admin Portal -- Redesign Specification

**Version:** 1.0
**Date:** 2026-03-08
**Status:** Draft
**Author:** Design Team / EmeSoft
**Client:** KLC

---

## Table of Contents

1. [Design System Foundation](#1-design-system-foundation)
2. [Page 1: Login](#2-page-1-login)
3. [Page 2: Main Dashboard](#3-page-2-main-dashboard)
4. [Page 3: Real-time Monitoring](#4-page-3-real-time-monitoring)
5. [Page 4: Station List](#5-page-4-station-list)
6. [Page 5: Station Detail](#6-page-5-station-detail)
7. [Page 6: Session List](#7-page-6-session-list)
8. [Page 7: Session Detail](#8-page-7-session-detail)
9. [Page 8: Faults & Alerts](#9-page-8-faults--alerts)
10. [Page 9: Pricing Configuration (Tariffs)](#10-page-9-pricing-configuration-tariffs)
11. [Page 10: Payments & Transactions](#11-page-10-payments--transactions)
12. [Page 11: User Management](#12-page-11-user-management)
13. [Page 12: Audit Logs](#13-page-12-audit-logs)

---

## 1. Design System Foundation

### 1.1 Brand Colors

| Token                  | Hex       | Usage                                            |
|------------------------|-----------|--------------------------------------------------|
| `brand-green`          | `#28A649` | Primary actions, success states, energy/charging  |
| `brand-green-dark`     | `#1E7D37` | Hover state for primary buttons                  |
| `brand-green-light`    | `#E8F5EC` | Light green backgrounds, success tints            |
| `brand-orange`         | `#FAA623` | Secondary accent, warnings, attention items       |
| `brand-orange-dark`    | `#D4891A` | Hover state for secondary buttons                |
| `brand-orange-light`   | `#FFF5E5` | Light orange backgrounds, warning tints           |
| `chart-blue`           | `#3B82F6` | Session/charging data in charts                   |
| `chart-blue-light`     | `#EFF6FF` | Blue tint backgrounds                             |
| `critical-red`         | `#EF4444` | Critical alerts, errors, destructive actions      |
| `critical-red-light`   | `#FEF2F2` | Red tint backgrounds                              |
| `neutral-900`          | `#111827` | Primary text                                      |
| `neutral-600`          | `#4B5563` | Secondary text                                    |
| `neutral-400`          | `#9CA3AF` | Placeholder text, disabled states                 |
| `neutral-200`          | `#E5E7EB` | Borders, dividers                                 |
| `neutral-100`          | `#F3F4F6` | Page backgrounds, card hover                      |
| `neutral-50`           | `#F9FAFB` | Subtle backgrounds                                |
| `white`                | `#FFFFFF` | Card surfaces, inputs                             |

### 1.2 Typography

| Element          | Font   | Weight   | Size  | Line Height | Tracking      |
|------------------|--------|----------|-------|-------------|---------------|
| Page title       | Inter  | 700      | 24px  | 32px        | -0.025em      |
| Section title    | Inter  | 600      | 18px  | 28px        | -0.015em      |
| Card title       | Inter  | 600      | 16px  | 24px        | -0.01em       |
| Body             | Inter  | 400      | 14px  | 20px        | 0             |
| Body small       | Inter  | 400      | 13px  | 18px        | 0             |
| Caption          | Inter  | 500      | 12px  | 16px        | 0.02em        |
| Label (uppercase)| Inter  | 600      | 11px  | 16px        | 0.06em        |
| KPI number       | Inter  | 700      | 28px  | 36px        | -0.02em       |
| Mono (codes/IDs) | JetBrains Mono | 400 | 13px | 20px     | 0             |

All numeric data in tables and KPI cards must use `font-variant-numeric: tabular-nums` to ensure columns align.

### 1.3 Spacing Scale

Based on a 4px grid: `4, 8, 12, 16, 20, 24, 32, 40, 48, 64`.

- Card padding: 24px
- Card gap (grid): 16px
- Section gap: 24px
- Page horizontal padding: 24px (desktop), 16px (mobile)
- Page vertical padding: 24px

### 1.4 Elevation & Surface

| Level   | Shadow                                          | Usage                      |
|---------|-------------------------------------------------|----------------------------|
| Level 0 | none                                            | Page background            |
| Level 1 | `0 1px 3px rgba(0,0,0,0.08)`                   | Cards, panels              |
| Level 2 | `0 4px 12px rgba(0,0,0,0.1)`                   | Dropdowns, popovers        |
| Level 3 | `0 8px 24px rgba(0,0,0,0.12)`                  | Modals, dialogs            |
| Level 4 | `0 16px 48px rgba(0,0,0,0.16)`                 | Toasts, floating panels    |

Cards: `border: 1px solid neutral-200`, `border-radius: 12px`, `background: white`.

### 1.5 Component Tokens

**Buttons:**

| Variant      | Background         | Text             | Border              | Hover Background     |
|--------------|--------------------|------------------|---------------------|--------------------- |
| Primary      | `brand-green`      | `white`          | none                | `brand-green-dark`   |
| Secondary    | `white`            | `neutral-900`    | `1px neutral-200`   | `neutral-100`        |
| Destructive  | `critical-red`     | `white`          | none                | `#DC2626`            |
| Ghost        | `transparent`      | `neutral-600`    | none                | `neutral-100`        |
| Orange       | `brand-orange`     | `white`          | none                | `brand-orange-dark`  |

Button sizes: `sm` (32px height, 12px padding-x), `md` (40px height, 16px padding-x), `lg` (48px height, 24px padding-x).
Border radius: 8px for all buttons.

**Badges / Status Pills:**

| Variant      | Background              | Text              | Border     |
|--------------|-------------------------|--------------------|-----------|
| Success      | `brand-green-light`     | `#166534`          | none      |
| Warning      | `brand-orange-light`    | `#92400E`          | none      |
| Error        | `critical-red-light`    | `#991B1B`          | none      |
| Info         | `chart-blue-light`      | `#1E40AF`          | none      |
| Neutral      | `neutral-100`           | `neutral-600`      | none      |
| Outline      | `transparent`           | `neutral-600`      | `neutral-200` |

Badge shape: `border-radius: 9999px` (full rounded), height 22px, horizontal padding 10px, font-size 12px, font-weight 500.

**Inputs:**

- Height: 40px
- Border: `1px solid neutral-200`
- Border radius: 8px
- Focus ring: `0 0 0 2px brand-green` with 0.2 opacity
- Placeholder color: `neutral-400`
- Label: 13px, font-weight 500, `neutral-700`, margin-bottom 6px

### 1.6 Chart Color Palette

| Index | Color     | Semantic Use                |
|-------|----------|-----------------------------|
| 0     | `#28A649` | Energy, availability, primary metric |
| 1     | `#3B82F6` | Sessions, charging, secondary metric |
| 2     | `#FAA623` | Revenue, warnings                    |
| 3     | `#8B5CF6` | Tertiary / time-related              |
| 4     | `#EF4444` | Faults, failures                     |
| 5     | `#6B7280` | Offline, inactive                    |

Charts should use 2px stroke, 4px radius dots on hover, subtle grid lines (`neutral-200` with dashed stroke).

### 1.7 Breakpoints

| Name    | Width     | Columns | Sidebar   |
|---------|-----------|---------|-----------|
| Mobile  | < 768px   | 1       | Hidden (hamburger) |
| Tablet  | 768-1024px| 2       | Collapsed (icons only, 64px) |
| Desktop | > 1024px  | 3-4     | Expanded (256px) |

### 1.8 Motion

- Default transition: `150ms ease-out` for color, background, border, opacity changes.
- Layout transitions (sidebar collapse, panel open): `300ms cubic-bezier(0.4, 0, 0.2, 1)`.
- Skeleton loading: Shimmer gradient animation, `1.5s infinite`.
- Status dot pulse: `2s cubic-bezier(0.4, 0, 0.6, 1) infinite` for live indicators.

### 1.9 Icon System

Use Lucide React icons exclusively. Icon sizes: 16px (inline/badges), 20px (navigation/card headers), 24px (KPI cards), 40px (empty states).

---

## 2. Page 1: Login

### UX Goal
Trust, brand recognition, and fast access. The user should feel confident they are on the official K-Charge platform and be able to sign in within seconds.

### Key Data to Surface
- Brand identity (logo, name)
- Authentication form
- Error feedback
- Demo credentials (development environment only)

### Layout Structure

```
+------------------------------------------------------------------+
|                                                                    |
|          Subtle green-to-white gradient background                 |
|          (linear-gradient: brand-green at 5% opacity -> white)     |
|                                                                    |
|                   +----------------------------+                   |
|                   |                            |                   |
|                   |   [K-Charge Logo - full]   |                   |
|                   |   "K-Charge"               |                   |
|                   |   "EV Charging Management" |                   |
|                   |                            |                   |
|                   |   +----- Error Alert -----+|                   |
|                   |   |  (red, dismissible)   ||                   |
|                   |   +-----------------------+|                   |
|                   |                            |                   |
|                   |   Email or Username         |                   |
|                   |   [___________________]    |                   |
|                   |                            |                   |
|                   |   Password                 |                   |
|                   |   [_________________ eye]  |                   |
|                   |                            |                   |
|                   |   [===== Sign In =====]    |                   |
|                   |   (brand-green, full-width) |                   |
|                   |                            |                   |
|                   +----------------------------+                   |
|                                                                    |
|                   +------- Dev Only ----------+                    |
|                   |  Demo Credentials:        |                    |
|                   |  admin / Admin@123        |                    |
|                   |  operator / Admin@123     |                    |
|                   |  viewer / Admin@123       |                    |
|                   +---------------------------+                    |
|                                                                    |
+------------------------------------------------------------------+
```

### Component List

| Component              | Spec                                                                                     |
|------------------------|------------------------------------------------------------------------------------------|
| Background             | `linear-gradient(135deg, rgba(40,166,73,0.06) 0%, rgba(255,255,255,1) 60%)` full viewport |
| Logo                   | K-Charge logo SVG, max-height 48px, centered above card. Below it: "K-Charge" in 24px bold + "EV Charging Management" in 14px neutral-600 |
| Card                   | `max-width: 420px`, centered, Level 1 shadow, 32px padding, 12px border-radius           |
| Email input            | Standard text input, label "Email or Username", autocomplete="username"                  |
| Password input         | Password input with visibility toggle icon (Eye/EyeOff) at right end, autocomplete="current-password" |
| Sign In button         | Primary button (brand-green), full width, 44px height, 16px font, 700 weight             |
| Error alert            | Red-tinted container (`critical-red-light` bg), red icon (AlertCircle), 14px text. Appears between logo area and form |
| Demo credentials       | Appears only in development. Muted card below main card, 13px text, `neutral-100` background, 8px border-radius. Clickable entries that auto-fill form |

### Brand Expression
- Logo is the dominant visual element, placed centrally with generous spacing.
- The Sign In button uses `brand-green` to anchor the primary action to the brand.
- The gradient background subtly reinforces the green brand without overwhelming.
- Card surface is clean white with a soft shadow for a trustworthy, professional feel.

### States

| State    | Behavior                                                                            |
|----------|-------------------------------------------------------------------------------------|
| Default  | Empty form, Sign In button enabled, no error                                        |
| Loading  | Sign In button shows spinner icon + "Signing in..." text, inputs disabled           |
| Error    | Red alert appears below form header with error message. Inputs remain filled. Button re-enables |
| Success  | Brief checkmark animation on button, then redirect to dashboard/return URL          |

### Priority Actions
1. Sign In (primary)
2. Toggle password visibility (secondary)
3. Click demo credential to auto-fill (dev only)

### Mobile / Tablet Considerations
- Card becomes full-width with 16px horizontal margin.
- Logo scales to max-height 36px.
- Inputs and button remain full-width within card.
- Demo credentials section remains visible in dev.
- Minimum touch target: 44px for all interactive elements.

### Accessibility
- All inputs have associated `<label>` elements.
- Error messages use `role="alert"` and `aria-live="assertive"`.
- Password toggle has `aria-label="Show password"` / `"Hide password"`.
- Focus order: email -> password -> toggle -> sign in.

---

## 3. Page 2: Main Dashboard

### UX Goal
Instant operational awareness. Within 3 seconds of page load, the operator should know: how many sessions are active, whether the network is healthy, how much energy and revenue were generated today, and whether anything needs attention.

### Key Data to Surface
- Active session count
- Network availability percentage
- Energy delivered today (kWh)
- Revenue today (VND)
- Station status distribution (donut chart)
- Energy delivered trend (7-day area chart)
- Revenue trend (14-day bar chart)
- Recent alerts (5 most recent)

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Dashboard" + "Real-time overview of your          |
|  charging network"]                                               |
+------------------------------------------------------------------+
| Row 1: KPI Cards (4 columns on desktop, 2 on tablet, 1 mobile)  |
|                                                                    |
| +-- KPI 1 ----------+ +-- KPI 2 ----------+ +-- KPI 3 ----------+ +-- KPI 4 ----------+
| | [Zap icon]         | | [Shield icon]      | | [Battery icon]     | | [Banknote icon]    |
| |                    | |                    | |                    | |                    |
| |  12                | |  94.5%             | |  1,247.8 kWh       | |  12.450.000d       |
| |  ACTIVE SESSIONS   | |  AVAILABILITY      | |  ENERGY TODAY      | |  REVENUE TODAY     |
| |  +8% vs yesterday  | |  -0.5% vs yesterday| |  +12% vs yesterday | |  +5% vs yesterday  |
| +--------------------+ +--------------------+ +--------------------+ +--------------------+
|                                                                    |
| Row 2: Charts (2 columns on desktop, stacked on tablet/mobile)   |
|                                                                    |
| +-- Station Status Donut --------+ +-- Energy Area Chart (7d) ----+
| |                                | |                              |
| |     [Donut Chart]              | |   [Area Chart]               |
| |   Available: 45                | |   Brand-green fill           |
| |   Charging: 12                 | |   X: Mon-Sun                 |
| |   Offline: 3                   | |   Y: kWh                     |
| |   Faulted: 2                   | |                              |
| +--------------------------------+ +------------------------------+
|                                                                    |
| Row 3: Charts (2 columns on desktop, stacked on tablet/mobile)   |
|                                                                    |
| +-- Revenue Bar Chart (14d) -----+ +-- Recent Alerts (5 items) ---+
| |                                | |                              |
| |   [Bar Chart]                  | |  [!] Station A went offline  |
| |   Brand-orange bars            | |      2 min ago               |
| |   X: date labels               | |  [!] Connector fault at B    |
| |   Y: VND (millions)            | |      15 min ago              |
| |                                | |  [i] Low utilization at C    |
| |                                | |      1 hour ago              |
| |                                | |                              |
| |                                | |  [View All Alerts ->]        |
| +--------------------------------+ +------------------------------+
```

### Component List

| Component                 | Spec                                                                                     |
|---------------------------|------------------------------------------------------------------------------------------|
| Page header               | Title "Dashboard" (24px bold) + description (14px neutral-600)                           |
| KPI card                  | Level 1 card, 24px padding. Top-left: icon in 40px circle (brand-green-light bg for sessions/energy, brand-orange-light bg for revenue, chart-blue-light bg for availability). Right area: large number (28px bold tabular-nums), label below (11px uppercase 600 weight neutral-600), trend arrow below label (green up-arrow or red down-arrow, 12px, with percentage and "vs yesterday") |
| Station Status Donut      | Recharts PieChart, inner radius 60%, outer radius 80%. Colors: brand-green (Available), chart-blue (Charging), neutral-400 (Offline), critical-red (Faulted). Center label: total station count. Legend below chart as colored pills with count |
| Energy Area Chart (7d)    | Recharts AreaChart. Fill: brand-green at 15% opacity, stroke: brand-green 2px. X-axis: day labels (Mon, Tue...). Y-axis: kWh. Smooth monotone curve. Tooltip on hover showing exact kWh per day |
| Revenue Bar Chart (14d)   | Recharts BarChart. Bars: brand-orange, 8px border-radius top. X-axis: date labels (dd/MM). Y-axis: VND in millions (suffix "M"). Tooltip shows formatted VND |
| Recent Alerts list        | List of 5 items, each row: severity icon (left, colored by severity), content (station name + message in 14px, timestamp in 12px neutral-400), all within a subtle hover background. "View All" link at bottom in brand-green text |

### KPI Card Design Detail

```
+-----------------------------------------------+
|  [Icon circle, 40px]           [Trend area]   |
|  brand-green-light bg          (right-aligned) |
|  20px icon inside                              |
|                                                |
|  1,247.8 kWh                                  |
|  28px / Inter 700 / tabular-nums              |
|                                                |
|  ENERGY TODAY                                  |
|  11px / Inter 600 / uppercase / neutral-600   |
|                                                |
|  [ArrowUp icon] +12.3% vs yesterday           |
|  12px / brand-green (positive)                 |
|  12px / critical-red (negative)                |
+-----------------------------------------------+
```

### Actions

| Trigger                  | Action                                            |
|--------------------------|---------------------------------------------------|
| Click KPI "Active Sessions" | Navigate to `/sessions?status=active`           |
| Click KPI "Availability" | Navigate to `/monitoring`                         |
| Click KPI "Energy Today" | Navigate to `/analytics`                          |
| Click KPI "Revenue Today"| Navigate to `/payments`                           |
| Click alert row          | Navigate to `/faults/{faultId}` or `/alerts`      |
| Click "View All"         | Navigate to `/alerts`                             |
| Click donut segment      | Navigate to `/stations?status={segment}`          |

### Loading State
- 4 skeleton KPI cards: rounded rectangle pulsing (shimmer) matching card dimensions.
- 2 skeleton chart areas: rounded rectangle with shimmer, matching chart container height (300px).
- 1 skeleton alert list: 5 rows of horizontal lines with shimmer.

### Empty State
When no stations are configured:
- Single centered card with illustration (empty network icon, 64px neutral-300).
- Heading: "No data yet"
- Subtext: "Add your first charging station to see dashboard metrics."
- CTA button: "Add Station" (brand-green, links to `/stations/new`).

### Error State
- If API fails, each section independently shows: "Failed to load data" with a "Retry" button.
- KPI cards show "--" for the number with neutral-400 color.

### Auto-refresh
- TanStack Query `refetchInterval: 30000` (30 seconds) on all dashboard queries.
- No full-page reload; only data refreshes with smooth number transitions.

### Mobile / Tablet Considerations
- KPI cards: 2x2 grid on tablet, single column on mobile.
- Charts: Full-width stacked on tablet and mobile.
- Recent alerts: Full-width card, last in stack order on mobile.
- Donut chart center label font scales down to 20px on mobile.

---

## 4. Page 3: Real-time Monitoring

### UX Goal
Live operational control and instant status awareness. The operator should see the real-time state of every station and be able to issue OCPP commands without leaving the page.

### Key Data to Surface
- SignalR connection status
- Station count and online/offline breakdown
- Per-station connector status array
- Active session count per station
- Last heartbeat time per station
- OCPP command panel for selected station

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Real-time Monitoring"]                             |
+------------------------------------------------------------------+
| Top Bar (sticky)                                                  |
| +--------------------------------------------------------------+ |
| |  [Green Dot Pulsing] Live   |   62 stations   |   Last       | |
| |  (or "Connecting..." or     |   58 online      |   updated    | |
| |   "Polling (10s)")          |   4 offline      |   3s ago     | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Main Content                                                       |
| +------------------------------------------+ +------------------+ |
| |  Station Grid (3 cols desktop, 2 tablet) | | OCPP Command     | |
| |                                          | | Panel (right)    | |
| |  +-- Station Card --+ +-- Station Card --| |                  | |
| |  | Station Alpha    | | Station Beta    || | Selected:        | |
| |  | [*] [*] [o] [x]  | | [*] [*]         || | Station Alpha    | |
| |  | 2 active sessions | | 1 active session || |                  | |
| |  | Last HB: 10s ago  | | Last HB: 5s ago || | [Remote Start]   | |
| |  +------------------+ +------------------|| | [Remote Stop]    | |
| |                                          | | [Reset]          | |
| |  +-- Station Card --+ +-- Station Card --| | [Unlock]         | |
| |  | Station Gamma    | | Station Delta   || | [Change Avail.]  | |
| |  | [*] [x]          | | [*] [*] [*]     || | [Trigger Msg]    | |
| |  | 0 active sessions | | 3 active sessions|| | [Get Config]     | |
| |  | Last HB: 2m ago   | | Last HB: 8s ago || | ...              | |
| |  +------------------+ +------------------|| |                  | |
| +------------------------------------------+ +------------------+ |
```

### Component List

| Component                     | Spec                                                                                      |
|-------------------------------|-------------------------------------------------------------------------------------------|
| Connection status indicator   | Green filled circle (8px) with CSS pulse animation when connected. Text "Live" in brand-green 600 weight. "Connecting..." in brand-orange when reconnecting. "Polling (10s)" with gray WifiOff icon when disconnected |
| Station count bar             | 3 inline stats: total stations, online (brand-green text), offline (neutral-400 text)     |
| "Last updated" timer          | Relative time "3s ago", "1m ago" etc., updates every second via local timer               |
| Station card                  | Level 1 card, 20px padding. Top row: station name (16px 600 weight, truncate) + status badge (right). Middle: connector status pills (horizontal row). Bottom row: active session count + last heartbeat. Border-left: 3px solid colored by overall station status (green=online, gray=offline, red=faulted). Hover: `neutral-50` bg. Click: selects station |
| Connector status pills        | Horizontal row of small pills (28px wide, 22px tall, 8px border-radius). Colors: brand-green=Available, chart-blue=Charging, critical-red=Faulted, neutral-300=Offline/Unavailable. Tooltip on hover showing connector number and type |
| OCPP Command panel            | Right sidebar (320px desktop) or bottom sheet (mobile). Card with station name header, list of command buttons. Each button: secondary style, full-width, icon + label. Disabled when no station selected |

### Connector Status Visualization Detail

```
Station Alpha
+---------+---------+---------+---------+
|   #1    |   #2    |   #3    |   #4    |
|  Avail  | Charg.  | Avail   | Faulted |
|  (green)|  (blue) | (green) |  (red)  |
+---------+---------+---------+---------+
```

Each pill:
- Width: proportional within row (flex: 1, max 60px)
- Height: 28px
- Border-radius: 6px
- Font: 10px uppercase 600 weight, white text for Charging/Faulted, dark text for Available
- Transition: `background-color 300ms ease-out` for smooth status change animation

### Live Update Behavior
- SignalR events (`StationStatusChanged`, `ConnectorStatusChanged`, `SessionUpdated`) trigger TanStack Query cache invalidation.
- When a connector status changes, the pill smoothly transitions color over 300ms.
- New alerts from `AlertCreated` events show a toast notification (top-right, auto-dismiss 5s).
- Active session counter updates in real-time without page refresh.

### Actions

| Trigger                     | Action                                               |
|-----------------------------|------------------------------------------------------|
| Click station card          | Select station, open/populate OCPP command panel      |
| Click connector pill        | Show session info popover (if charging)               |
| Click "Remote Start"        | Open dialog: select connector, enter ID tag, confirm  |
| Click "Remote Stop"         | Open dialog: select transaction, confirm              |
| Click "Reset"               | Confirmation dialog: soft/hard reset options          |
| Click "Unlock Connector"    | Select connector number, confirm                      |

### Loading State
- Top bar: skeleton rectangles for counts.
- Station grid: 6-9 skeleton cards (matching card dimensions) with shimmer animation.
- OCPP panel: "Select a station" placeholder.

### Empty State
- If no stations: centered illustration, "No stations configured", link to add station.
- If no station selected for OCPP panel: "Select a station from the grid to send commands."

### Error State
- SignalR connection failure: top bar shows orange "Reconnecting..." indicator.
- Fallback polling interval decreases: 60s (connected) -> 10s (disconnected).

### Mobile / Tablet Considerations
- Station grid: single column on mobile, 2 columns on tablet.
- OCPP command panel: hidden by default on mobile, accessible via "Commands" floating action button (FAB) at bottom-right. Opens as bottom sheet.
- Top bar: wraps to 2 lines on mobile; connection indicator + counts on line 1, last updated on line 2.
- Cards stack vertically; connector pills shrink to colored dots (12px circles) on mobile.

---

## 5. Page 4: Station List

### UX Goal
Find and manage stations efficiently. Support multiple view modes (list, map, grid) to accommodate different mental models. Enable quick status filtering and bulk awareness.

### Key Data to Surface
- Station name, code, address
- Station status
- Connector count and status array
- Active session count
- Station group assignment
- Last heartbeat

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Stations" + "Manage charging stations"]           |
+------------------------------------------------------------------+
| Filter Bar                                                        |
| +--------------------------------------------------------------+ |
| | [Search icon] Search stations...  | Status: [All v] |         | |
| |                                   | Group: [All v]  |         | |
| |                                   | View: [List|Map|Grid]     | |
| |                                   |        + [Add Station]    | |
| +--------------------------------------------------------------+ |
|                                                                    |
| === LIST VIEW ===                                                  |
| +--------------------------------------------------------------+ |
| | Name          | Code    | Address    | Status | Connectors   | |
| |               |         |            |        | [*][*][o][x] | |
| | Station Alpha | STA-001 | 123 Tran...| Online | [*][*][ ]    | |
| | Station Beta  | STA-002 | 456 Nguy...| Offline| [x][x]       | |
| | ...           |         |            |        |              | |
| +--------------------------------------------------------------+ |
|                                                                    |
| === MAP VIEW ===                                                   |
| +---------------------------+ +--------------------------------+ |
| | Station List (40%)        | | Map (60%)                      | |
| | (condensed rows)          | | [Leaflet map with markers]     | |
| |                           | | Status-colored markers          | |
| |                           | | Click marker -> highlight row   | |
| +---------------------------+ +--------------------------------+ |
|                                                                    |
| === GRID VIEW ===                                                  |
| +-- Card --+ +-- Card --+ +-- Card --+ +-- Card --+             |
| | [Photo]  | | [Photo]  | | [Photo]  | | [Photo]  |             |
| | Name     | | Name     | | Name     | | Name     |             |
| | Status   | | Status   | | Status   | | Status   |             |
| | 3 conn.  | | 2 conn.  | | 4 conn.  | | 1 conn.  |             |
| +----------+ +----------+ +----------+ +----------+             |
```

### Component List

| Component                  | Spec                                                                                       |
|----------------------------|--------------------------------------------------------------------------------------------|
| Search input               | Left-aligned, 320px max-width, Search icon (16px, neutral-400) inside left padding. Debounced 300ms |
| Status filter              | Dropdown or chip group. Options: All, Available (green), Occupied (blue), Offline (gray), Faulted (red), Disabled (neutral). Each option shows colored dot + label |
| Group filter               | Dropdown populated from station groups API. "All Groups" default                           |
| View toggle                | 3-button segmented control (List/Map/Grid). Active button: brand-green background, white text. Inactive: neutral-100 bg, neutral-600 text |
| Add Station button         | Primary button (brand-green), "Add Station" with Plus icon                                 |
| Table (list view)          | Full-width table. Header: neutral-50 bg, 13px 600 weight. Rows: 48px height, border-bottom neutral-200, hover neutral-50. Alternating subtle row tinting optional |
| Station status badge       | Colored dot (8px circle) + label text. Available=brand-green, Occupied=chart-blue, Offline=neutral-400, Faulted=critical-red, Disabled=neutral-300 with strikethrough style |
| Connector array (table)    | Row of small colored circles (10px diameter, 4px gap). Green=Available, Blue=Charging, Red=Faulted, Gray=Offline. Tooltip on hover: "Connector #N: Type2, Available" |
| Row actions                | Icon buttons: Eye (view), Edit (edit), Power/PowerOff (toggle enable). Compact, 32px touch target |
| Map view (split pane)      | Left: condensed list (name + status only, 36px rows). Right: Leaflet map with status-colored circle markers. Green=Available, Blue=Occupied, Red=Faulted, Gray=Offline. Click marker highlights list row. Click list row pans map. Bidirectional sync |
| Grid view card             | Level 1 card, 200px min-width. Photo placeholder (160px height, neutral-100 bg, MapPin icon centered). Below: station name (14px 600 weight), status badge, connector summary ("3 connectors, 2 available") |

### Pagination
- Cursor-based, 20 items per page.
- Footer: "Showing 1-20 of 62 stations" + Previous/Next buttons.
- Previous button: disabled on first page.
- Next button: disabled when fewer items than page size returned.

### Actions

| Trigger                    | Action                                          |
|----------------------------|-------------------------------------------------|
| Click row / card           | Navigate to `/stations/{id}`                    |
| Click "View" icon          | Navigate to `/stations/{id}`                    |
| Click "Edit" icon          | Navigate to `/stations/{id}/edit`               |
| Click Enable/Disable icon  | Inline mutation, optimistic update, toast        |
| Click "Add Station"        | Navigate to `/stations/new`                     |
| Change view toggle         | Switch view mode, persist in localStorage        |
| Type in search             | Debounce 300ms, reset cursor, refetch            |
| Change status/group filter | Reset cursor, refetch                            |

### Loading State
- List view: 10 skeleton table rows (shimmer rectangles matching column widths).
- Grid view: 8 skeleton cards (shimmer rectangle for photo area + 2 text lines).
- Map view: skeleton for list side + map shows loading overlay.

### Empty State
- Center of content area: MapPin icon (48px, neutral-300), "No stations found" heading, contextual subtext.
- If search active: "No stations match your search. Try a different term."
- If no stations at all: "Get started by adding your first charging station." + "Add Station" CTA button.

### Error State
- Red banner at top of content: "Failed to load stations" + Retry button.

### Mobile / Tablet Considerations
- Default view: Grid on mobile (cards are more touch-friendly).
- Filter bar wraps: search full-width on first line, filters and view toggle on second line.
- Map view: full-screen map with floating list toggle button.
- Table view: horizontal scroll with sticky first column (station name).
- Add Station button: FAB on mobile (bottom-right, 56px circle, brand-green).

---

## 6. Page 5: Station Detail

### UX Goal
Complete station information and control in one view. The operator should be able to see everything about a station, manage its connectors, review recent sessions and faults, and issue commands -- all without navigating away.

### Key Data to Surface
- Station identity (name, code, address, coordinates, group, tariff)
- Station status and enable/disable state
- Technical details (vendor, model, serial, firmware, last heartbeat)
- Connector list with types, power, and real-time status
- Recent sessions
- Recent faults
- Usage statistics (total sessions, energy, revenue, uptime)

### Layout Structure

```
+------------------------------------------------------------------+
| [Back to Stations]                                                |
+------------------------------------------------------------------+
| Header                                                            |
| +--------------------------------------------------------------+ |
| | Station Alpha                [Available]   [Edit] [Disable]  | |
| | STA-001 | 123 Tran Hung Dao, Q1, HCMC     [Decommission]    | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Tab Bar                                                            |
| [Overview]  [Connectors]  [Sessions]  [Faults]  [Maintenance]    |
|                                                                    |
| === OVERVIEW TAB ===                                               |
| +-- Station Info Card --------+ +-- Stats Card ----------------+ |
| | Name: Station Alpha         | | Total Sessions: 1,234        | |
| | Address: 123 Tran Hung Dao  | | Energy Delivered: 45,678 kWh | |
| | Coordinates: 10.762, 106.66 | | Revenue: 450.000.000d        | |
| | Group: HCM City Center      | | Uptime: 99.2%                | |
| | Tariff: Standard Rate        | | Avg Session Duration: 45m    | |
| | Vendor: ABB                  | |                              | |
| | Model: Terra 54              | |                              | |
| | Serial: ABC123456            | |                              | |
| | Firmware: v2.1.0             | |                              | |
| | Last Heartbeat: 10s ago      | |                              | |
| +------------------------------+ +------------------------------+ |
|                                                                    |
| === CONNECTORS TAB ===                                             |
| +-- Connector Card --+ +-- Connector Card --+ +-- Conn. Card --+ |
| | #1 - CCS2         | | #2 - Type 2        | | #3 - CHAdeMO    | |
| | [CCS icon]         | | [Type2 icon]        | | [CHAdeMO icon]  | |
| | 150 kW max         | | 22 kW max           | | 50 kW max       | |
| | [Charging]         | | [Available]          | | [Faulted]       | |
| | Session: #S-045    | |                      | | Error: OVP      | |
| | User: Nguyen Van A | |                      | |                 | |
| | 12.4 kWh / 45m     | |                      | |                 | |
| | [Stop] [Unlock]    | | [Start] [Unlock]     | | [Reset]         | |
| +--------------------+ +----------------------+ +-----------------+ |
|                                                                    |
| === SESSIONS TAB ===                                               |
| (Embedded session table, filtered to this station)                 |
|                                                                    |
| === FAULTS TAB ===                                                 |
| (Embedded fault cards, filtered to this station)                   |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Back button                  | Ghost button with ArrowLeft icon, "Back to Stations", navigates to `/stations`             |
| Header                       | Station name (24px bold) + status badge (right-aligned). Below: station code (mono, 13px, neutral-400) + address (14px neutral-600). Action buttons: Edit (secondary), Enable/Disable (secondary), Decommission (destructive, with confirmation dialog) |
| Tab bar                      | Horizontal tab strip, 5 tabs. Active tab: brand-green underline (2px), brand-green text. Inactive: neutral-600 text, hover: neutral-900 text. Tabs: Overview, Connectors, Sessions, Faults, Maintenance |
| Station Info card            | Level 1 card, 2-column key-value layout. Label (13px neutral-600) on left, value (14px neutral-900) on right. Dividers between rows (neutral-100 border-bottom) |
| Stats card                   | Level 1 card, same layout. Numbers use tabular-nums. Revenue formatted as VND. Uptime as percentage with green color if >95%, orange if 90-95%, red if <90% |
| Connector card               | Level 1 card, 200px min-width. Top: connector number + type label + type icon (CCS, Type2, CHAdeMO, GBT). Middle: max power (kW) + status badge. If charging: active session details (session ID, user, energy, duration) in compact format. Bottom: action buttons (Start/Stop/Unlock/Reset) appropriate to status |
| Connector type icon          | 32px SVG icons for each connector type. CCS2=lightning-bolt-in-circle, Type2=plug-circle, CHAdeMO=japanese-plug, GBT=china-plug. Fallback: generic Plug icon |
| Session table (embedded)     | Same as Session List table but pre-filtered to this station. Compact variant (no station column) |
| Fault list (embedded)        | Same as Faults page cards but pre-filtered. Compact variant                                |

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click "Edit"                 | Navigate to `/stations/{id}/edit`                    |
| Click "Enable/Disable"       | Mutation + optimistic update + success toast          |
| Click "Decommission"        | Confirmation dialog with warning text, then mutation + redirect to stations list |
| Click connector "Start"     | Dialog: select ID tag or user, confirm remote start   |
| Click connector "Stop"      | Confirmation dialog, then RemoteStopTransaction       |
| Click connector "Unlock"    | Direct mutation + toast                               |
| Click connector "Reset"     | Dialog: soft or hard reset selection                  |
| Click "Add Connector"       | Inline form or modal: connector number, type dropdown, max power input |

### Loading State
- Header: skeleton name + skeleton badge.
- Tab content: skeleton matching active tab layout.
- Connector cards: 3 skeleton cards.

### Empty State
- Connectors tab with 0 connectors: "No connectors configured. Add one to start charging." + "Add Connector" button.
- Sessions tab empty: "No sessions recorded for this station."
- Faults tab empty: "No faults reported. All systems operational." with green CheckCircle icon.

### Error State
- Station not found: "Station not found" message with back button.
- API error: error banner at top of content area.

### Mobile / Tablet Considerations
- Tab bar becomes horizontally scrollable on mobile (horizontal scroll with snap points).
- Connector cards: single column on mobile.
- Station Info and Stats cards: stacked (single column) on mobile and tablet.
- Action buttons in header: overflow into a "..." dropdown menu on mobile.
- Connector action buttons: icon-only on mobile (with tooltips).

---

## 7. Page 6: Session List

### UX Goal
Track and troubleshoot charging sessions. Quickly identify active sessions, filter by status, and drill into details. Active sessions should be visually prominent and auto-updating.

### Key Data to Surface
- Session ID
- Station and connector
- User name
- Session status
- Start time and duration
- Energy delivered (kWh)
- Cost (VND)
- Summary KPIs (active count, total energy, total revenue, total count)

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Charging Sessions"]                                |
+------------------------------------------------------------------+
| KPI Row (4 cards)                                                  |
| +-- Active -----+ +-- Energy -----+ +-- Revenue ----+ +-- Total -+
| | [Zap] 12      | | [Battery]     | | [Banknote]    | | [Clock]  |
| | Active Sessions| | 1,247.8 kWh  | | 12.450.000d   | | 3,456    |
| |                | | delivered     | | total revenue | | sessions |
| +----------------+ +---------------+ +---------------+ +----------+
|                                                                    |
| Filter Bar                                                         |
| +--------------------------------------------------------------+ |
| | [Search] Search by station or user... | [All] [Active]       | |
| |                                       | [Completed] [Failed] | |
| |                          [Date from] [Date to]               | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Session Table                                                      |
| +--------------------------------------------------------------+ |
| | ID      | Station  | Connector | User   | Status | Start    | |
| |         |          |           |        |        | Time     | |
| | Duration| Energy   | Cost     | Actions                      | |
| +---------+----------+-----------+--------+--------+----------+ |
| | S-045   | Alpha    | #1 CCS2  | Nguyen | [Charging] | 14:30 | |  <- green tinted row
| | 45m     | 12.4 kWh | 24.800d  | [View] [Stop]              | |
| +---------+----------+-----------+--------+--------+----------+ |
| | S-044   | Beta     | #2 Type2 | Tran   | [Completed]| 13:15 | |
| | 1h 20m  | 28.6 kWh | 57.200d  | [View]                     | |
| +---------+----------+-----------+--------+--------+----------+ |
|                                                                    |
| Pagination: << Previous  |  Page info  |  Next >>                 |
+------------------------------------------------------------------+
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| KPI cards                    | Same design as Dashboard KPIs but context-specific. Active Sessions: chart-blue icon bg. Energy: brand-green. Revenue: brand-orange. Total: neutral purple (#8B5CF6) |
| Status filter chips          | Horizontal chip group. All=neutral, Active=chart-blue filled, Completed=brand-green filled, Failed=critical-red filled. Active chip has white text. Inactive chips are outline style |
| Date range picker            | Two date inputs (from/to) with Calendar icon prefix. Optional: preset buttons (Today, Last 7 Days, Last 30 Days) |
| Session table                | Full-width table. Key columns left-aligned. Energy and Cost columns right-aligned with tabular-nums. Active sessions: entire row has `brand-green-light` (5% opacity) background tint |
| Session status badges        | InProgress: chart-blue badge with subtle pulse animation. Completed: brand-green badge. Failed: critical-red badge. Suspended: brand-orange badge. Pending: neutral badge |
| Row actions                  | "View" icon button (Eye, always visible). "Stop" button (Square icon, only for active sessions, destructive-outline style). Confirmation dialog before stopping |
| Pagination                   | Cursor-based. "Showing 1-20 of 3,456 sessions" + Previous/Next outline buttons            |

### Number Formatting
- Energy: right-aligned, tabular-nums, format: `XX.XX kWh`
- Cost: right-aligned, tabular-nums, format: `XX.XXXd` (VND with d suffix, dot thousands separator)
- Duration: `Xh Ym` format or `Ym` if under 1 hour
- Session ID: mono font, truncated to 8 chars with tooltip for full ID

### Active Session Row Highlighting
```css
.session-row-active {
  background: rgba(40, 166, 73, 0.04);
  border-left: 3px solid #28A649;
}
```

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click row                    | Navigate to `/sessions/{id}`                         |
| Click "View" icon            | Navigate to `/sessions/{id}`                         |
| Click "Stop" button          | Confirmation dialog -> stop session mutation          |
| Click status chip            | Filter table by status, reset cursor                 |
| Type in search               | Debounce 300ms, filter by station/user name          |
| Change date range            | Reset cursor, refetch                                |

### Loading State
- KPI cards: 4 skeleton cards.
- Table: 10 skeleton rows with shimmer.

### Empty State
- No sessions at all: Zap icon (48px neutral-300), "No charging sessions recorded yet."
- No sessions matching filter: "No sessions match your criteria. Try adjusting filters."

### Auto-refresh
- Active sessions tab: `refetchInterval: 15000` (15 seconds).
- Completed/Failed tabs: `refetchInterval: 60000` (60 seconds).

### Mobile / Tablet Considerations
- KPI cards: 2x2 grid on tablet, scrollable horizontal row on mobile.
- Table: converts to card list on mobile. Each card shows: station name + connector, status badge, start time, duration, energy, cost. Tap card to navigate to detail.
- Filter chips: horizontally scrollable.
- Date range: stacked inputs on mobile.

---

## 8. Page 7: Session Detail

### UX Goal
Full session analysis with energy curve. Provide all information needed to understand, verify, and troubleshoot a specific charging session.

### Key Data to Surface
- Session identity (ID, station, connector)
- Session status with timeline
- User and vehicle information
- Timing (start, end, duration)
- Authentication method (ID tag)
- Billing breakdown (energy, rate, subtotal, tax, total)
- Energy delivery curve (meter values over time)
- Meter values table (raw data)

### Layout Structure

```
+------------------------------------------------------------------+
| [<- Back to Sessions]                                             |
+------------------------------------------------------------------+
| Header                                                            |
| +--------------------------------------------------------------+ |
| | Session #S-045           [Charging]                           | |
| | Station Alpha > Connector #1 (CCS2)                          | |
| +--------------------------------------------------------------+ |
|                                                                    |
| 2-Column Layout (stacked on mobile)                               |
| +-- Session Info Card --------+ +-- Billing Card ---------------+ |
| | User: Nguyen Van A          | | Energy Delivered: 12.40 kWh  | |
| | Vehicle: VinFast VF8        | | Rate: 2.000d/kWh             | |
| | Start: 14:30 08/03/2026     | | Subtotal: 24.800d            | |
| | End: (still charging)       | | Tax (10%): 2.480d            | |
| | Duration: 45m (live counter)| |-------------------------------|  |
| | Auth: RFID (tag: ABC123)    | | Total: 27.280d               | |
| | Stop Reason: (n/a)          | | (bold, brand-green, 20px)    | |
| +------------------------------+ +------------------------------+ |
|                                                                    |
| Energy Curve (full width)                                          |
| +--------------------------------------------------------------+ |
| | [Smooth Area Chart]                                           | |
| | Y-axis left: Power (kW)                                       | |
| | Y-axis right: SoC (%) -- if available                         | |
| | X-axis: Time (HH:mm)                                          | |
| | Fill: brand-green at 15% opacity                               | |
| | Stroke: brand-green 2px                                        | |
| | Data points on hover: circular dot with tooltip                | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Meter Values Table                                                 |
| +--------------------------------------------------------------+ |
| | Timestamp    | Power kW | Energy kWh | SoC %  | Voltage V   | |
| | 14:30:00     | 48.2     | 0.00       | 45%    | 400.1       | |
| | 14:35:00     | 52.1     | 4.17       | 52%    | 401.3       | |
| | 14:40:00     | 50.8     | 8.40       | 59%    | 400.8       | |
| | ...          |          |            |        |             | |
| +--------------------------------------------------------------+ |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Back button                  | Ghost button, ArrowLeft icon, "Back to Sessions"                                           |
| Header                       | Session ID (mono, 18px 600 weight) + status badge (right). Below: station name link + connector info (14px neutral-600) |
| Session Info card            | Level 1 card, key-value pairs. User name (link to user profile if admin). Vehicle name. Start/End time in `dd/MM/yyyy HH:mm` format. Duration: live counter for active sessions (updates every second). Auth method + ID tag (mono) |
| Billing card                 | Level 1 card. Line items: Energy x Rate = Subtotal. Tax line. Divider. Total in 20px bold brand-green. For active sessions: "estimated" label next to total |
| Energy Curve chart           | Recharts AreaChart. Area fill: `rgba(40,166,73,0.12)`. Stroke: `#28A649`, 2px. If SoC data available: second line in chart-blue on right Y-axis. Smooth monotone interpolation. Hover: vertical guideline + tooltip with all values. For active sessions: chart auto-refreshes every 30s with new data points appending |
| Meter Values table           | Compact table. Timestamp: `HH:mm:ss` format. Numeric columns: right-aligned, mono font, tabular-nums. SoC column: show percentage with colored bar (inline mini progress bar). Missing values: en-dash |
| Live counter (active)        | For active sessions: duration shows `XXh XXm XXs` updating every second. Energy shows current total updating on refetch. Billing "estimated" with blinking dot indicator |

### Chart Detail

```
Chart area: 400px height desktop, 280px mobile.
Grid: dashed lines, neutral-200.
X-axis: time labels every 5 minutes (or auto-computed based on duration).
Y-axis left: Power (kW), auto-scaled.
Y-axis right (if SoC): 0-100%.
Lines:
  - Power: brand-orange (#FAA623), 2px stroke, dashed.
  - Energy: brand-green (#28A649), 2px stroke, solid, area fill.
  - SoC: chart-blue (#3B82F6), 2px stroke, solid.
Legend: below chart, horizontal, colored circles + labels.
```

### States

| State           | Behavior                                                                        |
|-----------------|---------------------------------------------------------------------------------|
| Active session  | Live-updating duration counter, energy refreshes every 30s, chart appends new points, billing shows "estimated", "Stop Session" button visible in header |
| Completed       | All fields static, chart complete, no refresh                                   |
| Failed          | Red status badge, stop reason displayed prominently, chart shows data up to failure point |
| Loading         | Skeleton for cards, skeleton rectangle for chart, skeleton table rows           |
| No meter data   | Chart area shows: "No meter data available for this session" with BarChart2 icon |

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click "Back to Sessions"     | Navigate to `/sessions`                              |
| Click station name           | Navigate to `/stations/{stationId}`                  |
| Click "Stop Session"         | Confirmation dialog -> stop mutation -> refresh       |
| Click user name              | Navigate to user profile (if admin)                  |
| Hover chart data point       | Tooltip with timestamp, power, energy, SoC values     |

### Mobile / Tablet Considerations
- Session Info and Billing cards: stacked single column on mobile.
- Chart: full width, reduced height (280px on mobile).
- Meter Values table: horizontal scrollable on mobile with sticky timestamp column.
- Live counter: simplified format on mobile (`45m 30s` instead of `0h 45m 30s`).

---

## 9. Page 8: Faults & Alerts

### UX Goal
Rapid incident triage and resolution. Operators should immediately see the severity distribution, prioritize critical issues, and track resolution progress.

### Key Data to Surface
- Severity distribution (Critical, High, Medium, Low counts)
- Fault list with severity, station, connector, type, time, status
- Alert list with acknowledgement actions
- Resolution workflow (Open -> Investigating -> Resolved)

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Faults & Alerts"]                                  |
+------------------------------------------------------------------+
| Severity Summary Bar (4 clickable cards)                           |
| +-- Critical ----+ +-- High --------+ +-- Medium -----+ +-- Low -+
| | [Filled circle] | | [Triangle]     | | [Circle-!]    | | [Info] |
| | 3               | | 7              | | 12            | | 5      |
| | CRITICAL        | | HIGH           | | MEDIUM        | | LOW    |
| | (red bg)        | | (orange bg)    | | (amber bg)    | | (blue) |
| +--critical-red---+ +--brand-orange--+ +--amber--------+ +--blue--+
|                                                                    |
| Tab Bar: [Faults] [Alerts]                                         |
|                                                                    |
| Filter Bar                                                         |
| +--------------------------------------------------------------+ |
| | [Search] Search faults...  | [All] [Open] [Investigating]   | |
| |                            | [Resolved] [Closed]            | |
| +--------------------------------------------------------------+ |
|                                                                    |
| === FAULTS TAB ===                                                 |
| +--------------------------------------------------------------+ |
| | [RED LEFT BORDER]                                             | |
| | ConnectorFailure at Station Alpha - Connector #3              | |
| | Over-voltage protection triggered                              | |
| | Detected: 14:30 08/03/2026  |  Status: [Open]                | |
| | [Investigate]                                                  | |
| +--------------------------------------------------------------+ |
| | [ORANGE LEFT BORDER]                                          | |
| | GroundFailure at Station Beta - Connector #1                  | |
| | Ground fault detected on AC line                               | |
| | Detected: 12:15 08/03/2026  |  Status: [Investigating]       | |
| | [Mark Resolved]  [Add Notes]                                   | |
| +--------------------------------------------------------------+ |
|                                                                    |
| === ALERTS TAB ===                                                 |
| (Similar card layout with Acknowledge/Dismiss actions)             |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Severity summary card        | Compact KPI card, 120px min-width. Icon at top (filled-circle=Critical, triangle=High, circle-exclamation=Medium, info-circle=Low). Large count (28px bold). Label (11px uppercase). Background tint matching severity. Clickable: filters fault list by severity. Active state: ring/border highlight |
| Tab bar                      | 2 tabs: Faults, Alerts. Same style as Station Detail tabs                                   |
| Status filter chips          | Same chip group pattern. All, Open (red), Investigating (orange), Resolved (green), Closed (gray) |
| Fault card                   | Level 1 card with colored left border (3px). Critical=critical-red, High=brand-orange, Medium=#F59E0B (amber), Low=chart-blue. Content: error code (16px 600 weight), severity badge + status badge on same line. Description (14px neutral-600). Footer: station name, connector, detection time, resolution time (if applicable). Right side: action buttons |
| Severity badge               | Uses standard badge component. Critical=red, High=orange with white text, Medium=amber/yellow, Low=blue |
| Fault status badge           | Open=critical-red, Investigating=brand-orange, Resolved=brand-green, Closed=neutral        |
| Action buttons               | "Investigate" (secondary, Wrench icon) for Open faults. "Mark Resolved" (primary, CheckCircle icon) for Investigating. "Add Notes" (ghost, MessageSquare icon) opens textarea dialog. "Close" (ghost) for Resolved |
| Alert card                   | Similar to fault card but with Acknowledge (CheckCircle) and Dismiss (X) actions. Bell icon instead of severity icon |

### Severity Icons

| Severity | Icon               | Color          | Background Tint    |
|----------|--------------------|----------------|-------------------|
| Critical | Filled circle (solid) | `#EF4444`   | `#FEF2F2`          |
| High     | Triangle (AlertTriangle) | `#FAA623` | `#FFF5E5`          |
| Medium   | CircleAlert        | `#F59E0B`      | `#FFFBEB`          |
| Low      | Info               | `#3B82F6`      | `#EFF6FF`          |

### Sort Order
- Default: newest first.
- Critical and High severity faults pinned to top regardless of time.
- Sort algorithm: `severity ASC, detectedAt DESC` (Critical=1 first, then by most recent).

### Real-time Updates
- SignalR `AlertCreated` events add new faults/alerts to the top of the list with a slide-in animation.
- Toast notification appears for Critical/High severity: red/orange banner at top-right, auto-dismiss 8s.
- Count badges in severity summary cards update in real-time.

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click severity card          | Filter faults by that severity                       |
| Click fault card             | Navigate to `/faults/{id}` detail page               |
| Click "Investigate"          | Update status to Investigating, toast confirmation   |
| Click "Mark Resolved"        | Dialog: optional resolution notes textarea, then update status |
| Click "Add Notes"            | Dialog: textarea for notes, save                     |
| Click "Acknowledge" (alert)  | Mark alert as acknowledged, remove from active list   |
| Click "Dismiss" (alert)      | Dismiss alert, remove from list                      |

### Loading State
- Severity cards: 4 skeleton cards.
- Fault list: 5 skeleton cards with shimmer.

### Empty State
- Faults tab empty: Green CheckCircle icon (48px), "All clear! No faults reported." Positive messaging.
- Alerts tab empty: Bell icon (48px neutral-300), "No active alerts."

### Mobile / Tablet Considerations
- Severity summary: 2x2 grid on tablet, horizontal scrollable on mobile.
- Fault cards: full width, stacked.
- Action buttons: stacked below card content on mobile (not side-by-side).
- Tab bar: full width, equal split on mobile.
- New fault toast: full-width banner on mobile (not corner popup).

---

## 10. Page 9: Pricing Configuration (Tariffs)

### UX Goal
Clear rate management with instant understanding of pricing impact. Operators should quickly see all tariffs, identify the default/active ones, and manage rates with confidence.

### Key Data to Surface
- Tariff name and description
- Base rate per kWh (VND)
- Tax rate (%)
- Total rate (computed)
- Active/Inactive status
- Default tariff designation
- Effective dates
- Assigned station count

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Pricing"]  [+ Create Tariff]                      |
+------------------------------------------------------------------+
|                                                                    |
| === CREATE/EDIT FORM (inline, shown when creating/editing) ===    |
| +--------------------------------------------------------------+ |
| | Tariff Name *     | Description                               | |
| | [Standard Rate ]  | [Default pricing for...]                  | |
| |                                                                | |
| | Base Rate/kWh *   | Tax Rate *        | Effective From *       | |
| | [2,000] d/kWh     | [10] %            | [2026-03-08]           | |
| |                                                                | |
| | Effective To (optional)  | Description                        | |
| | [____]                   | [_______________]                  | |
| |                                                                | |
| | [Create Tariff]  [Cancel]                                      | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Tariff Grid (3 columns desktop, 2 tablet, 1 mobile)              |
|                                                                    |
| +-- DEFAULT TARIFF -------+ +-- TARIFF --------+ +-- TARIFF ---+ |
| | [Green ring border]     | |                   | |              | |
| | [Star] Default          | |                   | |              | |
| |                         | |                   | |              | |
| | Standard Rate           | | Peak Hours        | | Off-Peak     | |
| | [Active]                | | [Active]          | | [Inactive]   | |
| |                         | |                   | |              | |
| | Base: 2.000d/kWh        | | Base: 3.000d/kWh  | | Base: 1.500d | |
| | Tax: 10%                | | Tax: 10%          | | Tax: 10%     | |
| | Total: 2.200d/kWh       | | Total: 3.300d/kWh | | Total: 1.650d| |
| |                         | |                   | |              | |
| | Effective: 01/01/2026   | | Effective: 01/03  | | Effective: - | |
| | 15 stations assigned     | | 5 stations        | | 0 stations   | |
| |                         | |                   | |              | |
| | [Edit] [Deactivate]     | | [Edit] [Deact.]   | | [Edit] [Act.]| |
| |                         | | [Set Default]     | | [Delete]     | |
| +-------------------------+ +-------------------+ +--------------+ |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Create button                | Primary (brand-green), Plus icon, "Create Tariff"                                          |
| Create/Edit form             | Inline card that appears at top when creating/editing. 2-column grid for fields. Inputs: name (text, required), description (text), base rate (number with "d/kWh" suffix), tax rate (number with "%" suffix), effective from (date, required), effective to (date, optional). Validation errors shown inline below each field |
| Tariff card                  | Level 1 card, 280px min-width. Default tariff: `border: 2px solid brand-green` (ring). Badge positioning: "Default" badge with Star icon at top-right corner, overlapping card edge. Card content: name (16px 600 weight), status badge (Active=green, Inactive=gray), rate section (3 rows: base, tax, total with clear labels), effective date, station count. Action buttons at bottom |
| Default tariff ring          | `box-shadow: 0 0 0 2px #28A649` (brand-green ring around card). Default badge: brand-green badge with Star icon, positioned absolute at top-right (-8px offset) |
| Active/Inactive badge        | Active: brand-green badge. Inactive: neutral badge                                         |
| Rate display                 | Key-value layout. Label: 13px neutral-600 with icon (Zap=base, DollarSign=total, Percent=tax). Value: 14px 600 weight, tabular-nums. VND formatted with "d/kWh" suffix |
| Station count                | 13px neutral-600, icon MapPin, "15 stations assigned"                                      |
| Action buttons               | Row of icon buttons: Edit (Edit icon), Activate/Deactivate (Check/X icon), Set Default (Star icon, only for active non-default), Delete (Trash2 icon, destructive, only for non-default, with confirmation dialog) |
| Error alert                  | Red-tinted container with AlertCircle icon for form validation or API errors               |

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click "Create Tariff"        | Show inline form at top                              |
| Submit form                  | Create/update mutation + close form + toast           |
| Click "Cancel"               | Close form, reset fields                             |
| Click "Edit" on card         | Populate form with tariff data, scroll to form       |
| Click "Activate/Deactivate"  | Inline mutation + toast                              |
| Click "Set Default"          | Mutation (only one default at a time) + toast         |
| Click "Delete"               | Confirmation dialog -> delete mutation + toast        |

### Loading State
- Tariff grid: 3 skeleton cards with shimmer.

### Empty State
- "No tariffs configured yet. Create your first pricing plan to start billing."
- CTA: "Create Tariff" button.

### Mobile / Tablet Considerations
- Tariff cards: single column on mobile.
- Create/Edit form: stacked single column layout on mobile.
- Rate inputs: full width stacked.
- Action buttons: icon-only on mobile with tooltips.

### Future Enhancement: TOU Visual Editor
- Reserved space for time-of-use block editor.
- Visual 24-hour timeline with draggable rate blocks.
- Color-coded: peak (orange), off-peak (green), standard (blue).
- Not in MVP scope; placeholder note in UI.

---

## 11. Page 10: Payments & Transactions

### UX Goal
Financial visibility and reconciliation. Provide clear, filterable transaction history with KPI summary for quick financial health assessment. Enable refund workflows.

### Key Data to Surface
- Total revenue (period)
- Successful payment count
- Pending payment count
- Failed payment count
- Transaction list (ID, user, amount, method, status, date, session link)
- Payment method breakdown

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Payments"]  [Export]                                |
+------------------------------------------------------------------+
| KPI Row (4 cards)                                                  |
| +-- Total Rev. --+ +-- Successful --+ +-- Pending ----+ +--Failed+
| | [Banknote]     | | [CheckCircle]  | | [Clock]       | | [X]   |
| | 45.600.000d    | | 1,234          | | 15            | | 3     |
| | TOTAL REVENUE  | | SUCCESSFUL     | | PENDING       | | FAILED|
| | +5.2% trend    | |                | | (amber text)  | | (red) |
| +----------------+ +----------------+ +---------------+ +-------+
|                                                                    |
| Filter Bar                                                         |
| +--------------------------------------------------------------+ |
| | [Search] Search by transaction or user...                     | |
| | Status: [All Status v]  Method: [All Methods v]               | |
| | Date: [from] to [to]                                          | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Transaction Table                                                  |
| +--------------------------------------------------------------+ |
| | ID        | User    | Amount   | Method      | Status  | Date | |
| | TXN-001   | Nguyen  | 24.800d  | [MoMo icon] | [Paid]  | ... | |
| | TXN-002   | Tran    | 57.200d  | [VnPay icon]| [Pending]|... | |
| +--------------------------------------------------------------+ |
| | Side Drawer / Modal for detail (on row click)                  | |
| +--------------------------------------------------------------+ |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Export button                | Secondary button, Download icon, "Export". Downloads CSV with current filter applied        |
| KPI cards                    | Same pattern as other pages. Total Revenue: brand-green icon bg. Successful: brand-green. Pending: brand-orange (amber text for count). Failed: critical-red (red text for count). Trend indicator on Total Revenue card |
| Search input                 | Standard search with icon, searches by transaction ID, user name, or reference code         |
| Status filter                | Dropdown: All, Pending, Processing, Completed, Failed, Refunded, Cancelled                 |
| Payment method filter        | Dropdown: All, MoMo, VnPay, ZaloPay, Wallet, QR Payment. Each option has small colored icon |
| Date range                   | Two date inputs with Calendar icon                                                          |
| Transaction table            | Columns: ID (mono, truncated), User, Amount (right-aligned tabular-nums, "d" suffix), Method (icon + label), Status (badge), Date (relative with full datetime tooltip), Actions (View, Refund) |
| Payment method icons         | Small (16px) colored icons inline with method label. MoMo: #A50064 circle. VnPay: #0066CC circle. ZaloPay: #00AAFF circle. Wallet: brand-green circle. QR: neutral circle. Each with first letter or abbreviation |
| Amount formatting            | VND with "d" suffix, right-aligned, tabular-nums, dot separator (e.g., "24.800d")         |
| Status badges                | Completed/Success=brand-green. Pending=brand-orange. Processing=chart-blue. Failed=critical-red. Refunded=neutral (with RotateCcw icon). Cancelled=neutral with strikethrough |
| Detail drawer                | Slide-in from right (400px width) or modal. Shows: full transaction details, linked session, user info, payment gateway response, refund button (for completed), timeline of status changes |
| Refund dialog                | Modal: amount display, reason textarea (optional), confirmation buttons. Warning text about wallet credit |

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click row                    | Open detail drawer/modal                             |
| Click "View" icon            | Open detail drawer/modal                             |
| Click "Refund" icon          | Open refund confirmation dialog                      |
| Click session link           | Navigate to `/sessions/{sessionId}`                  |
| Click "Export"               | Download CSV of current filtered results              |
| Change filter                | Reset cursor, refetch                                |

### Loading State
- KPI cards: 4 skeleton cards.
- Table: 10 skeleton rows.

### Empty State
- "No payment transactions found." with CreditCard icon.
- If filtered: "No transactions match your filters."

### Error State
- Table error: "Failed to load transactions" with Retry button.
- Refund error: inline error message in refund dialog.

### Mobile / Tablet Considerations
- KPI cards: 2x2 grid on tablet, horizontal scroll on mobile.
- Table: card list on mobile. Each card: user + amount header, method + status inline, date below.
- Detail: full-screen modal on mobile instead of side drawer.
- Export: accessible from overflow menu on mobile.
- Filters: collapsible filter panel (tap "Filters" to expand/collapse).

---

## 12. Page 11: User Management

### UX Goal
Manage access and permissions efficiently. Separate concerns between admin users, mobile app users, and role/permission management through clear tab navigation.

### Key Data to Surface
- Admin user list (username, email, name, roles, status, last login)
- Mobile user list
- Role list (name, permission count, user count)
- Permission editor (grouped checkboxes by module)

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "User Management"]                                  |
+------------------------------------------------------------------+
| Tab Bar: [Admin Users] [Mobile Users] [Roles & Permissions]       |
|                                                                    |
| === ADMIN USERS TAB ===                                            |
| +--------------------------------------------------------------+ |
| | [Search] Search users...                     [+ Add User]    | |
| +--------------------------------------------------------------+ |
| | Table                                                          | |
| | +----------------------------------------------------------+ | |
| | | [Avatar] | Name    | Email      | Roles     | Status     | | |
| | |          |         |            |           | Last Login | | |
| | | Actions                                                   | | |
| | +----------+---------+------------+-----------+------------+ | |
| | | [JD]     | John D  | john@...   | [Admin]   | [Active]   | | |
| | |          |         |            | [Operator] | 2h ago     | | |
| | | [Edit] [Roles] [Lock] [Reset PW] [Delete]                | | |
| | +----------+---------+------------+-----------+------------+ | |
| +--------------------------------------------------------------+ |
|                                                                    |
| === ROLES & PERMISSIONS TAB ===                                    |
| +--------------------------------------------------------------+ |
| | Role Cards                                                     | |
| | +-- Admin --------+ +-- Operator -----+ +-- Viewer --------+ | |
| | | admin            | | operator        | | viewer           | | |
| | | 45 permissions   | | 28 permissions  | | 12 permissions   | | |
| | | 3 users          | | 5 users         | | 8 users          | | |
| | | [Edit] [Perms]   | | [Edit] [Perms]  | | [Edit] [Perms]   | | |
| | +------------------+ +-----------------+ +------------------+ | |
| +--------------------------------------------------------------+ |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Tab bar                      | 3 tabs: Admin Users, Mobile Users, Roles & Permissions. Same tab style as other pages      |
| Search input                 | Standard search, filters by username, email, name                                          |
| Add User button              | Primary (brand-green), Plus icon, "Add User"                                               |
| User table                   | Columns: Avatar (40px circle with initials, colored by role), Name (first + last), Email, Roles (badge array), Status (Active=green dot + "Active", Locked=red lock icon + "Locked"), Last Login (relative time), Actions |
| Avatar placeholder           | 40px circle with user initials (first letter of first + last name). Background: deterministic color from role (Admin=brand-green, Operator=chart-blue, Viewer=neutral). Text: white, 14px 600 weight |
| Role badges                  | Outline badges in row. Admin=brand-green outline. Operator=chart-blue outline. Viewer=neutral outline. Custom roles=brand-orange outline |
| User status indicator        | Active: green dot (8px) + "Active" text. Locked: Lock icon (14px red) + "Locked" text. Inactive: gray dot + "Inactive" text |
| Last login                   | Relative time ("2h ago", "3 days ago") with full datetime on hover tooltip                 |
| Row actions                  | Icon buttons: Edit (Edit), Assign Roles (Shield), Lock/Unlock (Lock/Unlock), Reset Password (Key), Delete (Trash2, destructive) |
| Create/Edit User modal       | Dialog modal (480px width). Fields: Username (disabled on edit), Email, Password (create only, with strength indicator), First Name, Last Name, Phone, Active checkbox. Role assignment: multi-select checkboxes |
| Role assignment modal        | Dialog: checkbox list of all roles. Current roles pre-checked. Save/Cancel buttons         |
| Reset password modal         | Dialog: new password input with strength indicator + visibility toggle. Warning text about forcing re-login |
| Role card                    | Level 1 card, 200px min-width. Role name (16px 600 weight), permission count, user count, Static badge (if applicable). Actions: Edit (name only), Permissions (open editor) |
| Permission editor modal      | Full-width modal (max 720px), scrollable. Grouped by module (Stations, Sessions, Payments, etc.). Each group: header (module name, 14px 600 weight) + checkbox list (Default, Create, Update, Delete, plus action-specific). "Select All" / "Deselect All" per group. Sticky footer with Save/Cancel |

### Permission Editor Detail

```
+--------------------------------------------------------------+
| Permissions -- Admin                                [X Close] |
+--------------------------------------------------------------+
| [Select All] [Deselect All]                                   |
|                                                                |
| Stations                                                       |
|   [x] View Stations                                           |
|   [x] Create Station                                          |
|   [x] Update Station                                          |
|   [x] Delete Station                                          |
|   [x] Enable/Disable Station                                  |
|                                                                |
| Sessions                                                       |
|   [x] View Sessions                                           |
|   [ ] Stop Session                                             |
|                                                                |
| Payments                                                       |
|   [x] View Payments                                           |
|   [ ] Issue Refund                                             |
|                                                                |
| ... (scrollable)                                               |
+--------------------------------------------------------------+
| [Save Permissions]  [Cancel]                                   |
+--------------------------------------------------------------+
```

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click "Add User"             | Open Create User modal                               |
| Click "Edit" on user row     | Open Edit User modal (pre-populated)                 |
| Click "Roles" on user row    | Open Role Assignment modal                           |
| Click "Lock/Unlock"          | Direct mutation + toast confirmation                 |
| Click "Reset Password"       | Open Reset Password modal                            |
| Click "Delete"               | Confirmation dialog with username -> delete mutation  |
| Click "Edit" on role card    | Open Edit Role modal (name, default, public)         |
| Click "Permissions" on role  | Open Permission Editor modal                         |
| Click "Add Role"             | Open Create Role modal                               |

### Loading State
- Table: 10 skeleton rows.
- Role cards: 3 skeleton cards.
- Permission modal: "Loading permissions..." centered text.

### Empty State
- Users tab: "No admin users found." (should not normally occur)
- Roles tab: "No roles configured."

### Mobile / Tablet Considerations
- Tab bar: full-width equal split.
- User table: card list on mobile. Each card: avatar + name header, email below, role badges, status indicator, action icons in a row at bottom.
- Role cards: single column on mobile.
- Permission editor: full-screen modal on mobile, grouped sections collapsible (accordion pattern).
- Modals: full-screen on mobile.

---

## 13. Page 12: Audit Logs

### UX Goal
Compliance, change tracking, and forensics. Enable administrators to search, filter, and investigate any system activity with full request/response detail and entity change tracking.

### Key Data to Surface
- Timestamp
- User who performed the action
- HTTP method and URL
- HTTP status code
- Execution duration
- Client IP address
- Entity changes (created/updated/deleted with before/after values)

### Layout Structure

```
+------------------------------------------------------------------+
| [Page Header: "Audit Logs"]  [Export CSV]                         |
+------------------------------------------------------------------+
| Filter Bar                                                         |
| +--------------------------------------------------------------+ |
| | [Search] Search by URL or entity...                           | |
| |                                                                | |
| | Method: [ALL] [GET] [POST] [PUT] [DELETE]                     | |
| |         (colored chips)                                        | |
| |                                                                | |
| | Entity Type: [All Types v]   Date: [from] to [to]            | |
| +--------------------------------------------------------------+ |
|                                                                    |
| Log Table                                                          |
| +--------------------------------------------------------------+ |
| | Timestamp     | User    | Method   | URL       | Status |Dur.| |
| +---------------+---------+----------+-----------+--------+----+ |
| | 12 min ago    | admin   | [POST]   | /api/stat | [200]  |45ms| |
| |   (hover:     |         | (green)  | ions      |(green) |    | |
| |   full date)  |         |          |           |        |    | |
| +---------------+---------+----------+-----------+--------+----+ |
| | 1 hour ago    | system  | [DELETE]  | /api/fau  | [204]  |12ms| |
| |               |         | (red)    | lts/abc   |(green) |    | |
| +---------------+---------+----------+-----------+--------+----+ |
|                                                                    |
| Pagination: << Previous  |  1,234 total logs  |  Next >>         |
+------------------------------------------------------------------+
|                                                                    |
| === DETAIL MODAL (on row click) ===                               |
| +--------------------------------------------------------------+ |
| | Audit Log Details                                     [X]    | |
| |                                                                | |
| | Time: 08/03/2026 14:30:45                                     | |
| | User: admin                                                    | |
| | Method: [POST]  Status: [200]                                 | |
| | URL: /api/stations                                             | |
| | Duration: 45ms                                                 | |
| | IP: 192.168.1.100                                              | |
| | Browser: Chrome 120 / macOS                                    | |
| |                                                                | |
| | --- Entity Changes ---                                         | |
| | [Created] ChargingStation                                      | |
| |   Name: (null) -> "Station Alpha"                              | |
| |   Address: (null) -> "123 Tran Hung Dao"                       | |
| |   Status: (null) -> "Available"                                | |
| |                                                                | |
| | [Updated] Connector                                            | |
| |   Status: "Available" -> "Charging"                            | |
| |   (red strikethrough for old, green for new)                   | |
| +--------------------------------------------------------------+ |
```

### Component List

| Component                    | Spec                                                                                       |
|------------------------------|--------------------------------------------------------------------------------------------|
| Export button                | Secondary button, Download icon, "Export CSV". Exports with current date range filter       |
| Search input                 | Standard search, filters by URL path or entity name                                        |
| HTTP method filter chips     | Horizontal chip group (colored). GET=chart-blue filled. POST=brand-green filled. PUT=brand-orange filled. DELETE=critical-red filled. ALL=neutral. Active chip has white text. Multiple selection supported |
| Entity type filter           | Dropdown: All Types, ChargingStation, Connector, ChargingSession, TariffPlan, AppUser, Fault, etc. |
| Date range                   | Two date inputs with Calendar icon                                                          |
| Log table                    | Columns: Timestamp, User, Method, URL, Status, Duration, IP, Actions. Compact rows (40px height) |
| Timestamp column             | Relative time as primary display ("12 min ago", "2 hours ago"). Full datetime visible on hover tooltip (`dd/MM/yyyy HH:mm:ss`). Font: 13px |
| HTTP method badge            | Outline badge, colored by method. GET: `border-color: #3B82F6, color: #3B82F6`. POST: `border-color: #28A649, color: #28A649`. PUT: `border-color: #FAA623, color: #FAA623`. DELETE: `border-color: #EF4444, color: #EF4444`. PATCH: `border-color: #8B5CF6, color: #8B5CF6` |
| Status code badge            | Colored by range. 2xx: brand-green bg. 3xx: chart-blue bg. 4xx: brand-orange bg. 5xx: critical-red bg. All with white text |
| Duration column              | Right-aligned, 13px. Under 100ms: green text. 100-500ms: neutral. Over 500ms: orange. Over 2s: red |
| URL column                   | Mono font, 13px, truncated to 40 chars with full URL on hover tooltip                     |
| IP column                    | Mono font, 13px                                                                            |
| View action                  | Eye icon button, opens detail modal                                                        |
| Detail modal                 | 640px max-width modal. Top: 2-column grid of metadata. Bottom: Entity Changes section with change cards. Each change card: change type badge (Created=green, Updated=amber, Deleted=red) + entity type name. Property changes: list with property name in bold, old value in red with strikethrough, new value in green. Diff-style presentation |

### Entity Change Diff Display

```
Property changes in detail modal:

Name:     (null) -> "Station Alpha"         (created - only new value, green)
Status:   "Available" -> "Charging"         (updated - old red strikethrough, new green)
IsDeleted: "false" -> "true"                (soft delete - old red, new red)
```

Styling:
- Old value: `text-decoration: line-through, color: critical-red`
- New value: `color: brand-green, font-weight: 500`
- Null values: displayed as italic "(null)" in neutral-400

### Pagination
- Cursor-based, 50 items per page (higher density for logs).
- Footer: total count + Previous/Next.

### Actions

| Trigger                      | Action                                               |
|------------------------------|------------------------------------------------------|
| Click row                    | Open detail modal                                    |
| Click "View" icon            | Open detail modal                                    |
| Click "Export CSV"           | Download CSV with current filters applied             |
| Click method filter chip     | Toggle filter, reset cursor, refetch                 |
| Change entity type           | Reset cursor, refetch                                |
| Change date range            | Reset cursor, refetch                                |

### Loading State
- Table: 15 skeleton rows (denser for logs).
- Detail modal: "Loading..." centered.

### Empty State
- "No audit logs found for the selected criteria."
- If no filters: "No audit logs recorded yet."

### Error State
- Table error: "Failed to load audit logs" with Retry.
- Export error: toast notification "Export failed. Please try again."

### Mobile / Tablet Considerations
- Table: card list on mobile. Each card: timestamp + user header, method badge + URL (truncated) + status badge in row, duration + IP below.
- Filter bar: method chips horizontally scrollable. Entity type and date range in collapsible filter panel.
- Detail modal: full-screen on mobile with scroll.
- Export: accessible from header overflow menu.
- Pagination: simplified to just Previous/Next buttons centered.

---

## Appendix A: Shared UI Patterns

### A.1 Page Header Pattern

Every page uses a consistent header:
```
+------------------------------------------------------------------+
| Page Title (24px, Inter 700)                      [Action Button] |
| Description text (14px, neutral-600)                              |
+------------------------------------------------------------------+
```

### A.2 Filter Bar Pattern

Consistent filter bar across list pages:
```
+------------------------------------------------------------------+
| [Search icon] Search...  | [Filter dropdowns]  | [Action button] |
|                          | [Filter chips]       |                 |
+------------------------------------------------------------------+
```
- Search: always left-aligned, max 320px.
- Filters: center area, wrapping allowed.
- Actions: right-aligned.

### A.3 Table Pattern

All tables follow:
- Header: `neutral-50` background, 13px 600 weight text, `neutral-600` color.
- Rows: 48px height, `neutral-200` border-bottom, hover `neutral-50` background.
- Text alignment: labels left, numbers right (tabular-nums), badges center.
- Empty: centered message with icon.
- Loading: shimmer skeleton rows.

### A.4 Card Pattern

All cards follow:
- Surface: white, Level 1 shadow, 12px border-radius, 24px padding.
- Title: 16px 600 weight, optional icon (20px) to the left.
- Content: 14px body text.
- Actions: bottom of card, separated by border-top.

### A.5 Modal / Dialog Pattern

- Overlay: `rgba(0,0,0,0.5)` backdrop.
- Container: white, Level 3 shadow, 16px border-radius, max-width varies (420-720px).
- Header: title + close button (X icon, top-right).
- Content: scrollable body.
- Footer: action buttons right-aligned, primary + secondary.
- Mobile: full-screen (100vh, no border-radius).

### A.6 Toast / Notification Pattern

- Position: top-right corner, 16px from edges.
- Width: 360px (auto on mobile, full-width with margin).
- Variants: success (brand-green left border), error (critical-red), warning (brand-orange), info (chart-blue).
- Auto-dismiss: 5s (standard), 8s (critical alerts).
- Manual dismiss: X button.
- Animation: slide in from right, fade out.

### A.7 Skeleton Loading Pattern

- Shape: matches exact component dimensions.
- Background: `neutral-100` with shimmer gradient.
- Shimmer: `linear-gradient(90deg, neutral-100 25%, neutral-50 50%, neutral-100 75%)` animated `1.5s infinite`.
- Corner radius: matches the component being loaded.

### A.8 Cursor-based Pagination Pattern

All paginated lists use cursor-based pagination:
- Footer bar: "Showing X items of Y total" (left) + Previous/Next buttons (right).
- Previous: disabled on first page. Uses cursor stack for back navigation.
- Next: disabled when returned items < page size.
- Page sizes: 20 (default for most tables), 50 (audit logs).
- No page numbers shown (cursor-based, not offset-based).

---

## Appendix B: Responsive Breakpoint Behavior Summary

| Page              | Desktop (>1024px)            | Tablet (768-1024px)         | Mobile (<768px)              |
|-------------------|------------------------------|-----------------------------|------------------------------|
| Login             | Centered card (420px)        | Centered card (420px)        | Full-width card with margin  |
| Dashboard         | 4-col KPIs, 2-col charts    | 2-col KPIs, stacked charts  | 1-col everything stacked     |
| Monitoring        | 3-col grid + side panel      | 2-col grid, panel as tab    | 1-col grid, FAB for commands |
| Station List      | Table/3-col grid/split map   | 2-col grid, map full-screen  | Cards, map full-screen toggle |
| Station Detail    | 2-col info, 3-col connectors | 2-col info, 2-col connectors| Stacked, scrollable tabs     |
| Session List      | Full table                    | Full table (scroll)          | Card list                    |
| Session Detail    | 2-col cards, full chart       | 2-col cards, full chart      | Stacked cards, full chart    |
| Faults & Alerts   | 4-col severity, card list    | 2-col severity, card list   | Scrollable severity, cards   |
| Tariffs           | 3-col grid                    | 2-col grid                   | 1-col stacked               |
| Payments          | Full table with drawer        | Full table with modal        | Card list with full modal    |
| User Management   | Full table with modals        | Full table with modals       | Card list, full-screen modals|
| Audit Logs        | Full table with modal         | Full table (scroll)          | Card list, full-screen modal |

---

## Appendix C: Current Implementation Gap Analysis

This section identifies specific gaps between the current codebase and this specification, organized by page.

| Page             | Current State                              | Gap                                                      |
|------------------|--------------------------------------------|-----------------------------------------------------------|
| Login            | Generic Zap icon, "KLC Admin" branding     | Needs K-Charge logo SVG, brand gradient background, demo credential auto-fill |
| Dashboard        | Missing trend arrows, basic bar chart      | Needs donut chart, area chart, trend indicators, KPI click navigation |
| Monitoring       | No OCPP command panel, basic connector viz  | Needs side panel for OCPP commands, connector pills with smooth transitions, last-updated timer |
| Station List     | Grid view only, no map/list view toggle    | Needs list/map/grid toggle, map view with bidirectional sync, status/group filters |
| Station Detail   | Tab-less, flat layout                      | Needs tabbed interface (Overview/Connectors/Sessions/Faults/Maintenance), connector type icons, stats card |
| Session List     | Basic table                                | Needs active row highlighting, status chips, date range filter, auto-refresh per status |
| Session Detail   | LineChart (not area), basic layout          | Needs area chart with brand-green fill, live counter for active sessions, improved billing card |
| Faults & Alerts  | Combined page, no severity summary         | Needs severity summary cards, separate Faults/Alerts tabs, colored left borders, severity pinning |
| Tariffs          | Card grid working                           | Needs station count on cards, future TOU editor placeholder, improved rate display layout |
| Payments         | Table working, refund dialog exists         | Needs payment method icons, detail side drawer, KPI trend indicators, improved amount formatting |
| User Management  | Users and Roles tabs working                | Needs Mobile Users tab, avatar placeholders, last login relative time, password strength indicator |
| Audit Logs       | Table working, detail modal exists          | Needs relative timestamps, HTTP method colored chips (outline style), entity type filter, 50 per page |

---

## Appendix D: Tailwind CSS Configuration Additions

To support the design system, add the following to `tailwind.config.ts`:

```typescript
// Colors to add to theme.extend.colors
colors: {
  brand: {
    green: {
      DEFAULT: '#28A649',
      dark: '#1E7D37',
      light: '#E8F5EC',
    },
    orange: {
      DEFAULT: '#FAA623',
      dark: '#D4891A',
      light: '#FFF5E5',
    },
  },
  chart: {
    blue: '#3B82F6',
    'blue-light': '#EFF6FF',
    purple: '#8B5CF6',
  },
}

// Font family addition
fontFamily: {
  sans: ['Inter', ...defaultTheme.fontFamily.sans],
  mono: ['JetBrains Mono', ...defaultTheme.fontFamily.mono],
}
```

---

*End of specification. This document should be treated as the source of truth for all visual and interaction design decisions in the K-Charge Admin Portal redesign.*
