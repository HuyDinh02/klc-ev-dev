# Admin Portal UI Audit Report

**Date:** 2026-03-08
**Scope:** K-Charge EV Charging Admin Portal (`src/admin-portal/`)
**Auditor:** Design & Frontend Review
**Status:** Findings documented, remediation pending

---

## 1. Technology Stack Summary

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | Next.js (App Router) | 16.1.6 |
| UI Library | React | 19.2.3 |
| Language | TypeScript | 5.x (strict mode) |
| Styling | Tailwind CSS 4 with CSS variables (HSL-based) | 4.x |
| State | Zustand (auth, sidebar, alerts) | 5.0.11 |
| Data Fetching | TanStack React Query | 5.90.21 |
| Charts | Recharts | 3.7.0 |
| Maps | Leaflet + react-leaflet | 1.9.4 / 5.0.0 |
| Icons | Lucide React | 0.575.0 |
| Real-time | @microsoft/signalr | 10.0.0 |
| Font | Inter (via Next.js font optimization) | -- |

Custom UI components (no external component library): Button (6 variants, 4 sizes), Card (6 sub-components), Badge (6 variants), Input.

**Pages implemented:** 27 fully functional pages (not stubs), 29 route files total.

---

## 2. Strengths

1. **Clean component library** -- Four well-structured primitives with no external UI framework dependency. Zero bloat from unused component library code.

2. **Secure authentication** -- Token exchange via server-side Next.js API route (`/api/auth/token`), keeping OpenIddict client_secret off the browser. JWT parsing done client-side only for display.

3. **Real-time integration** -- SignalR `MonitoringHub` with graceful degradation: connected state uses 60s polling fallback, disconnected state uses 10s polling. Live connection indicator in monitoring page.

4. **Cursor-based pagination** -- Implemented correctly across sessions, payments, and list pages with cursor stack for back-navigation. No offset-based pagination anywhere.

5. **TanStack Query discipline** -- Consistent query key patterns, 1-minute stale time default, 10-30 second refetch intervals for live data. Query invalidation on mutations.

6. **All 27 pages are production-quality** -- Each page fetches real data, handles loading/empty/error states (even if inconsistently), and is fully navigable.

---

## 3. Problems Found

### 3.1 Color System -- CRITICAL

**Finding:** The primary color is blue (`--primary: 221.2 83.2% 53.3%`, approximately `#5B87D9`) instead of the K-Charge brand green (`#28A649`). There is no brand green or brand orange anywhere in the CSS theme.

**Evidence from `globals.css`:**
```css
--primary: 221.2 83.2% 53.3%;  /* Blue -- should be K-Charge green */
--ring: 221.2 83.2% 53.3%;      /* Also blue */
```

**No brand tokens defined** -- There are no CSS variables for:
- Brand green (`#28A649`)
- Brand orange (accent)
- Semantic status colors (available, charging, faulted, offline)
- Chart color palette

**Hardcoded status colors across 20 files (108 occurrences):**
- `text-green-600`, `text-green-500`, `text-green-700` (inconsistent shades)
- `text-red-600`, `text-red-700`
- `text-yellow-600`, `text-yellow-500`
- `text-blue-600`
- `text-gray-600`, `text-gray-500`, `text-gray-700`
- `bg-green-100`, `bg-blue-100`, `bg-gray-100`, `bg-red-50`, `bg-green-50`, `bg-yellow-50`
- `bg-orange-500` (faults page, inline override on Badge)

**Hardcoded chart hex colors:**
- `#22c55e` (green) in analytics energy chart, session detail chart, map markers
- `#3b82f6` (blue) in analytics sessions chart, session detail chart, map markers
- `#f59e0b` (amber) in session detail chart, map markers
- `#ef4444` (red) in map markers

**Impact:** The portal has zero brand identity. It is visually indistinguishable from a generic admin template. Every status color is a bare Tailwind class, making global color changes impossible without touching 20+ files.

---

### 3.2 Typography -- HIGH

**Page title inconsistency:**
- `header.tsx` renders titles as `text-xl font-semibold` (via the Header component)
- `monitoring/page.tsx` renders its own title as `text-3xl font-bold` (bypasses Header)
- Some pages use Header component, some render their own heading

**Evidence (69 heading occurrences across 22 files):**
- `text-2xl font-bold` -- 24 occurrences (stat card values)
- `text-xl font-semibold` -- Header component (used by ~15 pages)
- `text-3xl font-bold` -- monitoring page, some analytics sections
- `text-lg font-semibold` -- analytics sub-sections, settings page

**CardTitle default is `text-2xl font-semibold`** (defined in `card.tsx`), but is frequently overridden inline with `text-sm font-medium` in stat cards, making the base size meaningless.

