# K-Charge Admin Portal — Frontend UI Refactoring Plan

**Project:** KLC EV Charging Station Management System (CSMS)
**Module:** Admin Portal (`src/admin-portal/`)
**Stack:** Next.js 16, React 19, Tailwind CSS 4, TypeScript
**Objective:** Rebrand from generic blue shadcn/ui theme to K-Charge identity (green #28A649 + orange #FAA623). Establish a design token system, create reusable status/data components, and systematically update all 28 pages for visual consistency.

**Total Estimated Effort:** 7–9 days
**Start Date:** TBD
**Owner:** TBD

---

## Current State Analysis

### Theme
- `globals.css` uses shadcn/ui defaults with blue primary (`221.2 83.2% 53.3%` = `#3B82F6`)
- Tailwind 4 `@theme inline` block maps CSS variables to `--color-*` tokens
- No brand-specific variables; no status color tokens
- Page background is pure white (`0 0% 100%`)

### Components (4 UI + 2 Layout)
- **Button** (`src/components/ui/button.tsx`): 6 variants — `default`, `destructive`, `outline`, `secondary`, `ghost`, `link`. No brand/green variant.
- **Badge** (`src/components/ui/badge.tsx`): 6 variants — `default`, `secondary`, `destructive`, `outline`, `success` (hardcoded `bg-green-500`), `warning` (hardcoded `bg-yellow-500`).
- **Card** (`src/components/ui/card.tsx`): Standard card/header/content/footer. Uses tokens correctly.
- **Input** (`src/components/ui/input.tsx`): With label/error/hint/suffix. Focus ring uses `--ring` (blue).
- **Sidebar** (`src/components/layout/sidebar.tsx`): 20 flat menu items (no grouping), Zap icon branding, active state = blue `bg-primary`.
- **Header** (`src/components/layout/header.tsx`): Page title + search + alerts bell (duplicated in sidebar).

### Patterns Observed Across Pages
- **Status badges are duplicated per page**: `getStatusBadge()` functions exist in `sessions/page.tsx`, `faults/page.tsx`, `monitoring/page.tsx`, etc. — each with inline switch statements mapping enum values to Badge variants. No single source of truth.
- **Status color maps are duplicated**: `StationStatusMap`, `SessionStatusLabels`, `FaultStatusLabels` etc. are redefined in each page file.
- **KPI cards are manually constructed**: Dashboard uses raw `Card` + `CardHeader` + `CardContent` with repeated layout markup. No dedicated stat component.
- **108 hardcoded Tailwind color classes** across 23 files (e.g., `text-green-600`, `bg-orange-500`, `text-red-600`, `text-blue-600`). These bypass the token system.
- **No loading skeletons**: Pages show nothing or a spinner while data loads.
- **No empty state component**: Pages use inline `<p>` tags for "no data" messages.
- **No filter bar pattern**: Each page builds its own search/filter layout with inconsistent spacing.
- **No dialog/modal component**: User management and other pages that need modals are either using custom implementations or navigating to separate pages.

---

## Phase 1: Design Foundation

**Priority:** P0 | **Effort:** 1 day | **Risk:** Low (CSS-only, no logic changes)

Changes in Phase 1 propagate instantly to all pages via CSS variable inheritance.

### 1.1 Theme Token System

**File:** `src/admin-portal/src/app/globals.css`

Replace the `:root` block and `@theme` block with K-Charge brand tokens.

#### Brand Colors (hex reference)
| Token | Hex | HSL | Usage |
|-------|-----|-----|-------|
| Brand Primary (Green) | `#28A649` | `136 62% 40%` | Primary actions, active states, positive indicators |
| Brand Secondary (Orange) | `#FAA623` | `39 96% 56%` | Accents, highlights, secondary CTAs |
| Brand Primary Light | `#E8F5EC` | `136 40% 93%` | Hover backgrounds, tinted surfaces |
| Brand Secondary Light | `#FFF4E0` | `39 100% 94%` | Warning tint backgrounds |

#### CSS Variable Changes in `:root`

```css
:root {
  /* Brand */
  --brand-primary: 136 62% 40%;          /* #28A649 — K-Charge Green */
  --brand-primary-foreground: 0 0% 100%; /* White text on green */
  --brand-secondary: 39 96% 56%;         /* #FAA623 — K-Charge Orange */
  --brand-secondary-foreground: 0 0% 100%;

  /* Override shadcn defaults to use brand green as primary */
  --primary: 136 62% 40%;
  --primary-foreground: 0 0% 100%;
  --ring: 136 62% 40%;

  /* Keep secondary as neutral gray (for secondary buttons, etc.) */
  --secondary: 210 40% 96.1%;
  --secondary-foreground: 222.2 47.4% 11.2%;

  /* Page background — very slight green tint */
  --background: 180 14% 98%;             /* #F8FAFB */
  --foreground: 222.2 84% 4.9%;

  /* Card stays white */
  --card: 0 0% 100%;
  --card-foreground: 222.2 84% 4.9%;

  /* Popover */
  --popover: 0 0% 100%;
  --popover-foreground: 222.2 84% 4.9%;

  /* Accent — light green tint for hover states */
  --accent: 136 30% 95%;
  --accent-foreground: 136 62% 25%;

  /* Muted */
  --muted: 210 40% 96.1%;
  --muted-foreground: 215.4 16.3% 46.9%;

  /* Destructive stays red */
  --destructive: 0 84.2% 60.2%;
  --destructive-foreground: 0 0% 100%;

  /* Border / Input */
  --border: 214.3 31.8% 91.4%;
  --input: 214.3 31.8% 91.4%;

  --radius: 0.5rem;

  /* === Semantic Status Colors === */

  /* Station Status */
  --status-available: 136 62% 40%;       /* Green — #28A649 */
  --status-occupied: 217 91% 60%;        /* Blue — #3B82F6 */
  --status-charging: 217 91% 60%;        /* Blue — same as occupied */
  --status-offline: 215 14% 55%;         /* Gray — #808B96 */
  --status-faulted: 0 84% 60%;           /* Red — #EF4444 */
  --status-unavailable: 39 96% 56%;      /* Orange — #FAA623 */
  --status-decommissioned: 215 14% 75%;  /* Light gray */

  /* Session Status */
  --status-pending: 215 14% 55%;         /* Gray */
  --status-starting: 217 91% 60%;        /* Blue */
  --status-in-progress: 136 62% 40%;     /* Green */
  --status-suspended: 39 96% 56%;        /* Orange */
  --status-stopping: 215 14% 55%;        /* Gray */
  --status-completed: 136 62% 40%;       /* Green */
  --status-failed: 0 84% 60%;            /* Red */

  /* Payment Status */
  --status-paid: 136 62% 40%;            /* Green */
  --status-payment-pending: 39 96% 56%;  /* Orange */
  --status-payment-failed: 0 84% 60%;    /* Red */
  --status-refunded: 217 91% 60%;        /* Blue */

  /* Severity */
  --severity-critical: 0 84% 60%;        /* Red */
  --severity-high: 24 95% 53%;           /* Dark orange */
  --severity-medium: 39 96% 56%;         /* Orange */
  --severity-low: 215 14% 55%;           /* Gray */

  /* Semantic Colors */
  --color-success: 136 62% 40%;          /* Green */
  --color-warning: 39 96% 56%;           /* Orange */
  --color-danger: 0 84% 60%;             /* Red */
  --color-info: 217 91% 60%;             /* Blue */

  /* Surface tints (for status background fills) */
  --surface-success: 136 40% 93%;        /* Light green */
  --surface-warning: 39 100% 94%;        /* Light orange */
  --surface-danger: 0 86% 95%;           /* Light red */
  --surface-info: 217 91% 95%;           /* Light blue */
  --surface-neutral: 210 40% 96%;        /* Light gray */
}
```

#### `@theme inline` Block Update

Add the new tokens to the Tailwind `@theme` block so they are usable as utility classes:

```css
@theme inline {
  /* Existing mappings (keep) */
  --color-background: hsl(var(--background));
  --color-foreground: hsl(var(--foreground));
  --color-card: hsl(var(--card));
  --color-card-foreground: hsl(var(--card-foreground));
  --color-popover: hsl(var(--popover));
  --color-popover-foreground: hsl(var(--popover-foreground));
  --color-primary: hsl(var(--primary));
  --color-primary-foreground: hsl(var(--primary-foreground));
  --color-secondary: hsl(var(--secondary));
  --color-secondary-foreground: hsl(var(--secondary-foreground));
  --color-muted: hsl(var(--muted));
  --color-muted-foreground: hsl(var(--muted-foreground));
  --color-accent: hsl(var(--accent));
  --color-accent-foreground: hsl(var(--accent-foreground));
  --color-destructive: hsl(var(--destructive));
  --color-destructive-foreground: hsl(var(--destructive-foreground));
  --color-border: hsl(var(--border));
  --color-input: hsl(var(--input));
  --color-ring: hsl(var(--ring));
  --radius-sm: calc(var(--radius) - 4px);
  --radius-md: calc(var(--radius) - 2px);
  --radius-lg: var(--radius);
  --font-sans: var(--font-inter);

  /* New: Brand colors */
  --color-brand: hsl(var(--brand-primary));
  --color-brand-foreground: hsl(var(--brand-primary-foreground));
  --color-brand-secondary: hsl(var(--brand-secondary));
  --color-brand-secondary-foreground: hsl(var(--brand-secondary-foreground));

  /* New: Semantic colors */
  --color-success: hsl(var(--color-success));
  --color-warning: hsl(var(--color-warning));
  --color-danger: hsl(var(--color-danger));
  --color-info: hsl(var(--color-info));

  /* New: Surface tints */
  --color-surface-success: hsl(var(--surface-success));
  --color-surface-warning: hsl(var(--surface-warning));
  --color-surface-danger: hsl(var(--surface-danger));
  --color-surface-info: hsl(var(--surface-info));
  --color-surface-neutral: hsl(var(--surface-neutral));
}
```

This enables Tailwind classes like `bg-brand`, `text-success`, `bg-surface-warning`, etc.

### 1.2 Typography Tokens

**File:** `src/admin-portal/src/app/globals.css` (append after the `body` rules)

```css
/* === Typography Utilities === */

.tabular-nums {
  font-variant-numeric: tabular-nums;
}

.kpi-value {
  font-size: 1.875rem;    /* 30px */
  font-weight: 700;
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.025em;
}

.kpi-label {
  font-size: 0.875rem;    /* 14px */
  font-weight: 500;
  color: hsl(var(--muted-foreground));
}

.kpi-sublabel {
  font-size: 0.75rem;     /* 12px */
  color: hsl(var(--muted-foreground));
}

/* Type scale variables (reference) */
:root {
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  --text-3xl: 1.875rem;
}
```

### 1.3 Status System Constants

**File:** `src/admin-portal/src/lib/constants.ts` (new file)

This centralizes all status-to-display mappings currently scattered across 10+ page files.

```typescript
import {
  CheckCircle2,
  Zap,
  XCircle,
  AlertTriangle,
  Circle,
  Clock,
  Ban,
  Wrench,
  ArrowDown,
  Pause,
  CreditCard,
  RotateCcw,
  type LucideIcon,
} from "lucide-react";

// ── Types ──────────────────────────────────────────────────────────────

export type BadgeVariant =
  | "default"
  | "secondary"
  | "destructive"
  | "outline"
  | "success"
  | "warning"
  | "info"
  | "brand";

export interface StatusConfig {
  label: string;
  color: string;           // CSS variable name, e.g., "var(--status-available)"
  textClass: string;       // Tailwind text color class
  bgClass: string;         // Tailwind bg color class (surface tint)
  icon: LucideIcon;
  badgeVariant: BadgeVariant;
}

// ── Station Status ─────────────────────────────────────────────────────
// Enum: StationStatus { Offline=0, Available=1, Occupied=2, Unavailable=3, Faulted=4, Decommissioned=5 }

export const STATION_STATUS_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Offline",
    color: "var(--status-offline)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: XCircle,
    badgeVariant: "secondary",
  },
  1: {
    label: "Available",
    color: "var(--status-available)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
  2: {
    label: "Occupied",
    color: "var(--status-occupied)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Zap,
    badgeVariant: "info",
  },
  3: {
    label: "Unavailable",
    color: "var(--status-unavailable)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: Ban,
    badgeVariant: "warning",
  },
  4: {
    label: "Faulted",
    color: "var(--status-faulted)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: AlertTriangle,
    badgeVariant: "destructive",
  },
  5: {
    label: "Decommissioned",
    color: "var(--status-decommissioned)",
    textClass: "text-gray-400",
    bgClass: "bg-surface-neutral",
    icon: Ban,
    badgeVariant: "secondary",
  },
};

// ── Connector Status ───────────────────────────────────────────────────
// Enum: ConnectorStatus { Available=0, Occupied=1, Charging=2, Faulted=3, Unavailable=4, Reserved=5, Finishing=6 }

export const CONNECTOR_STATUS_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Available",
    color: "var(--status-available)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
  1: {
    label: "Occupied",
    color: "var(--status-occupied)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Circle,
    badgeVariant: "info",
  },
  2: {
    label: "Charging",
    color: "var(--status-charging)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Zap,
    badgeVariant: "info",
  },
  3: {
    label: "Faulted",
    color: "var(--status-faulted)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: AlertTriangle,
    badgeVariant: "destructive",
  },
  4: {
    label: "Unavailable",
    color: "var(--status-unavailable)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: Ban,
    badgeVariant: "warning",
  },
  5: {
    label: "Reserved",
    color: "var(--status-occupied)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Clock,
    badgeVariant: "info",
  },
  6: {
    label: "Finishing",
    color: "var(--status-available)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
};

// ── Session Status ─────────────────────────────────────────────────────
// Enum: SessionStatus { Pending=0, Starting=1, InProgress=2, Suspended=3, Stopping=4, Completed=5, Failed=6 }

export const SESSION_STATUS_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Pending",
    color: "var(--status-pending)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: Clock,
    badgeVariant: "secondary",
  },
  1: {
    label: "Starting",
    color: "var(--status-starting)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Zap,
    badgeVariant: "info",
  },
  2: {
    label: "Charging",
    color: "var(--status-in-progress)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: Zap,
    badgeVariant: "success",
  },
  3: {
    label: "Suspended",
    color: "var(--status-suspended)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: Pause,
    badgeVariant: "warning",
  },
  4: {
    label: "Stopping",
    color: "var(--status-stopping)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: ArrowDown,
    badgeVariant: "secondary",
  },
  5: {
    label: "Completed",
    color: "var(--status-completed)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
  6: {
    label: "Failed",
    color: "var(--status-failed)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: XCircle,
    badgeVariant: "destructive",
  },
};

// ── Payment Status ─────────────────────────────────────────────────────
// Enum: PaymentStatus { Pending=0, Completed=1, Failed=2, Refunded=3 }

export const PAYMENT_STATUS_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Pending",
    color: "var(--status-payment-pending)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: Clock,
    badgeVariant: "warning",
  },
  1: {
    label: "Completed",
    color: "var(--status-paid)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
  2: {
    label: "Failed",
    color: "var(--status-payment-failed)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: XCircle,
    badgeVariant: "destructive",
  },
  3: {
    label: "Refunded",
    color: "var(--status-refunded)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: RotateCcw,
    badgeVariant: "info",
  },
};

// ── Fault Severity ─────────────────────────────────────────────────────
// Backend priority: 1=Critical, 2=High, 3=Medium, 4=Low

export const FAULT_SEVERITY_CONFIG: Record<number, StatusConfig> = {
  1: {
    label: "Critical",
    color: "var(--severity-critical)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: AlertTriangle,
    badgeVariant: "destructive",
  },
  2: {
    label: "High",
    color: "var(--severity-high)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: AlertTriangle,
    badgeVariant: "warning",
  },
  3: {
    label: "Medium",
    color: "var(--severity-medium)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: AlertTriangle,
    badgeVariant: "warning",
  },
  4: {
    label: "Low",
    color: "var(--severity-low)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: Circle,
    badgeVariant: "secondary",
  },
};

// ── Fault Status ───────────────────────────────────────────────────────
// Enum: FaultStatus { Open=0, Investigating=1, Resolved=2, Closed=3 }

export const FAULT_STATUS_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Open",
    color: "var(--severity-critical)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: AlertTriangle,
    badgeVariant: "destructive",
  },
  1: {
    label: "Investigating",
    color: "var(--severity-medium)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: Wrench,
    badgeVariant: "warning",
  },
  2: {
    label: "Resolved",
    color: "var(--status-available)",
    textClass: "text-success",
    bgClass: "bg-surface-success",
    icon: CheckCircle2,
    badgeVariant: "success",
  },
  3: {
    label: "Closed",
    color: "var(--status-offline)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: Ban,
    badgeVariant: "secondary",
  },
};

// ── Alert Severity ─────────────────────────────────────────────────────
// AlertType: 0=StationOffline, 1=ConnectorFault, 2=LowUtilization, 3=HighUtilization,
//            4=FirmwareUpdate, 5=PaymentFailure, 6=EInvoiceFailure, 7=HeartbeatTimeout

export const ALERT_TYPE_CONFIG: Record<number, StatusConfig> = {
  0: {
    label: "Station Offline",
    color: "var(--severity-critical)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: XCircle,
    badgeVariant: "destructive",
  },
  1: {
    label: "Connector Fault",
    color: "var(--severity-critical)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: AlertTriangle,
    badgeVariant: "destructive",
  },
  2: {
    label: "Low Utilization",
    color: "var(--severity-low)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: Circle,
    badgeVariant: "secondary",
  },
  3: {
    label: "High Utilization",
    color: "var(--severity-medium)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: AlertTriangle,
    badgeVariant: "warning",
  },
  4: {
    label: "Firmware Update",
    color: "var(--status-starting)",
    textClass: "text-info",
    bgClass: "bg-surface-info",
    icon: Circle,
    badgeVariant: "info",
  },
  5: {
    label: "Payment Failure",
    color: "var(--severity-high)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: CreditCard,
    badgeVariant: "destructive",
  },
  6: {
    label: "E-Invoice Failure",
    color: "var(--severity-medium)",
    textClass: "text-warning",
    bgClass: "bg-surface-warning",
    icon: AlertTriangle,
    badgeVariant: "warning",
  },
  7: {
    label: "Heartbeat Timeout",
    color: "var(--severity-critical)",
    textClass: "text-danger",
    bgClass: "bg-surface-danger",
    icon: XCircle,
    badgeVariant: "destructive",
  },
};

// ── Chart Color Palette ────────────────────────────────────────────────
// For Recharts — use these as fill/stroke values

export const CHART_COLORS = {
  primary: "#28A649",      // Brand green
  secondary: "#FAA623",    // Brand orange
  info: "#3B82F6",         // Blue
  danger: "#EF4444",       // Red
  muted: "#808B96",        // Gray
  palette: [
    "#28A649",  // Green
    "#FAA623",  // Orange
    "#3B82F6",  // Blue
    "#8B5CF6",  // Purple
    "#EC4899",  // Pink
    "#14B8A6",  // Teal
    "#F97316",  // Dark orange
    "#6366F1",  // Indigo
  ],
} as const;

// ── Helper Function ────────────────────────────────────────────────────

export function getStatusConfig(
  type: "station" | "connector" | "session" | "payment" | "fault" | "faultSeverity" | "alert",
  value: number
): StatusConfig {
  const configMap = {
    station: STATION_STATUS_CONFIG,
    connector: CONNECTOR_STATUS_CONFIG,
    session: SESSION_STATUS_CONFIG,
    payment: PAYMENT_STATUS_CONFIG,
    fault: FAULT_STATUS_CONFIG,
    faultSeverity: FAULT_SEVERITY_CONFIG,
    alert: ALERT_TYPE_CONFIG,
  };

  return configMap[type][value] ?? {
    label: "Unknown",
    color: "var(--status-offline)",
    textClass: "text-gray-500",
    bgClass: "bg-surface-neutral",
    icon: Circle,
    badgeVariant: "secondary" as BadgeVariant,
  };
}
```

### 1.4 Phase 1 Completion Checklist

- [ ] `:root` CSS variables updated with brand colors
- [ ] `@theme inline` block includes new token mappings
- [ ] Dark mode `.dark` block updated to match (if dark mode is planned)
- [ ] Typography utility classes added
- [ ] `constants.ts` created with all status configs
- [ ] Verify: all existing pages render with green primary instead of blue (visual sanity check)
- [ ] Verify: no TypeScript compilation errors

---

## Phase 2: Core Components

**Priority:** P0 | **Effort:** 1–2 days | **Risk:** Low (additive, no breaking changes)

### 2.1 Update Existing Components

#### Button — Add `"brand"` variant

**File:** `src/admin-portal/src/components/ui/button.tsx`

Add to the variant type:
```typescript
variant?: "default" | "destructive" | "outline" | "secondary" | "ghost" | "link" | "brand";
```

Add to the variant classes:
```typescript
"bg-brand text-brand-foreground hover:bg-brand/90": variant === "brand",
```

Since `--primary` is now green, the `default` variant will already render green. The `brand` variant is an explicit alias for clarity and future-proofing if primary ever diverges from brand green.

#### Badge — Add status-specific variants

**File:** `src/admin-portal/src/components/ui/badge.tsx`

Update variant type:
```typescript
variant?: "default" | "secondary" | "destructive" | "outline" | "success" | "warning" | "info" | "brand";
```

Replace hardcoded colors with token-based classes:
```typescript
"border-transparent bg-success text-white hover:bg-success/80": variant === "success",
"border-transparent bg-warning text-white hover:bg-warning/80": variant === "warning",
"border-transparent bg-info text-white hover:bg-info/80": variant === "info",
"border-transparent bg-brand text-brand-foreground hover:bg-brand/80": variant === "brand",
```

This removes the hardcoded `bg-green-500` and `bg-yellow-500` values.

#### Input — Update focus ring

**File:** `src/admin-portal/src/components/ui/input.tsx`

No code change needed. The `focus:ring-ring` class already references `--ring`, which is now set to brand green in Phase 1. The ring will automatically become green.

### 2.2 New Shared Components

All new components go in `src/admin-portal/src/components/ui/`.

#### StatusBadge (`status-badge.tsx`)

Eliminates all per-page `getStatusBadge()` functions.

```typescript
// Props:
interface StatusBadgeProps {
  type: "station" | "connector" | "session" | "payment" | "fault" | "faultSeverity" | "alert";
  value: number;
  showIcon?: boolean;  // default: false
  size?: "sm" | "default";
}

// Implementation:
// - Imports getStatusConfig from constants.ts
// - Renders <Badge variant={config.badgeVariant}> with config.label
// - Optionally renders config.icon as a 3.5/4px icon before the label
// - "sm" size uses smaller text/padding
```

Usage example: `<StatusBadge type="session" value={2} showIcon />` renders a green "Charging" badge with a Zap icon.

#### StatCard (`stat-card.tsx`)

Replaces the manual Card+CardHeader+CardContent pattern used for KPI metrics on the Dashboard and Analytics pages.

```typescript
interface StatCardProps {
  label: string;
  value: string | number;
  icon: LucideIcon;
  iconColor?: string;       // Tailwind text color class, default "text-muted-foreground"
  sublabel?: string;
  trend?: {
    value: number;           // Percentage, e.g., 12.5
    direction: "up" | "down" | "flat";
  };
  className?: string;
}

// Implementation:
// - Renders <Card> with icon top-right, label below, large value using .kpi-value class
// - Sublabel uses .kpi-sublabel class
// - Trend renders a green up-arrow or red down-arrow with percentage
```

#### FilterBar (`filter-bar.tsx`)

Standardizes the search + filter + action button layout used on every list page.

```typescript
interface FilterBarProps {
  searchValue: string;
  onSearchChange: (value: string) => void;
  searchPlaceholder?: string;
  filters?: React.ReactNode;    // Slot for filter dropdowns/chips
  actions?: React.ReactNode;    // Slot for action buttons (e.g., "Create New")
  className?: string;
}

// Implementation:
// - Horizontal flex container with gap-4
// - Search input with Search icon (left icon), uses <Input> component
// - {filters} slot renders inline
// - {actions} slot renders at the end (ml-auto)
// - Responsive: stacks vertically on mobile
```

#### EmptyState (`empty-state.tsx`)

```typescript
interface EmptyStateProps {
  icon?: LucideIcon;           // Default: Inbox
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

// Implementation:
// - Centered layout: icon (48px, text-muted-foreground), title (text-lg font-medium), description (text-sm text-muted-foreground), optional Button CTA
// - Minimum height: 200px
```

#### Skeleton (`skeleton.tsx`)

```typescript
interface SkeletonProps {
  variant?: "rectangle" | "circle" | "text";
  width?: string;              // CSS width, default "100%"
  height?: string;             // CSS height, default "1rem"
  className?: string;
}

// Also export compound components:
// - SkeletonCard: Card-shaped skeleton for stat cards
// - SkeletonTable: Table rows skeleton for data tables
// - SkeletonText: Multiple lines of text skeleton

// Implementation:
// - Animated pulse background using Tailwind animate-pulse
// - bg-muted rounded-md for rectangle/text
// - rounded-full for circle
```

#### DataTable (`data-table.tsx`)

Standardizes the table pattern used across sessions, payments, faults, audit-logs, etc.

```typescript
interface Column<T> {
  key: string;
  header: string;
  render: (item: T) => React.ReactNode;
  className?: string;          // Column-specific classes (e.g., "text-right tabular-nums")
  sortable?: boolean;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  isLoading?: boolean;
  emptyState?: EmptyStateProps;
  onRowClick?: (item: T) => void;
  rowKey: (item: T) => string;
  pagination?: {
    hasNext: boolean;
    hasPrev: boolean;
    onNext: () => void;
    onPrev: () => void;
    pageLabel?: string;        // e.g., "Page 2"
  };
  className?: string;
}

// Implementation:
// - Renders <table> with thead/tbody
// - Loading state: <SkeletonTable> with same column count
// - Empty state: <EmptyState> spanning full width
// - Row hover: bg-accent/50 cursor-pointer (if onRowClick provided)
// - Pagination footer with prev/next buttons
// - Responsive: horizontal scroll wrapper on mobile
```

#### PageHeader (`page-header.tsx`)

```typescript
interface PageHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: Array<{ label: string; href?: string }>;
  actions?: React.ReactNode;   // Slot for action buttons
}

// Implementation:
// - Replaces the current Header component for page-level use
// - Renders breadcrumbs (if provided) above the title
// - Title + description on the left, actions on the right
// - Sticky positioning with backdrop blur (same as current Header)
```

#### Dialog (`dialog.tsx`)

```typescript
interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  size?: "sm" | "default" | "lg" | "xl";  // max-w-sm/md/lg/xl
  children: React.ReactNode;
  footer?: React.ReactNode;
}

// Implementation:
// - Portal-rendered overlay (fixed inset-0, bg-black/50, backdrop-blur-sm)
// - Centered Card with title, close (X) button, children content, optional footer
// - Escape key closes
// - Click outside closes
// - Focus trap within dialog
// - Transition: fade + scale animation
```

#### Select (`select.tsx`)

```typescript
interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface SelectProps {
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label?: string;
  error?: string;
  className?: string;
}

// Implementation:
// - Native <select> element styled to match Input component
// - Same height (h-10), border, focus ring as Input
// - Chevron icon on the right
// - Optional label and error display (same pattern as Input)
```

#### Tabs (`tabs.tsx`)

```typescript
interface Tab {
  id: string;
  label: string;
  count?: number;              // Optional count badge
}

interface TabsProps {
  tabs: Tab[];
  activeTab: string;
  onChange: (tabId: string) => void;
  className?: string;
}

// Implementation:
// - Underline-style horizontal tab bar
// - Active tab: brand-green bottom border (2px), font-semibold, text-foreground
// - Inactive: text-muted-foreground, hover:text-foreground
// - Optional count renders as a small Badge next to the label
// - Responsive: horizontal scroll on mobile
```

### 2.3 Update Layout Components

#### Sidebar Refactor

**File:** `src/admin-portal/src/components/layout/sidebar.tsx`

**Changes:**

1. **Branding:** Replace `<Zap>` icon with K-Charge text logo or SVG. Update `<span className="text-lg font-bold">KLC</span>` to `<span className="text-lg font-bold text-brand">K-Charge</span>`.

2. **Group 20 items into 5 labeled sections:**

```typescript
const sidebarSections = [
  {
    label: "Overview",
    items: [
      { href: "/", label: "Dashboard", icon: LayoutDashboard },
      { href: "/analytics", label: "Analytics", icon: BarChart3 },
    ],
  },
  {
    label: "Operations",
    items: [
      { href: "/stations", label: "Stations", icon: MapPin },
      { href: "/map", label: "Station Map", icon: Map },
      { href: "/monitoring", label: "Monitoring", icon: Activity },
      { href: "/sessions", label: "Sessions", icon: Zap },
      { href: "/ocpp", label: "OCPP", icon: Plug },
    ],
  },
  {
    label: "Incidents",
    items: [
      { href: "/faults", label: "Faults", icon: AlertTriangle },
      { href: "/maintenance", label: "Maintenance", icon: Wrench },
    ],
  },
  {
    label: "Business",
    items: [
      { href: "/tariffs", label: "Tariffs", icon: DollarSign },
      { href: "/payments", label: "Payments", icon: Receipt },
      { href: "/e-invoices", label: "E-Invoices", icon: FileText },
      { href: "/vouchers", label: "Marketing", icon: Ticket },
      // Merge: Promotions page accessible via Marketing tab or sub-route
    ],
  },
  {
    label: "Users & System",
    items: [
      { href: "/mobile-users", label: "Mobile Users", icon: Smartphone },
      { href: "/vehicles", label: "Vehicles", icon: Car },
      { href: "/user-management", label: "Users & Roles", icon: Users },
      { href: "/groups", label: "Station Groups", icon: FolderTree },
      { href: "/audit-logs", label: "Audit Logs", icon: FileText },
      { href: "/feedback", label: "Feedback", icon: MessageSquare },
    ],
  },
];
```

3. **Render section labels** between groups (when not collapsed):
```tsx
{!isCollapsed && (
  <div className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
    {section.label}
  </div>
)}
```

4. **Active state:** Change from `bg-primary text-primary-foreground` (solid fill) to a subtler `bg-brand/10 text-brand font-medium border-l-2 border-brand` (green left border + light green background).

5. **Move Alerts and Settings into the main navigation** (Alerts under "Incidents", Settings as a bottom icon-only item alongside Logout).

#### Header Refactor

**File:** `src/admin-portal/src/components/layout/header.tsx`

**Changes:**

1. **Remove duplicate alerts bell** — keep alerts in sidebar only. The header alert bell is redundant since Alerts already has a dedicated sidebar link with unread badge.

2. **Add breadcrumbs** below the title:
```tsx
<nav className="flex items-center gap-1 text-xs text-muted-foreground">
  <Link href="/" className="hover:text-foreground">Dashboard</Link>
  <ChevronRight className="h-3 w-3" />
  <span className="text-foreground">{title}</span>
</nav>
```

3. **Update search input** to use the `<Input>` component (consistency).

### 2.4 Phase 2 Completion Checklist

- [ ] Button: `brand` variant added
- [ ] Badge: `info` and `brand` variants added, hardcoded colors removed
- [ ] StatusBadge component created and working
- [ ] StatCard component created and working
- [ ] FilterBar component created and working
- [ ] EmptyState component created and working
- [ ] Skeleton + compounds created and working
- [ ] DataTable component created and working
- [ ] PageHeader component created and working
- [ ] Dialog component created and working
- [ ] Select component created and working
- [ ] Tabs component created and working
- [ ] Sidebar regrouped into 5 sections with labels
- [ ] Sidebar active state updated to green tint
- [ ] Sidebar branding updated (K-Charge)
- [ ] Header alerts bell removed or deduplicated
- [ ] Header breadcrumbs added
- [ ] All new components export from their files
- [ ] No TypeScript compilation errors

---

## Phase 3: Priority Page Refactor

**Priority:** P1 | **Effort:** 2–3 days | **Risk:** Medium (logic preserved, layout changes)

Refactor pages in dependency order. Each page refactor follows the same pattern:
1. Replace inline status badge functions with `<StatusBadge>`
2. Replace manual KPI cards with `<StatCard>`
3. Replace inline search/filter layouts with `<FilterBar>`
4. Replace raw `<table>` or list layouts with `<DataTable>`
5. Add `<Skeleton>` loading states
6. Add `<EmptyState>` for empty data
7. Replace hardcoded color classes with token classes
8. Replace `<Header>` with `<PageHeader>` (if breadcrumbs are useful)

### 3.1 Login Page

**File:** `src/admin-portal/src/app/login/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| Background gradient | `from-primary/10` (blue tint) | `from-brand/5 to-background` (subtle green) |
| Logo icon | `<Zap>` in blue circle | K-Charge logo or `<Zap>` in green circle (`bg-brand`) |
| Title | "KLC Admin" | "K-Charge Admin" |
| Submit button | `bg-primary` (already green after Phase 1) | Confirmed green, no extra change |
| Loading spinner | `border-primary` (already green) | Confirmed green |

Minimal code changes since Phase 1 tokens already make `--primary` green.

### 3.2 Dashboard

**File:** `src/admin-portal/src/app/(dashboard)/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| 4 KPI cards | Manual Card+CardHeader+CardContent | `<StatCard icon={MapPin} label="Total Stations" value={data.totalStations} sublabel="12 online" />` |
| Bar chart fill | `fill="hsl(var(--primary))"` (now green) | Use multi-color: available=green, charging=blue, faulted=red from `CHART_COLORS` |
| Station overview values | Hardcoded `text-green-600`, `text-gray-600`, `text-red-600`, `text-blue-600` | Use `STATION_STATUS_CONFIG[n].textClass` |
| Recent alerts badges | Inline `isCritical` logic + Badge variant | `<StatusBadge type="alert" value={alert.type} />` |
| Loading state | No skeleton | Add `<SkeletonCard>` grid while `isLoading` |
| "No recent alerts" | Inline `<p>` | `<EmptyState icon={Bell} title="No recent alerts" description="Your charging network is running smoothly" />` |

### 3.3 Monitoring

**File:** `src/admin-portal/src/app/(dashboard)/monitoring/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| `StationStatusMap` dict | Inline at top of file | Import from `constants.ts` (`STATION_STATUS_CONFIG`) |
| Station status colors | Hardcoded green/red/gray classes | `STATION_STATUS_CONFIG[status].textClass` |
| Connector status badges | Inline color logic | `<StatusBadge type="connector" value={status} showIcon />` |
| Real-time event badges | Inline badge variant selection | `<StatusBadge>` using appropriate type |
| KPI cards at top | Manual layout | `<StatCard>` components |
| Connection indicator | Custom component (keep) | Keep, but update colors to tokens |

### 3.4 Stations (List + Detail)

**Files:**
- `src/admin-portal/src/app/(dashboard)/stations/page.tsx`
- `src/admin-portal/src/app/(dashboard)/stations/[id]/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| Search + filter layout | Inline | `<FilterBar>` with status dropdown via `<Select>` |
| Station list | Manual table/card layout | `<DataTable>` with columns for name, address, status, connectors, last heartbeat |
| Status display | Inline badges | `<StatusBadge type="station" value={status} showIcon />` |
| Connector status array | Inline color logic | `<StatusBadge type="connector" value={connectorStatus} size="sm" />` |
| Empty state | Inline text | `<EmptyState icon={MapPin} title="No stations found" action={{ label: "Add Station", onClick: ... }} />` |
| Detail page tabs | If any | Use `<Tabs>` component |
| Loading state | None or spinner | `<SkeletonTable>` for list, `<SkeletonCard>` for detail |

### 3.5 Sessions

**File:** `src/admin-portal/src/app/(dashboard)/sessions/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| `SessionStatusLabels` dict | Inline at top of file | Remove, use `SESSION_STATUS_CONFIG` from constants |
| `getStatusBadge()` function | 20-line inline switch | Remove, use `<StatusBadge type="session" value={status} />` |
| Search + filter | Inline layout | `<FilterBar>` |
| Session table | Manual layout | `<DataTable>` |
| Currency values | Already using `formatCurrency()` | Add `tabular-nums` class to value columns |
| Energy values | Already using `formatEnergy()` | Add `tabular-nums` class |
| Pagination | Manual prev/next buttons | `<DataTable pagination={...}>` |
| Loading | Inline loading check | `<DataTable isLoading={isLoading}>` renders `<SkeletonTable>` |

### 3.6 Faults & Alerts

**Files:**
- `src/admin-portal/src/app/(dashboard)/faults/page.tsx`
- `src/admin-portal/src/app/(dashboard)/faults/[id]/page.tsx`
- `src/admin-portal/src/app/(dashboard)/alerts/page.tsx`

| Change | Current | Target |
|--------|---------|--------|
| `FaultStatusLabels`, `getStatusBadge()`, `getSeverityBadge()` | 55 lines of inline functions | Remove all, use `<StatusBadge type="fault" value={status} />` and `<StatusBadge type="faultSeverity" value={priority} />` |
| Hardcoded `bg-orange-500 text-white` for "High" severity | Badge with inline className override | `<StatusBadge type="faultSeverity" value={2} />` renders warning variant |
| Alert type badges | Inline `alertTypeMap` + `isCritical` logic | `<StatusBadge type="alert" value={alertType} />` |
| Fault detail page | Keep layout, update badges | Replace inline badges with `<StatusBadge>` |
| Summary cards (Open/Investigating/Resolved counts) | Manual Card layout | `<StatCard>` with severity icon colors |

### 3.7 Phase 3 Completion Checklist

- [ ] Login: brand gradient, logo, confirmed green button
- [ ] Dashboard: StatCard x4, chart colors, StatusBadge for alerts, skeleton loading, empty state
- [ ] Monitoring: constants import, StatusBadge, StatCard, token colors
- [ ] Stations list: FilterBar, DataTable, StatusBadge, EmptyState, Skeleton
- [ ] Station detail: StatusBadge, Tabs, token colors
- [ ] Sessions: remove inline status maps, DataTable, StatusBadge, tabular-nums, FilterBar
- [ ] Faults list: remove 55 lines of inline functions, DataTable, StatusBadge, FilterBar
- [ ] Fault detail: StatusBadge for status and severity
- [ ] Alerts: StatusBadge, FilterBar
- [ ] All hardcoded color classes in these pages replaced with token classes
- [ ] No functional regressions (all API calls, navigation, mutations preserved)

---

## Phase 4: Remaining Pages

**Priority:** P2 | **Effort:** 2 days | **Risk:** Low (same patterns as Phase 3)

### 4.1 Payments

**Files:** `payments/page.tsx`, `payments/[id]/page.tsx`

- Replace inline payment status badges with `<StatusBadge type="payment" value={status} />`
- KPI cards (total revenue, pending, completed) with `<StatCard>`
- Table with `<DataTable>`
- Currency columns: `tabular-nums` class
- Remove hardcoded color classes (approximately 5 instances)

### 4.2 Tariffs

**File:** `tariffs/page.tsx`

- Active tariff indicator: `text-brand` or `<Badge variant="brand">Active</Badge>`
- Table with `<DataTable>`
- Price columns: `tabular-nums`
- Remove hardcoded green for active tariff

### 4.3 User Management

**File:** `user-management/page.tsx`

- Replace any inline modal with `<Dialog>`
- Role assignment UI: use `<Tabs>` for Users vs Roles view
- Use `<DataTable>` for user list
- Use `<FilterBar>` for search/filter
- Remove hardcoded color classes (approximately 2 instances)

### 4.4 Audit Logs

**File:** `audit-logs/page.tsx`

- `<DataTable>` for log entries
- `<FilterBar>` with date range, user filter, action type filter
- Remove hardcoded color classes (approximately 2 instances)

### 4.5 Other Pages

| Page | Key Changes |
|------|-------------|
| **Maintenance** | StatusBadge for maintenance status, DataTable, remove ~7 hardcoded colors |
| **E-Invoices** | StatusBadge for invoice status, DataTable, remove ~11 hardcoded colors |
| **Groups** | StatusBadge for group station statuses, remove ~10 hardcoded colors |
| **Settings** | Minimal changes — update any blue-tinted sections to brand |
| **Vehicles** | DataTable, remove ~1 hardcoded color |
| **Mobile Users** | DataTable, remove ~1 hardcoded color |
| **Feedback** | StatusBadge for feedback status, DataTable |
| **Vouchers** | StatusBadge for voucher status, DataTable, merge with Promotions |
| **Promotions** | StatusBadge, DataTable. Consider merging into Vouchers as a tab |
| **OCPP** | Status indicators for connected chargers, remove ~10 hardcoded colors |
| **Analytics** | Chart palette update to `CHART_COLORS`, StatCard for KPIs, remove ~3 hardcoded colors |
| **Map** | Station markers: color from `STATION_STATUS_CONFIG[status].color`, remove ~3 hardcoded colors |

### 4.6 Phase 4 Completion Checklist

- [ ] All 28 pages updated
- [ ] All inline `getStatusBadge()` / `getSeverityBadge()` functions removed
- [ ] All inline status label maps removed
- [ ] All hardcoded Tailwind color classes replaced (target: 0 remaining in `grep` for `text-green-|bg-green-|text-red-|bg-red-|text-blue-|bg-blue-|text-yellow-|bg-yellow-|text-orange-|bg-orange-|text-gray-`)
- [ ] Vouchers + Promotions consolidated (if decided)
- [ ] No functional regressions

---

## Phase 5: Polish & Consistency Audit

**Priority:** P3 | **Effort:** 1 day | **Risk:** Low

### 5.1 Color Consistency Audit

Run a comprehensive search for remaining hardcoded colors:

```bash
grep -rn "text-\(green\|blue\|red\|yellow\|orange\|gray\)-" src/admin-portal/src/
grep -rn "bg-\(green\|blue\|red\|yellow\|orange\|gray\)-" src/admin-portal/src/
grep -rn "#[0-9A-Fa-f]\{6\}" src/admin-portal/src/  # Hex colors (except in constants.ts)
```

Target: 0 hardcoded color utilities outside of `constants.ts` and `globals.css`.

### 5.2 Component Usage Audit

Verify every page uses the shared components:

| Component | Verify |
|-----------|--------|
| `StatusBadge` | Every status display across all pages uses it |
| `StatCard` | Every KPI metric card uses it (Dashboard, Analytics, Payments, Monitoring) |
| `FilterBar` | Every list page uses it |
| `DataTable` | Every data list uses it |
| `EmptyState` | Every list page has an empty state |
| `Skeleton` | Every page with async data has a loading skeleton |
| `Dialog` | All modals use it (User Management, station actions, fault resolution) |
| `Select` | All dropdowns use it (replacing raw `<select>` elements) |
| `Tabs` | Multi-view pages use it (Station detail, User Management) |

### 5.3 Responsive Layout Testing

Test all pages at these breakpoints:

| Breakpoint | Width | Key Checks |
|------------|-------|------------|
| Mobile | 375px | Sidebar collapsed, tables horizontally scrollable, FilterBar stacks vertically, StatCards stack 1-col |
| Tablet | 768px | Sidebar toggle works, 2-col grids, tables readable |
| Desktop | 1280px | Full sidebar, 4-col stat grids, all content visible |

### 5.4 Visual Regression Checklist

For each page, verify:

- [ ] Brand green appears in primary buttons, active sidebar, links
- [ ] Orange appears only as warning/secondary accent (not dominant)
- [ ] Status colors are consistent (green=good, blue=active, orange=warning, red=error, gray=inactive)
- [ ] Page background is `#F8FAFB` (slight green tint), cards are white
- [ ] All focus rings are green
- [ ] No blue remnants from the original shadcn/ui theme
- [ ] Loading skeletons appear during data fetch
- [ ] Empty states display when no data
- [ ] Charts use the brand color palette
- [ ] Numbers in tables/cards use tabular-nums (monospace alignment)

### 5.5 Phase 5 Completion Checklist

- [ ] Zero hardcoded color classes outside token files
- [ ] All status displays use StatusBadge
- [ ] All loading states use Skeleton
- [ ] All empty states use EmptyState
- [ ] Responsive layouts tested at 3 breakpoints
- [ ] Visual regression check complete for all 28 pages
- [ ] Final screenshot comparison: before vs after

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking existing functionality during refactor | Low | High | Phase 1 is CSS-only (no logic changes). Phase 2 adds new components without modifying existing ones. Phase 3–4 preserves all API calls, state management, and navigation. |
| Inconsistent partial migration | Medium | Medium | Phase 1 tokens affect all pages immediately via CSS variable inheritance. Even before component migration, the entire app shifts from blue to green. |
| Merge conflicts with ongoing feature work | Medium | Medium | Execute Phase 1+2 as a single PR (foundation). Then submit Phase 3 pages as individual PRs per page group. |
| Dark mode compatibility | Low | Low | Current codebase has a `.dark` block but no dark mode toggle. Update `.dark` variables to match brand colors if dark mode is planned. |
| Performance regression from new components | Low | Low | All new components are lightweight wrappers. No additional API calls or heavy computation. StatusBadge is a pure render function. |
| Chart color mismatch after palette update | Low | Medium | Recharts fills reference `CHART_COLORS` constants. Test all 4 chart-using pages (Dashboard, Analytics, Monitoring, Sessions detail). |

---

## Files Changed Summary

### Modified Files

| File | Phase | Nature of Change |
|------|-------|-----------------|
| `src/app/globals.css` | 1 | CSS variables (brand, status, surface tokens), typography utilities |
| `src/components/ui/button.tsx` | 2 | Add `brand` variant |
| `src/components/ui/badge.tsx` | 2 | Add `info`, `brand` variants; replace hardcoded colors |
| `src/components/layout/sidebar.tsx` | 2 | Regroup items, update branding, active state |
| `src/components/layout/header.tsx` | 2 | Remove duplicate alerts, add breadcrumbs |
| `src/app/login/page.tsx` | 3 | Brand gradient, logo update |
| `src/app/(dashboard)/page.tsx` | 3 | StatCard, chart colors, StatusBadge, skeleton |
| `src/app/(dashboard)/monitoring/page.tsx` | 3 | Constants import, StatusBadge, token colors |
| `src/app/(dashboard)/stations/page.tsx` | 3 | FilterBar, DataTable, StatusBadge |
| `src/app/(dashboard)/stations/[id]/page.tsx` | 3 | StatusBadge, Tabs, token colors |
| `src/app/(dashboard)/sessions/page.tsx` | 3 | Remove inline maps, DataTable, StatusBadge |
| `src/app/(dashboard)/sessions/[id]/page.tsx` | 3 | StatusBadge, token colors |
| `src/app/(dashboard)/faults/page.tsx` | 3 | Remove 55 lines inline functions, DataTable, StatusBadge |
| `src/app/(dashboard)/faults/[id]/page.tsx` | 3 | StatusBadge |
| `src/app/(dashboard)/alerts/page.tsx` | 3 | StatusBadge, FilterBar |
| `src/app/(dashboard)/payments/page.tsx` | 4 | StatusBadge, DataTable, StatCard |
| `src/app/(dashboard)/payments/[id]/page.tsx` | 4 | StatusBadge |
| `src/app/(dashboard)/tariffs/page.tsx` | 4 | DataTable, brand active indicator |
| `src/app/(dashboard)/user-management/page.tsx` | 4 | Dialog, Tabs, DataTable |
| `src/app/(dashboard)/audit-logs/page.tsx` | 4 | DataTable, FilterBar |
| `src/app/(dashboard)/maintenance/page.tsx` | 4 | StatusBadge, DataTable |
| `src/app/(dashboard)/e-invoices/page.tsx` | 4 | StatusBadge, DataTable |
| `src/app/(dashboard)/groups/page.tsx` | 4 | StatusBadge |
| `src/app/(dashboard)/settings/page.tsx` | 4 | Minimal token updates |
| `src/app/(dashboard)/vehicles/page.tsx` | 4 | DataTable |
| `src/app/(dashboard)/mobile-users/page.tsx` | 4 | DataTable |
| `src/app/(dashboard)/feedback/page.tsx` | 4 | StatusBadge, DataTable |
| `src/app/(dashboard)/vouchers/page.tsx` | 4 | StatusBadge, DataTable, Tabs (merge promotions) |
| `src/app/(dashboard)/promotions/page.tsx` | 4 | StatusBadge, DataTable (or merge into vouchers) |
| `src/app/(dashboard)/ocpp/page.tsx` | 4 | Token colors |
| `src/app/(dashboard)/analytics/page.tsx` | 4 | Chart palette, StatCard |
| `src/app/(dashboard)/map/page.tsx` | 4 | Marker colors from constants |
| `src/app/(dashboard)/stations/new/page.tsx` | 4 | Select component, brand button |
| `src/app/(dashboard)/stations/[id]/edit/page.tsx` | 4 | Select component, brand button |

### New Files

| File | Phase | Purpose |
|------|-------|---------|
| `src/lib/constants.ts` | 1 | Status configs, chart colors, helper functions |
| `src/components/ui/status-badge.tsx` | 2 | Universal status badge component |
| `src/components/ui/stat-card.tsx` | 2 | KPI metric card component |
| `src/components/ui/filter-bar.tsx` | 2 | Reusable search + filter layout |
| `src/components/ui/empty-state.tsx` | 2 | Empty data placeholder |
| `src/components/ui/skeleton.tsx` | 2 | Loading skeleton with variants |
| `src/components/ui/data-table.tsx` | 2 | Standard data table with pagination |
| `src/components/ui/page-header.tsx` | 2 | Page title with breadcrumbs and actions |
| `src/components/ui/dialog.tsx` | 2 | Modal overlay component |
| `src/components/ui/select.tsx` | 2 | Styled select dropdown |
| `src/components/ui/tabs.tsx` | 2 | Underline-style tab navigation |

**Total:** 34 modified files + 11 new files = 45 files across 5 phases.

---

## PR Strategy

| PR | Contents | Phase | Estimated Review |
|----|----------|-------|-----------------|
| PR #1 | Design Foundation + Core Components | 1 + 2 | 1 day |
| PR #2 | Login + Dashboard + Monitoring | 3.1–3.3 | 0.5 day |
| PR #3 | Stations + Sessions | 3.4–3.5 | 0.5 day |
| PR #4 | Faults + Alerts | 3.6 | 0.5 day |
| PR #5 | All remaining pages | 4 | 1 day |
| PR #6 | Polish & consistency audit | 5 | 0.5 day |

Each PR should be independently deployable. PR #1 is the most impactful — it changes the entire app's color palette instantly and provides the component library for subsequent PRs.