**No tabular-nums** -- Numeric values (revenue, energy, counts) use proportional figures, causing column misalignment in tables and visual jitter during live updates.

**Helper text inconsistency:**
- `text-xs text-muted-foreground` in dashboard stat cards
- `text-sm text-muted-foreground` in monitoring page descriptions
- No standard for which to use when

---

### 3.3 Component Gaps -- HIGH

**Only 4 UI primitives exist:** Button, Card, Badge, Input.

**Missing components (needed and currently reimplemented inline):**

| Component | Current Workaround | Occurrences |
|-----------|-------------------|-------------|
| **Select/Dropdown** | Raw `<select>` or Button-group filters | 8+ pages |
| **Dialog/Modal** | Overlay Card with inline close button (faults) OR conditional render (user-management) | 2 different patterns |
| **Tabs** | Button groups styled as tabs (analytics, ocpp) | 3 pages |
| **Skeleton** | "Loading..." text string | 28 occurrences across 21 files |
| **EmptyState** | Inline `<p>` or `<td>` with "No X found" | Every list page |
| **Tooltip** | `title` attribute only | Map page, monitoring |
| **FilterBar** | Reimplemented differently per page | Every list page |
| **StatCard** | Repeated Card+CardHeader+CardContent pattern | Dashboard, monitoring, sessions, analytics |
| **DataTable** | Raw `<table>` with inline styles | Sessions, payments, faults, audit-logs |
| **Pagination** | Cursor logic duplicated per page | Sessions, payments, e-invoices |

**Badge variant gaps:** The Badge component has 6 variants (`default`, `secondary`, `destructive`, `outline`, `success`, `warning`). Missing: `info` (for "Charging"/"Processing" states). The faults page works around this with an inline `className` override: `<Badge className="bg-orange-500 text-white">High</Badge>`.

---

### 3.4 Layout & Navigation -- HIGH

**Sidebar issues:**
- Brand identity: Displays `Zap` icon + "KLC" text. Should display K-Charge logo.
- 19 menu items in a flat list with no visual grouping, section labels, or separators.
- No indication of item frequency/importance -- OCPP Management (operationally critical) is buried as the 19th item.
- Alerts bell appears in both the sidebar footer AND the header, creating redundancy.
- Settings is in the sidebar footer (user section) but not in the main navigation, making it hard to discover.

**Header issues:**
- Global search input is present but non-functional (no `onChange` handler, no search logic).
- User menu button exists but has no dropdown -- just an icon that does nothing.

---

### 3.5 Branding -- CRITICAL

| Element | Current State | Expected |
|---------|--------------|----------|
| Primary color | Blue (#5B87D9) | K-Charge Green (#28A649) |
| Accent color | None | K-Charge Orange |
| Sidebar logo | Lucide `Zap` icon + "KLC" | K-Charge logo (SVG/image) |
| Login page | Generic gradient from blue/10 | Branded with K-Charge identity |
| Favicon | Next.js default | K-Charge icon |
| Page title | "KLC Admin" | "K-Charge Admin" or client-specified name |
| Charts | Blue/green hex codes | Brand palette |
| Maps | Green/blue/amber/red markers | Brand-derived status colors |

The portal currently has the visual personality of a Tailwind CSS template starter. There is no element that identifies it as a K-Charge product.

---

### 3.6 UX Issues -- MEDIUM

1. **No breadcrumbs** -- Station Detail, Session Detail, Fault Detail, Payment Detail pages have no navigation context. Users must use browser back button.

2. **No keyboard shortcuts** -- No shortcut for global search, navigation, or common actions. Power users (operators monitoring 24/7) have no accelerators.

3. **Inconsistent status mapping** -- "Faulted" is `warning` variant on stations page but `destructive` variant on monitoring page. "Available" is `success` everywhere except the Badge component default which maps to the blue primary.

4. **No error boundaries** -- React errors in any page component will crash the entire dashboard shell. No graceful error recovery.

5. **No optimistic updates** -- Mutations (enable/disable station, resolve fault, refund payment) show no immediate feedback. Users must wait for query refetch.

6. **Date formatting inconsistency:**
   - Dashboard: `new Date(...).toLocaleString("vi-VN")`
   - Sessions: `formatDateTime()` utility
   - Monitoring: `new Date(...).toLocaleTimeString()` (no locale)
   - Analytics: Custom `formatDate()` function (`d/M` format)

7. **No global search** -- The header search input exists visually but has no implementation.

---

### 3.7 Technical Debt -- LOW-MEDIUM

1. **Dark mode CSS variables defined but unused** -- `globals.css` has a `.dark` block with 14 variables, but there is no theme toggle anywhere in the UI. These variables are untested.

2. **Empty directories:**
   - `src/components/stations/` -- exists but is empty (station components are inline in pages)
   - `src/types/` -- exists but is empty (types are defined inline in each page)

3. **All UI strings hardcoded** -- Violates AP-004 (i18n anti-pattern). Every label, button text, empty state message, and status label is a hardcoded English string. No `IStringLocalizer` equivalent, no i18n library installed. The project requires Vietnamese as default locale.

4. **No form validation library** -- No Zod, Yup, or react-hook-form. The station create/edit form and login form use basic HTML `required` attributes only. No client-side validation for phone numbers, coordinates, tariff rates, etc.

5. **Duplicated status label maps** -- `StationStatusLabels`, `SessionStatusLabels`, `FaultStatusLabels`, `PaymentStatusLabels` are each defined inline in their respective page files. These should be shared constants.

6. **No `Suspense` boundaries** -- Only the login page uses `Suspense`. All other pages have no loading boundary, meaning TanStack Query's `isLoading` state is the only loading indicator.

---

## 4. Priority Remediation Plan

### P0 -- Brand Identity (Week 1)

**Goal:** The portal should be immediately recognizable as a K-Charge product.

| Task | File(s) | Effort |
|------|---------|--------|
| Replace `--primary` HSL value with K-Charge green (#28A649 = `145 62% 40%`) | `globals.css` | 5 min |
| Add `--brand-green`, `--brand-orange`, `--brand-green-light` CSS variables | `globals.css` | 15 min |
| Replace Zap icon + "KLC" with K-Charge SVG logo in sidebar | `sidebar.tsx` | 30 min |
| Update login page icon and gradient to use brand colors | `login/page.tsx` | 15 min |
| Add K-Charge favicon | `app/favicon.ico` | 5 min |

### P0 -- Semantic Color Tokens (Week 1)

**Goal:** Replace all 108 hardcoded color class occurrences with semantic tokens.

Add to `globals.css`:
```css
--status-available: 145 62% 40%;     /* green */
--status-charging: 217 91% 60%;      /* blue */
--status-faulted: 0 84% 60%;         /* red */
--status-offline: 215 16% 47%;       /* gray */
--status-warning: 38 92% 50%;        /* amber */
--status-suspended: 262 83% 58%;     /* purple */
--chart-1: 145 62% 40%;
--chart-2: 217 91% 60%;
--chart-3: 38 92% 50%;
--chart-4: 0 84% 60%;
--chart-5: 262 83% 58%;
```

Then update Tailwind config to map these to utility classes and replace inline colors in all 20 affected files.

### P1 -- Status Badge System (Week 2)

**Goal:** One function maps any domain status to the correct Badge variant. No per-page switch statements.

| Task | Effort |
|------|--------|
| Create `lib/status.ts` with `getStationStatusBadge()`, `getConnectorStatusBadge()`, `getSessionStatusBadge()`, `getFaultStatusBadge()`, `getPaymentStatusBadge()` | 2 hours |
| Add Badge variants: `info`, `critical`, `offline` | 30 min |
| Replace per-page `getStatusBadge()` functions (stations, sessions, faults, payments, monitoring, e-invoices, alerts) | 3 hours |

### P1 -- Navigation Grouping (Week 2)

**Goal:** Reduce cognitive load from 19 flat items to 5 grouped sections with 12 top-level items.

See `docs/design/admin-portal-ia.md` for the proposed information architecture.

| Task | Effort |
|------|--------|
| Restructure `menuItems` array with section labels | 1 hour |
| Add section header rendering to sidebar nav loop | 1 hour |
| Merge pages: Station Map into Stations, OCPP into Monitoring, Vouchers + Promotions into Marketing, User Management + Mobile Users into Users, Analytics + Audit Logs into Reports, Faults + Alerts into one page, Payments + E-Invoices into one page | 8 hours |

### P1 -- Missing Components (Week 2-3)

| Component | Priority | Effort |
|-----------|----------|--------|
| `Skeleton` (replaces 28 "Loading..." strings) | Immediate | 1 hour |
| `EmptyState` (icon + message + optional action) | Immediate | 1 hour |
| `FilterBar` (search + status filter + date range) | High | 3 hours |
| `StatCard` (icon + label + value + subtext) | High | 1 hour |
| `Select` (styled dropdown) | High | 2 hours |
| `Dialog` (modal with overlay) | High | 2 hours |
| `Tabs` (styled tab group) | Medium | 1 hour |
| `DataTable` (sortable, with pagination built-in) | Medium | 4 hours |
| `Tooltip` (hover info) | Low | 1 hour |
| `Breadcrumb` | Low | 1 hour |

### P2 -- Typography Scale (Week 3)

**Goal:** Consistent heading hierarchy and numeric formatting.

| Rule | Class | Usage |
|------|-------|-------|
| Page title (in Header) | `text-xl font-semibold` | All pages via Header component |
| Section title | `text-lg font-semibold` | Card groups, page sections |
| Card title | `text-base font-medium` | Override CardTitle default from `text-2xl` |
| Stat value | `text-2xl font-bold tabular-nums` | KPI cards |
| Table header | `text-sm font-medium` | All data tables |
| Body text | `text-sm` | Default |
| Helper/caption | `text-xs text-muted-foreground` | Timestamps, sub-labels |
| Numeric data | Add `tabular-nums` | All numbers in tables, stats, charts |

Action: Update `card.tsx` CardTitle default from `text-2xl` to `text-base`. Add `tabular-nums` to a global utility class.

### P2 -- Page Consistency (Week 3-4)

| Pattern | Standard | Pages Affected |
|---------|----------|---------------|
| Page header | Always use `<Header>` component | Monitoring (currently custom h1) |
| Filter bar | Use `<FilterBar>` component | Sessions, payments, faults, stations, alerts, e-invoices, audit-logs, vouchers, promotions, maintenance |
| Loading state | Use `<Skeleton>` component | All 21 pages with "Loading..." |
| Empty state | Use `<EmptyState>` component | All list pages |
| Modal/dialog | Use `<Dialog>` component | User-management, faults, payments (refund) |
| Status badges | Use shared `getXxxStatusBadge()` | 7 pages with per-page badge logic |

### P3 -- UX Enhancements (Week 4+)

| Feature | Effort | Impact |
|---------|--------|--------|
| Breadcrumbs on detail pages | 2 hours | Navigation context |
| Global search implementation | 4 hours | Discoverability |
| Keyboard shortcuts (Cmd+K search, G+D dashboard, G+M monitoring) | 3 hours | Power user efficiency |
| Error boundaries per route segment | 2 hours | Crash resilience |
| Optimistic updates on mutations | 3 hours | Perceived performance |
| i18n setup (next-intl or similar) | 8 hours | AP-004 compliance |
| Form validation with Zod + react-hook-form | 6 hours | Data integrity |
| Delete dark mode CSS or implement toggle | 1 hour | Clean up dead code |
| Extract shared types to `src/types/` | 2 hours | Clean up empty directory |
| Extract station components to `src/components/stations/` | 3 hours | Clean up empty directory |

---

## 5. Metrics for Success

| Metric | Current | Target |
|--------|---------|--------|
| Hardcoded color classes | 108 | 0 |
| "Loading..." text strings | 28 | 0 |
| Per-page status badge functions | 7 | 0 (shared) |
| Flat nav items | 19 | 12 grouped |
| UI primitives | 4 | 14+ |
| Brand-identifiable elements | 0 | 5+ (logo, colors, favicon, login, charts) |
| Hardcoded UI strings | ~500+ | 0 (i18n) |
| Unique heading styles | 6+ | 3 (standardized) |

---

## Appendix A: File-Level Color Audit

Files with the most hardcoded color classes (inline Tailwind status colors):

| File | Count | Primary Issue |
|------|-------|--------------|
| `groups/page.tsx` | 16 | Station/connector status colors |
| `e-invoices/page.tsx` | 14 | Invoice status + payment status colors |
| `monitoring/page.tsx` | 13 | Station status + connector status bars |
| `ocpp/page.tsx` | 10 | Charger status + command result colors |
| `alerts/page.tsx` | 9 | Alert severity colors |
| `maintenance/page.tsx` | 7 | Task status colors |
| `analytics/page.tsx` | 3 + 4 hex | Chart colors + uptimeColorClass function |
| `map/page.tsx` | 0 classes + 7 hex | All color in marker/popup hex codes |

## Appendix B: Status-to-Color Mapping Inconsistencies

| Status | stations/ | monitoring/ | sessions/ | faults/ |
|--------|----------|-------------|-----------|---------|
| Available/Online | `success` (green) | `success` (green) | -- | -- |
| Faulted | `warning` (yellow) | `destructive` (red) | -- | -- |
| Offline | `destructive` (red) | `secondary` (gray) | -- | -- |
| Charging/InProgress | `default` (blue) | `default` (blue) | `default` (blue) | -- |
| Open | -- | -- | -- | `destructive` (red) |
| High severity | -- | -- | -- | `bg-orange-500` (inline) |

The "Faulted" status maps to yellow on the stations page but red on the monitoring page. "Offline" maps to red on stations but gray on monitoring. These inconsistencies confuse operators who view both pages.
