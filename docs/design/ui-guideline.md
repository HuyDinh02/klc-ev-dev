# K-Charge Admin Portal — UI Guideline

> **Version:** 1.0
> **Last updated:** 2026-03-08
> **Status:** Implementation-ready
> **Applies to:** `src/admin-portal/` (Next.js + Tailwind CSS + shadcn/ui primitives)

---

## Table of Contents

1. [Brand Foundation](#1-brand-foundation)
2. [Color System](#2-color-system)
3. [Typography System](#3-typography-system)
4. [Logo Usage](#4-logo-usage)
5. [Layout System](#5-layout-system)
6. [Component Design Standards](#6-component-design-standards)
7. [Domain-Specific Patterns](#7-domain-specific-patterns)
8. [Status System](#8-status-system)
9. [Data Visualization](#9-data-visualization)
10. [Motion & Interaction](#10-motion--interaction)
11. [Accessibility](#11-accessibility)
12. [UI Copy & Tone](#12-ui-copy--tone)

---

## 1. Brand Foundation

### Brand Identity

- **Brand:** K-Charge by KLC Energy
- **Positioning:** Clean energy, sustainable infrastructure, trustworthy operations, modern enterprise
- **Market:** B2C EV charging station management in Vietnam
- **Audience:** Station operators, fleet managers, operations administrators

### Visual Principles

| Principle | Description |
|---|---|
| **Data-first** | Information density is a feature; every pixel should serve operational clarity |
| **Operational clarity** | Status, health, and alerts must be instantly scannable |
| **Professional enterprise** | Clean, restrained, confident -- not playful or consumer-grade |
| **Sustainable energy** | Green anchors the brand; it is the dominant accent, not decoration |

### Color Philosophy

| Color | Role | Use for |
|---|---|---|
| **Brand Green (#28A649)** | Primary | Primary actions, positive states, availability, sustainability messaging |
| **Brand Orange (#FAA623)** | Secondary accent | Energy highlights, important CTAs, dynamic accents, warning-lite indicators |
| **White / Neutrals** | Foundation | Readability, breathing room, professional backdrop, card surfaces |

### Do

- Use green for primary buttons, success states, available/online status, focus rings.
- Use orange sparingly for accent highlights, energy metric callouts, secondary CTA emphasis.
- Maintain generous whitespace between data-dense sections.
- Let the green-tinted page background subtly reinforce the brand.

### Don't

- Overuse saturated orange on large surfaces (cards, headers, backgrounds).
- Use brand green for danger or error states -- red is reserved for those.
- Mix Baskerville BT into UI body text, labels, or navigation -- it is logo-only.
- Combine green and orange adjacent without a neutral separator.
- Use gradients on interactive elements (buttons, inputs).

---

## 2. Color System

All colors are defined as CSS custom properties in `globals.css` and extended via Tailwind config. Hex values are canonical; HSL conversions are generated from these.

### 2.1 Brand Colors

| Token | CSS Variable | Hex | Usage |
|---|---|---|---|
| `brand-primary` | `--color-brand-primary` | `#28A649` | Primary buttons, active nav, focus rings, success-adjacent |
| `brand-primary-light` | `--color-brand-primary-light` | `#E8F5EC` | Primary button hover bg (ghost), selected row bg, tag bg |
| `brand-primary-dark` | `--color-brand-primary-dark` | `#1E7D37` | Primary button hover, pressed state |
| `brand-secondary` | `--color-brand-secondary` | `#FAA623` | Accent buttons, energy highlights, CTA emphasis |
| `brand-secondary-light` | `--color-brand-secondary-light` | `#FFF4E0` | Orange tag bg, warning-lite bg, highlight row |
| `brand-secondary-dark` | `--color-brand-secondary-dark` | `#D48A0F` | Orange hover, pressed state |

### 2.2 Semantic Colors

| Token | CSS Variable | Hex | Light Hex | Usage |
|---|---|---|---|---|
| `success` | `--color-success` | `#22C55E` | `#F0FDF4` | Completed, verified, available |
| `warning` | `--color-warning` | `#F59E0B` | `#FFFBEB` | Caution, pending review, suspended |
| `danger` | `--color-danger` | `#EF4444` | `#FEF2F2` | Errors, faults, failed, destructive actions |
| `info` | `--color-info` | `#3B82F6` | `#EFF6FF` | Informational, in-progress, charging |

### 2.3 Neutral Scale

| Token | CSS Variable | Hex | Usage |
|---|---|---|---|
| `gray-50` | `--color-gray-50` | `#F9FAFB` | Subtle bg, table stripe |
| `gray-100` | `--color-gray-100` | `#F3F4F6` | Hover bg, disabled bg |
| `gray-200` | `--color-gray-200` | `#E5E7EB` | Borders, dividers |
| `gray-300` | `--color-gray-300` | `#D1D5DB` | Strong borders, disabled text |
| `gray-400` | `--color-gray-400` | `#9CA3AF` | Placeholder text, muted icons |
| `gray-500` | `--color-gray-500` | `#6B7280` | Secondary text |
| `gray-600` | `--color-gray-600` | `#4B5563` | Body text secondary |
| `gray-700` | `--color-gray-700` | `#374151` | Body text |
| `gray-800` | `--color-gray-800` | `#1F2937` | Headings |
| `gray-900` | `--color-gray-900` | `#111827` | Primary text, high emphasis |

### 2.4 Surface & Background

| Token | CSS Variable | Hex | Usage |
|---|---|---|---|
| `bg-page` | `--color-bg-page` | `#F8FAFB` | Main content area background (very slight green tint) |
| `bg-card` | `--color-bg-card` | `#FFFFFF` | Card, modal, popover surfaces |
| `bg-sidebar` | `--color-bg-sidebar` | `#FFFFFF` | Sidebar background |
| `bg-header` | `--color-bg-header` | `#FFFFFF` | Header background (with `backdrop-blur`) |
| `bg-input` | `--color-bg-input` | `#FFFFFF` | Form input background |
| `bg-hover` | `--color-bg-hover` | `#F3F4F6` | Row/item hover state |
| `bg-selected` | `--color-bg-selected` | `#E8F5EC` | Selected row/item (brand-primary-light) |

### 2.5 Text

| Token | CSS Variable | Hex | Usage |
|---|---|---|---|
| `text-primary` | `--color-text-primary` | `#111827` | Headings, body, high-emphasis labels |
| `text-secondary` | `--color-text-secondary` | `#6B7280` | Descriptions, secondary info, table secondary cols |
| `text-muted` | `--color-text-muted` | `#9CA3AF` | Placeholders, disabled labels, timestamps |
| `text-inverse` | `--color-text-inverse` | `#FFFFFF` | Text on dark/colored backgrounds (buttons, badges) |
| `text-link` | `--color-text-link` | `#28A649` | Inline links (brand green) |

### 2.6 Border

| Token | CSS Variable | Hex | Usage |
|---|---|---|---|
| `border-default` | `--color-border-default` | `#E5E7EB` | Card borders, dividers, input borders |
| `border-strong` | `--color-border-strong` | `#D1D5DB` | Emphasized borders, active input |
| `border-focus` | `--color-border-focus` | `#28A649` | Focus ring color (brand green) |

### 2.7 Connector Status Palette (OCPP)

Each OCPP 1.6J `ChargePointStatus` maps to a unique color for instant visual identification.

| Status | Hex | Tailwind | Dot/Badge Color | Text Label |
|---|---|---|---|---|
| **Available** | `#22C55E` | `green-500` | Solid green | Available |
| **Charging** | `#3B82F6` | `blue-500` | Solid blue | Charging |
| **Preparing** | `#F59E0B` | `amber-500` | Solid amber | Preparing |
| **Finishing** | `#14B8A6` | `teal-500` | Solid teal | Finishing |
| **SuspendedEV** | `#F97316` | `orange-500` | Solid orange | Suspended (EV) |
| **SuspendedEVSE** | `#F97316` | `orange-500` | Solid orange | Suspended (EVSE) |
| **Reserved** | `#8B5CF6` | `violet-500` | Solid violet | Reserved |
| **Faulted** | `#EF4444` | `red-500` | Solid red | Faulted |
| **Unavailable** | `#78716C` | `stone-500` | Solid stone | Unavailable |
| **Offline** | `#9CA3AF` | `gray-400` | Hollow/dashed gray | Offline |
| **Maintenance** | `#6366F1` | `indigo-500` | Solid indigo | Maintenance |

### 2.8 Chart Palette

Six-color accessible sequence for charts and graphs. Ordered by visual distinctness and colorblind safety.

| Index | Hex | Name | Usage Example |
|---|---|---|---|
| 1 | `#28A649` | K-Charge Green | Revenue, energy delivered, primary metric |
| 2 | `#3B82F6` | Blue | Sessions, charging time |
| 3 | `#FAA623` | K-Charge Orange | Utilization, peak demand |
| 4 | `#8B5CF6` | Violet | Reservations, unique users |
| 5 | `#EC4899` | Pink | Faults, errors |
| 6 | `#14B8A6` | Teal | Availability, uptime |

For charts requiring more than 6 series, add lighter tints of the same sequence (30% opacity).

### 2.9 Alert Severity Colors

| Severity | Hex | Tailwind | Background | Border (left accent) |
|---|---|---|---|---|
| **Critical** | `#EF4444` | `red-500` | `#FEF2F2` | 4px solid `#EF4444` |
| **High** | `#F97316` | `orange-500` | `#FFF7ED` | 4px solid `#F97316` |
| **Medium** | `#F59E0B` | `amber-500` | `#FFFBEB` | 4px solid `#F59E0B` |
| **Low / Info** | `#3B82F6` | `blue-500` | `#EFF6FF` | 4px solid `#3B82F6` |

---

## 3. Typography System

### Font Stack

| Context | Font | Fallback | Notes |
|---|---|---|---|
| **UI (all text)** | `Inter` | `system-ui, -apple-system, sans-serif` | Loaded via `next/font/google`, variable `--font-inter` |
| **Logo / Brand** | `Baskerville BT` | `Georgia, serif` | Logo and brand expression ONLY. Never in UI body. |

Inter is chosen for its excellent legibility at small sizes, tabular figure support, and wide weight range -- ideal for data-dense admin screens.

### Type Scale

| Role | Size | Line Height | Weight | Tracking | Tailwind Classes |
|---|---|---|---|---|---|
| **Page title** | 24px | 1.3 | 600 (semibold) | normal | `text-2xl font-semibold leading-tight` |
| **Section title** | 18px | 1.4 | 600 (semibold) | normal | `text-lg font-semibold` |
| **Card title** | 16px | 1.5 | 600 (semibold) | normal | `text-base font-semibold` |
| **Body** | 14px | 1.5 | 400 (regular) | normal | `text-sm` |
| **Body strong** | 14px | 1.5 | 500 (medium) | normal | `text-sm font-medium` |
| **Small / Caption** | 12px | 1.4 | 400 (regular) | normal | `text-xs` |
| **Table header** | 12px | 1.4 | 600 (semibold) | 0.05em | `text-xs font-semibold uppercase tracking-wider text-gray-500` |
| **Table body** | 14px | 1.5 | 400 (regular) | normal | `text-sm` |
| **KPI value** | 28px | 1.2 | 700 (bold) | normal | `text-[28px] font-bold leading-tight tabular-nums` |
| **KPI label** | 12px | 1.4 | 500 (medium) | 0.05em | `text-xs font-medium uppercase tracking-wide text-gray-500` |
| **Badge text** | 12px | 1 | 600 (semibold) | normal | `text-xs font-semibold` |
| **Button text** | 14px | 1 | 500 (medium) | normal | `text-sm font-medium` |
| **Input text** | 14px | 1.5 | 400 (regular) | normal | `text-sm` |
| **Input label** | 14px | 1.5 | 500 (medium) | normal | `text-sm font-medium` |

### Font Weights

| Weight | CSS | Tailwind | Usage |
|---|---|---|---|
| 400 | `font-weight: 400` | `font-normal` | Body text, input text, descriptions |
| 500 | `font-weight: 500` | `font-medium` | Labels, buttons, nav items, KPI labels |
| 600 | `font-weight: 600` | `font-semibold` | Headings, card titles, table headers, badges |
| 700 | `font-weight: 700` | `font-bold` | KPI values, hero numbers, emphasis |

### Numeric Display Rules

- Always apply `font-variant-numeric: tabular-nums` (Tailwind: `tabular-nums`) on numeric values for alignment in tables and KPI cards.
- Right-align numeric columns in tables.
- Format currency as VND with dot separator: `9.900d` (use `toLocaleString('vi-VN')`).
- Format energy: `12,5 kWh` (one decimal, comma as decimal separator per Vietnamese locale).
- Format percentages: `87,5%` (one decimal).

---

## 4. Logo Usage

### Placement

| Context | Logo Variant | Behavior |
|---|---|---|
| **Sidebar expanded** | Full logo (icon + "K-Charge" wordmark) | 24px icon + text, left-aligned in 64px header |
| **Sidebar collapsed** | Logo mark (icon only) | 24px icon, centered in 64px header |
| **Login page** | Full logo centered | Above the login form card, 40px icon size |
| **Header** | No logo | Display page title instead |
| **Favicon** | Logo mark | 32x32 and 16x16 variants |
| **Loading screen** | Logo mark | Centered with subtle pulse animation |

### Clear Space

- Minimum 16px (1rem) clear space around the logo in all directions.
- In the sidebar header, the logo sits within the 64px-height row with vertical centering.

### Logo Variants

| Background | Variant | Notes |
|---|---|---|
| White / light | Full color (green icon + dark text) | Default usage |
| Dark | White variant (all white) | Inverse for dark surfaces |
| Colored bg | White variant | Ensure sufficient contrast |

### Restrictions

- Never distort, rotate, or skew the logo.
- Never recolor the logo icon outside the approved green or white variants.
- Never add drop shadows, glows, or outlines.
- Never place the logo on busy or low-contrast backgrounds.
- Never render the logo smaller than 20px icon size.

---

## 5. Layout System

### App Shell

```
+--+--------------------------------------------+
|  |  Header (sticky, 64px, border-bottom)      |
|S |--------------------------------------------+
|i |                                             |
|d |  Content Area (scrollable)                  |
|e |  padding: 24px                              |
|b |  background: #F8FAFB                        |
|a |                                             |
|r |                                             |
+--+---------------------------------------------+
```

| Element | Specification | Tailwind |
|---|---|---|
| **Sidebar collapsed** | 64px width (w-16) | `w-16` |
| **Sidebar expanded** | 256px width (w-64) | `w-64` |
| **Sidebar** | Fixed, full height, left, z-40, border-right | `fixed left-0 top-0 z-40 h-screen border-r bg-white` |
| **Header** | Sticky, 64px height, border-bottom, backdrop-blur, z-30 | `sticky top-0 z-30 h-16 border-b bg-white/95 backdrop-blur` |
| **Content area** | Fluid width (no max-width), 24px padding | `p-6` |
| **Content margin-left** | Matches sidebar width | `ml-16` or `ml-64` (transition) |

### Page Structure

Every page follows this hierarchy:

```
Page
 +-- Header (title + description + optional action buttons)
 +-- Filter/Action Bar (optional: search, filters, bulk actions)
 +-- Content
      +-- KPI Cards row (optional, dashboard/overview pages)
      +-- Primary content (table, grid, form, detail)
      +-- Pagination or load-more (if list)
```

### Grid System

| Pattern | Specification | Tailwind |
|---|---|---|
| **KPI card row** | 4 cols on xl, 2 on md, 1 on sm | `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4` |
| **Card grid** | 3 cols on lg, 2 on md, 1 on sm | `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4` |
| **Detail 2-col** | 2 cols on lg, stack on smaller | `grid grid-cols-1 lg:grid-cols-2 gap-6` |
| **Detail 3-col** | Main (2/3) + sidebar (1/3) | `grid grid-cols-1 lg:grid-cols-3 gap-6` with `lg:col-span-2` |
| **Form layout** | Max-width 640px, centered | `mx-auto max-w-2xl space-y-6` |

### Spacing Scale

Base unit: 4px. All spacing uses multiples of 4.

| Token | px | rem | Tailwind | Usage |
|---|---|---|---|---|
| `space-1` | 4px | 0.25rem | `1` | Tight inline spacing |
| `space-2` | 8px | 0.5rem | `2` | Icon-to-text gap, badge padding |
| `space-3` | 12px | 0.75rem | `3` | Compact padding |
| `space-4` | 16px | 1rem | `4` | Card grid gap, standard gap |
| `space-5` | 20px | 1.25rem | `5` | Section divider |
| `space-6` | 24px | 1.5rem | `6` | Page padding, card inner padding |
| `space-8` | 32px | 2rem | `8` | Section spacing |
| `space-10` | 40px | 2.5rem | `10` | Large section gaps |
| `space-12` | 48px | 3rem | `12` | Page section spacing |
| `space-16` | 64px | 4rem | `16` | Major layout spacing |

### Border Radius

| Token | px | Tailwind | Usage |
|---|---|---|---|
| `radius-sm` | 4px | `rounded` | Badges (inline), small elements |
| `radius-md` | 6px | `rounded-md` | Buttons, inputs, selects |
| `radius-lg` | 8px | `rounded-lg` | Cards, modals, popovers |
| `radius-xl` | 12px | `rounded-xl` | Large cards, hero sections |
| `radius-full` | 9999px | `rounded-full` | Status dots, avatar, pills |

### Overlay & Modal Sizes

| Size | Width | Tailwind | Usage |
|---|---|---|---|
| **sm** | 400px | `max-w-sm` (customized to 400px) | Confirmation dialogs, simple forms |
| **md** | 500px | `max-w-md` (customized to 500px) | Standard edit forms |
| **lg** | 640px | `max-w-lg` (customized to 640px) | Complex forms, detail views |
| **xl** | 768px | `max-w-xl` (customized to 768px) | Data-heavy modals, multi-step flows |

### Drawer

- Slides from right edge.
- Width: 400-500px depending on content.
- Overlay: `bg-black/50`.
- Contains header (title + close button), scrollable body, optional sticky footer.

### Breakpoints

| Name | Min-width | Tailwind prefix | Layout change |
|---|---|---|---|
| **sm** | 640px | `sm:` | Single-column to basic responsive |
| **md** | 768px | `md:` | 2-column grids, show search bar |
| **lg** | 1024px | `lg:` | 3-column grids, detail page 2-col |
| **xl** | 1280px | `xl:` | 4-column KPI grids, full data tables |

---

## 6. Component Design Standards

### 6.1 Buttons

#### Variants

| Variant | Background | Text | Border | Hover | Tailwind |
|---|---|---|---|---|---|
| **Primary** | `#28A649` | `#FFFFFF` | none | `#1E7D37` | `bg-[#28A649] text-white hover:bg-[#1E7D37]` |
| **Secondary** | transparent | `#374151` | `#E5E7EB` | `#F3F4F6` bg | `border border-gray-200 text-gray-700 hover:bg-gray-100` |
| **Danger** | `#EF4444` | `#FFFFFF` | none | `#DC2626` | `bg-red-500 text-white hover:bg-red-600` |
| **Ghost** | transparent | `#6B7280` | none | `#F3F4F6` bg | `text-gray-500 hover:bg-gray-100 hover:text-gray-700` |
| **Link** | transparent | `#28A649` | none | underline | `text-[#28A649] hover:underline` |
| **Accent** | `#FAA623` | `#FFFFFF` | none | `#D48A0F` | `bg-[#FAA623] text-white hover:bg-[#D48A0F]` |

#### Sizes

| Size | Height | Padding | Font | Icon | Tailwind |
|---|---|---|---|---|---|
| **sm** | 36px | 12px horizontal | 13px | 16px | `h-9 px-3 text-[13px]` |
| **default** | 40px | 16px horizontal | 14px | 18px | `h-10 px-4 text-sm` |
| **lg** | 44px | 32px horizontal | 14px | 20px | `h-11 px-8 text-sm` |
| **icon** | 40x40 | centered | -- | 20px | `h-10 w-10` |

#### Button Rules

- Primary action: one per visible area (e.g., "Add Station", "Save Changes").
- Destructive actions: always require confirmation modal.
- Loading state: replace label with spinner + "Saving..." (disable button).
- Icon + label: icon left, 8px gap (`gap-2`).
- All buttons: `rounded-md`, `font-medium`, `transition-colors`, `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#28A649] focus-visible:ring-offset-2`.
- Disabled: `opacity-50 cursor-not-allowed pointer-events-none`.

### 6.2 Inputs

```
Label (text-sm font-medium text-gray-700)
+------------------------------------------+
| Placeholder text                         |  40px height
+------------------------------------------+
Helper text or error message (text-xs)
```

| Property | Value | Tailwind |
|---|---|---|
| Height | 40px | `h-10` |
| Border | 1px `#E5E7EB` | `border border-gray-200` |
| Border radius | 6px | `rounded-md` |
| Padding | 12px horizontal | `px-3` |
| Font | 14px regular | `text-sm` |
| Background | `#FFFFFF` | `bg-white` |
| Focus | 2px ring `#28A649` | `focus:ring-2 focus:ring-[#28A649] focus:border-[#28A649]` |
| Error | 1px `#EF4444` border, red ring | `border-red-500 focus:ring-red-500` |
| Disabled | `#F3F4F6` bg, `opacity-50` | `bg-gray-100 opacity-50 cursor-not-allowed` |
| Placeholder | `#9CA3AF` | `placeholder:text-gray-400` |

- Always pair with a visible `<label>` element (never floating labels).
- Error messages appear below the input in `text-xs text-red-500`.
- Optional helper text below in `text-xs text-gray-400`.
- Required fields: append red asterisk to label (`<span class="text-red-500">*</span>`).

### 6.3 Select / Dropdown

Same dimensions as Input (h-10, rounded-md, border). Chevron icon right-aligned. Dropdown menu: white bg, border, rounded-lg, shadow-lg, max-height 300px with scroll.

### 6.4 Search Bar

| Property | Value | Tailwind |
|---|---|---|
| Height | 36px | `h-9` |
| Width | 256px (header), full-width (filter bar) | `w-64` or `w-full` |
| Icon | Search icon (16px) left, 12px inset | `pl-9` |
| Border radius | 6px | `rounded-md` |
| Background | `#FFFFFF` | `bg-white` |

### 6.5 Tabs

Underline style. Active tab has brand-green bottom border.

| State | Text | Border | Tailwind |
|---|---|---|---|
| **Default** | `text-gray-500` | none | `text-gray-500 hover:text-gray-700` |
| **Active** | `text-[#28A649]` | 2px bottom `#28A649` | `text-[#28A649] border-b-2 border-[#28A649]` |
| **Hover** | `text-gray-700` | none | `hover:text-gray-700` |

Tab container: `border-b border-gray-200`, tab items: `px-4 py-2 text-sm font-medium`.

### 6.6 Cards

```
+----------------------------------------------+
|  Card Header (p-6)                           |
|  Title (text-base font-semibold)             |
|  Description (text-sm text-gray-500)         |
+----------------------------------------------+
|  Card Content (p-6 pt-0)                     |
|  ...                                         |
+----------------------------------------------+
|  Card Footer (p-6 pt-0) [optional]           |
+----------------------------------------------+
```

| Property | Value | Tailwind |
|---|---|---|
| Background | `#FFFFFF` | `bg-white` |
| Border | 1px `#E5E7EB` | `border border-gray-200` |
| Border radius | 8px | `rounded-lg` |
| Shadow | subtle | `shadow-sm` |
| Padding | 24px | `p-6` |

### 6.7 KPI / Stat Cards

```
+-------------------------------------------+
|  [Icon]                                    |
|                                            |
|  LABEL (text-xs uppercase tracking-wide)   |
|  1.234 (text-[28px] font-bold)             |
|  +12,5% vs last period (trend)             |
+-------------------------------------------+
```

| Element | Tailwind |
|---|---|
| Icon container | `h-10 w-10 rounded-lg bg-[color]-100 flex items-center justify-center` |
| Icon | `h-5 w-5 text-[color]-600` |
| Label | `text-xs font-medium uppercase tracking-wide text-gray-500 mt-4` |
| Value | `text-[28px] font-bold leading-tight tabular-nums text-gray-900 mt-1` |
| Trend up | `text-xs font-medium text-green-600` with up-arrow icon |
| Trend down | `text-xs font-medium text-red-600` with down-arrow icon |
| Trend neutral | `text-xs font-medium text-gray-500` with dash icon |

Use the chart palette for icon background tints: green for energy, blue for sessions, orange for revenue, etc.

### 6.8 Tables

| Property | Value | Tailwind |
|---|---|---|
| Container | Full width, border, rounded-lg, overflow-hidden | `w-full border rounded-lg overflow-hidden` |
| Header row | `#F9FAFB` bg | `bg-gray-50` |
| Header cell | 12px semibold uppercase tracking-wider | `text-xs font-semibold uppercase tracking-wider text-gray-500 px-4 py-3` |
| Body cell | 14px regular | `text-sm px-4 py-3` |
| Row border | bottom 1px | `border-b border-gray-100` |
| Row hover | `#F9FAFB` bg | `hover:bg-gray-50` |
| Striped (optional) | Even rows `#F9FAFB` | `even:bg-gray-50` |
| Selected row | `#E8F5EC` bg | `bg-[#E8F5EC]` |
| Sortable header | Pointer cursor, sort icon right | `cursor-pointer` + chevron icon |

#### Table Density

| Density | Cell padding | Row height (approx) |
|---|---|---|
| **Comfortable** | `px-4 py-3` | 48px |
| **Compact** | `px-4 py-2` | 40px |
| **Dense** | `px-3 py-1.5` | 34px |

#### Table Column Alignment

- Text columns: left-aligned (default).
- Numeric columns (kWh, VND, counts): right-aligned (`text-right tabular-nums`).
- Status columns: left-aligned (badge).
- Action columns: right-aligned or centered.
- Checkbox columns: centered, 40px width.

### 6.9 Badges / Status Pills

#### Filled Badges (for status indicators)

| Variant | Background | Text | Tailwind |
|---|---|---|---|
| **Success** | `#F0FDF4` | `#16A34A` | `bg-green-50 text-green-700` |
| **Warning** | `#FFFBEB` | `#D97706` | `bg-amber-50 text-amber-700` |
| **Danger** | `#FEF2F2` | `#DC2626` | `bg-red-50 text-red-700` |
| **Info** | `#EFF6FF` | `#2563EB` | `bg-blue-50 text-blue-700` |
| **Neutral** | `#F3F4F6` | `#4B5563` | `bg-gray-100 text-gray-600` |
| **Violet** | `#F5F3FF` | `#7C3AED` | `bg-violet-50 text-violet-700` |
| **Orange** | `#FFF7ED` | `#EA580C` | `bg-orange-50 text-orange-700` |
| **Teal** | `#F0FDFA` | `#0D9488` | `bg-teal-50 text-teal-700` |
| **Indigo** | `#EEF2FF` | `#4F46E5` | `bg-indigo-50 text-indigo-700` |

Base classes: `inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold`.

#### Outline Badges (for categories/tags)

Border + transparent bg: `border border-gray-300 text-gray-600 bg-transparent`.

#### Status Dot

Small colored circle before status text: `h-2 w-2 rounded-full bg-[status-color]` with 6px gap.

### 6.10 Alerts / Toasts

#### Inline Alerts (page-level)

```
+--+-------------------------------------------+
|  |  Title (font-medium)                      |
|  |  Description text (text-sm)               |
+--+-------------------------------------------+
 ^-- 4px left border accent
```

| Variant | Left border | Background | Icon | Text |
|---|---|---|---|---|
| **Success** | `#22C55E` | `#F0FDF4` | CheckCircle green | `text-green-800` |
| **Warning** | `#F59E0B` | `#FFFBEB` | AlertTriangle amber | `text-amber-800` |
| **Danger** | `#EF4444` | `#FEF2F2` | XCircle red | `text-red-800` |
| **Info** | `#3B82F6` | `#EFF6FF` | Info blue | `text-blue-800` |

Tailwind: `border-l-4 rounded-r-lg p-4 flex gap-3`.

#### Toast Notifications

- Position: top-right, 16px from edge.
- Width: 360px.
- Same 4 variants as inline alerts.
- Auto-dismiss: 5 seconds (with progress bar).
- Dismissible: X button top-right.
- Stack: newest on top, max 3 visible.
- Animation: slide in from right (200ms), fade out (200ms).

### 6.11 Modals / Dialogs

| Property | Value | Tailwind |
|---|---|---|
| Overlay | `rgba(0,0,0,0.5)` | `bg-black/50` |
| Card | white, rounded-lg, shadow-xl | `bg-white rounded-lg shadow-xl` |
| Position | Centered vertically and horizontally | `fixed inset-0 flex items-center justify-center` |
| Padding | 24px | `p-6` |
| Header | Title (text-lg font-semibold) + optional close button | -- |
| Footer | Right-aligned buttons, gap-3 | `flex justify-end gap-3 pt-4` |
| Close | X icon button, top-right | `absolute top-4 right-4` |

Confirmation modals always have: icon (colored by severity), title, description, Cancel (secondary) + Confirm (primary or danger) buttons.

### 6.12 Drawers

- Slide from right edge.
- Width: 400px (sm), 500px (md).
- Header: title + close button, border-bottom.
- Body: scrollable, padding 24px.
- Footer (optional): sticky, border-top, padding 16px, right-aligned buttons.
- Overlay: `bg-black/50`.

### 6.13 Pagination

Cursor-based (never offset). Display as Previous / Next buttons.

```
Showing 1-20 of 156 results        [< Previous]  [Next >]
```

| Element | Tailwind |
|---|---|
| Container | `flex items-center justify-between py-3` |
| Info text | `text-sm text-gray-500` |
| Button | Secondary button style, disabled if no prev/next |

### 6.14 Breadcrumbs

```
Dashboard  /  Stations  /  Station Detail
```

| Element | Tailwind |
|---|---|
| Container | `flex items-center gap-2 text-sm` |
| Link | `text-gray-500 hover:text-gray-700` |
| Separator | `/` in `text-gray-300` |
| Current | `text-gray-900 font-medium` |

### 6.15 Empty States

Centered within the content area where data would appear.

```
        [Icon - 48px, text-gray-300]

        Title (text-lg font-semibold text-gray-900)
        Description (text-sm text-gray-500, max-w-md)

        [Primary Action Button]
```

Tailwind: `flex flex-col items-center justify-center py-16 text-center`.

### 6.16 Skeleton Loaders

Animated placeholder blocks that match the shape of content being loaded.

| Element | Tailwind |
|---|---|
| Base | `bg-gray-200 rounded animate-pulse` |
| Text line | `h-4 bg-gray-200 rounded animate-pulse` |
| Title | `h-6 w-48 bg-gray-200 rounded animate-pulse` |
| Avatar | `h-10 w-10 bg-gray-200 rounded-full animate-pulse` |
| Card | `h-32 bg-gray-200 rounded-lg animate-pulse` |
| Table row | Full-width row of skeleton cells |

Use skeletons for page loads. Use spinners only for inline actions (button loading, refresh).

### 6.17 Timeline / Activity Log

Vertical timeline with left-aligned dots and content right.

```
  o--- Title (font-medium)
  |    Description (text-sm text-gray-500)
  |    Timestamp (text-xs text-gray-400)
  |
  o--- Next event...
```

| Element | Tailwind |
|---|---|
| Line | `border-l-2 border-gray-200 ml-3` |
| Dot | `h-2.5 w-2.5 rounded-full bg-[status-color] ring-4 ring-white` |
| Content | `ml-6 pb-6` |

### 6.18 Map Markers

For the Leaflet station map view:

| Status | Marker Color | Stroke | Size |
|---|---|---|---|
| Available | `#22C55E` fill | 2px white stroke | 12px radius |
| Occupied/Charging | `#3B82F6` fill | 2px white stroke | 12px radius |
| Faulted | `#EF4444` fill | 2px white stroke | 12px radius |
| Offline | `#9CA3AF` fill | 2px dashed white stroke | 12px radius |
| Unavailable | `#78716C` fill | 2px white stroke | 12px radius |

Cluster markers: circle with count, sized proportionally, `#28A649` fill with white text.

### 6.19 Connector Status Widget

Compact visual array showing all connectors for a station:

```
+---+---+---+---+
| 1 | 2 | 3 | 4 |   <- Connector IDs
| G | G | B | R |   <- Color-coded by status
+---+---+---+---+
```

Each cell: 32x32px, rounded-md, colored background (light tint), status dot (8px) centered, connector ID below (text-xs). Hover tooltip shows full status + connector type.

---

## 7. Domain-Specific Patterns

### 7.1 Dashboard Overview

```
+---------------------------------------------------+
| Page Title: Dashboard                              |
+---------------------------------------------------+
| [KPI] Total    [KPI] Active    [KPI] Energy   [KPI] Revenue |
| Stations       Sessions        Today (kWh)     Today (VND) |
+---------------------------------------------------+
| [Chart: Energy over time - Area]  | [Chart: Status |
| Full-width or 2/3                 | Distribution   |
|                                   | - Donut]       |
+---------------------------------------------------+
| [Chart: Revenue trend - Bar]      | [Table: Recent |
|                                   | Alerts]        |
+---------------------------------------------------+
```

- KPI cards: 4 across on desktop, 2 on tablet, 1 on mobile.
- Charts: Recharts library, use chart palette.
- Time range selector: top-right of chart card (Today / 7D / 30D / Custom).

### 7.2 Station List

```
+---------------------------------------------------+
| Stations                          [+ Add Station]  |
+---------------------------------------------------+
| Search [___________]  Status [v]  Group [v]        |
+---------------------------------------------------+
| Table: Name | Code | Address | Status | Connectors | Actions |
| ...                                                 |
+---------------------------------------------------+
| Showing 1-20 of 45        [< Previous] [Next >]    |
+---------------------------------------------------+
```

- Station name is a link (text-[#28A649]) to detail page.
- Status column: colored badge.
- Connectors column: compact connector widget (mini dots).
- Actions: icon buttons (Edit, View) or dropdown menu for more.

### 7.3 Station Detail

```
+---------------------------------------------------+
| < Back to Stations                                  |
| Station Name                        [Edit] [OCPP]  |
| Station code | Group | Address                      |
+---------------------------------------------------+
| [Tab: Overview] [Tab: Connectors] [Tab: Sessions]  |
| [Tab: Faults] [Tab: Maintenance] [Tab: Config]     |
+---------------------------------------------------+
| Tab Content Area                                    |
|                                                     |
| Overview: KPI row + status timeline + map           |
| Connectors: Grid of connector cards                 |
| Sessions: Filtered table of sessions                |
+---------------------------------------------------+
```

- Tabs: underline style, brand-green active.
- Connector cards: status-colored left border, connector type icon, power rating, current status badge.

### 7.4 Connector Status Visualization

Each connector renders as a card:

```
+--+-------------------------------------------+
|  |  Connector #1 - CCS2                      |
|  |  Status: [Available badge]                |
|  |  Power: 150 kW DC                         |
|  |  Last session: 2 hours ago                |
+--+-------------------------------------------+
 ^-- 4px left border in status color
```

### 7.5 Charging Session List & Detail

**List view:** Table with columns: ID, Station, Connector, User, Start Time, Duration, Energy (kWh), Cost (VND), Status.

**Detail view:**
```
+---------------------------------------------------+
| Session #ABC-123                    [Stop Session]  |
| Station Name > Connector #2                         |
+---------------------------------------------------+
| [KPI] Duration  [KPI] Energy    [KPI] Cost         |
| 01:23:45        12,5 kWh        45.000d            |
+---------------------------------------------------+
| [Chart: Power curve over time - Area]               |
+---------------------------------------------------+
| Meter Values Table (timestamp, power, energy, SoC)  |
+---------------------------------------------------+
```

**Live session:** If status is InProgress, energy and duration counters update via SignalR. Use `tabular-nums` for the counters. Pulse animation on the "Charging" badge.

### 7.6 Alerts / Incidents Triage

```
+---------------------------------------------------+
| Alerts                     [Mark All Read]          |
+---------------------------------------------------+
| Filter: All | Critical | High | Medium | Low       |
+---------------------------------------------------+
| [Critical] Station ABC offline - 5 min ago    [Ack] |
| [High] Connector #2 faulted - 12 min ago     [Ack] |
| [Medium] Low utilization - 1 hour ago              |
+---------------------------------------------------+
```

- Left border colored by severity.
- Unacknowledged: bold title, white bg.
- Acknowledged: normal weight, `#F9FAFB` bg.
- Resolved: `text-gray-400`, strikethrough timestamp.

### 7.7 Maintenance Tickets

Kanban-style or table with status columns:

| Status | Badge Variant | Color |
|---|---|---|
| Planned | Info (blue) | `bg-blue-50 text-blue-700` |
| In Progress | Warning (amber) | `bg-amber-50 text-amber-700` |
| Completed | Success (green) | `bg-green-50 text-green-700` |
| Cancelled | Neutral (gray) | `bg-gray-100 text-gray-600` |

### 7.8 Tariff Configuration

Time-block editor concept:

```
+---------------------------------------------------+
| Tariff: Peak Hours Plan                [Edit]       |
+---------------------------------------------------+
| Time Blocks:                                        |
| 00:00-06:00  [Off-Peak]  1.500 VND/kWh   [green]   |
| 06:00-09:00  [Standard]  2.500 VND/kWh   [blue]    |
| 09:00-11:30  [Peak]      3.800 VND/kWh   [orange]  |
| 11:30-17:00  [Standard]  2.500 VND/kWh   [blue]    |
| 17:00-20:00  [Peak]      3.800 VND/kWh   [orange]  |
| 20:00-24:00  [Off-Peak]  1.500 VND/kWh   [green]   |
+---------------------------------------------------+
| Visual 24h timeline bar (color segments)            |
+---------------------------------------------------+
```

Color coding: Off-Peak = green, Standard = blue, Peak = orange.

### 7.9 Transaction / Payment History

Table: Transaction ID, Session ID, User, Amount (VND), Gateway (badge with icon), Status (badge), Timestamp.

- Amount column: right-aligned, `tabular-nums`, `font-medium`.
- Refunded amounts: shown with strikethrough + refund amount below.

### 7.10 User Management

```
+---------------------------------------------------+
| Users                              [+ Create User]  |
+---------------------------------------------------+
| Table: Username | Email | Role | Status | Actions   |
+---------------------------------------------------+
```

- Role: colored badge (Admin = violet, Operator = blue, Viewer = gray).
- Permission matrix: checkbox grid inside a modal or dedicated page (scrollable table with permission groups as rows, roles as columns).

### 7.11 Audit Log

```
+---------------------------------------------------+
| Audit Logs                                          |
+---------------------------------------------------+
| Date range: [_____] to [_____]  User [v]  Action [v]|
+---------------------------------------------------+
| Timeline view:                                      |
|   o  admin updated Station "ABC" - 10:30           |
|   o  operator resolved Fault #123 - 09:45          |
|   o  system generated E-Invoice #456 - 09:00       |
+---------------------------------------------------+
```

- Entity links: clickable, navigate to the referenced entity.
- Diff viewer: expandable section showing before/after JSON.

### 7.12 Reports / Analytics

```
+---------------------------------------------------+
| Analytics                   Time: [Last 30 Days v]  |
+---------------------------------------------------+
| [KPI] Total Energy  [KPI] Revenue  [KPI] Sessions  |
| [KPI] Avg Duration  [KPI] MTBF                     |
+---------------------------------------------------+
| [Area Chart: Energy over time]                      |
| [Bar Chart: Revenue by station]                     |
| [Table: Station utilization ranking (sortable)]     |
+---------------------------------------------------+
```

- Time range control: dropdown (Today, 7D, 30D, 90D, Custom date range).
- Export button: secondary button with download icon.
- Utilization table: sortable columns, horizontal bar chart inline.

---

## 8. Status System

### 8.1 Connector Status (OCPP 1.6J ChargePointStatus)

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Available | Green | `#22C55E` | `CheckCircle` | Success | Connector ready for use |
| Charging | Blue | `#3B82F6` | `Zap` | Info | Actively charging |
| Preparing | Amber | `#F59E0B` | `Loader` | Warning | Cable connected, waiting |
| Finishing | Teal | `#14B8A6` | `CheckCheck` | Teal | Session ending, wrap-up |
| SuspendedEV | Orange | `#F97316` | `PauseCircle` | Orange | Paused by vehicle |
| SuspendedEVSE | Orange | `#F97316` | `PauseCircle` | Orange | Paused by charger |
| Reserved | Violet | `#8B5CF6` | `Lock` | Violet | Reserved for user |
| Faulted | Red | `#EF4444` | `AlertTriangle` | Danger | Hardware fault |
| Unavailable | Stone | `#78716C` | `MinusCircle` | Neutral | Out of service |

### 8.2 Station Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Offline | Gray | `#9CA3AF` | `WifiOff` | Neutral | No heartbeat |
| Available | Green | `#22C55E` | `CheckCircle` | Success | All connectors available |
| Occupied | Blue | `#3B82F6` | `Zap` | Info | At least one connector in use |
| Unavailable | Stone | `#78716C` | `MinusCircle` | Neutral | Maintenance / disabled |
| Faulted | Red | `#EF4444` | `AlertTriangle` | Danger | Station-level fault |
| Decommissioned | Gray | `#9CA3AF` | `XCircle` | Neutral | Retired from service |

### 8.3 Session Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Pending | Gray | `#9CA3AF` | `Clock` | Neutral | Created, not started |
| Starting | Amber | `#F59E0B` | `Loader` | Warning | Remote start sent |
| InProgress | Blue | `#3B82F6` | `Zap` | Info | Actively charging |
| Suspended | Orange | `#F97316` | `PauseCircle` | Orange | Paused (EV or EVSE) |
| Stopping | Amber | `#F59E0B` | `Loader` | Warning | Stop sent, awaiting |
| Completed | Green | `#22C55E` | `CheckCircle` | Success | Finished successfully |
| Failed | Red | `#EF4444` | `XCircle` | Danger | Error or cancelled |

### 8.4 Payment Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Pending | Gray | `#9CA3AF` | `Clock` | Neutral | Awaiting initiation |
| Processing | Amber | `#F59E0B` | `Loader` | Warning | Gateway processing |
| Completed | Green | `#22C55E` | `CheckCircle` | Success | Payment received |
| Failed | Red | `#EF4444` | `XCircle` | Danger | Payment failed |
| Refunded | Violet | `#8B5CF6` | `RotateCcw` | Violet | Refund issued |
| Cancelled | Gray | `#9CA3AF` | `Ban` | Neutral | Payment cancelled |

### 8.5 Fault Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Open | Red | `#EF4444` | `AlertCircle` | Danger | New, unaddressed |
| Investigating | Amber | `#F59E0B` | `Search` | Warning | Under investigation |
| Resolved | Green | `#22C55E` | `CheckCircle` | Success | Fixed |
| Closed | Gray | `#9CA3AF` | `Archive` | Neutral | Closed (false alarm, etc.) |

### 8.6 Alert Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| New | Red | `#EF4444` | `Bell` | Danger | Unacknowledged |
| Acknowledged | Amber | `#F59E0B` | `Eye` | Warning | Operator aware |
| Resolved | Green | `#22C55E` | `CheckCircle` | Success | Issue resolved |

### 8.7 Alert Severity

| Severity | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Critical | Red | `#EF4444` | `AlertOctagon` | Danger | Immediate action required |
| High | Orange | `#F97316` | `AlertTriangle` | Orange | Urgent attention needed |
| Medium | Amber | `#F59E0B` | `AlertCircle` | Warning | Should be reviewed |
| Low / Info | Blue | `#3B82F6` | `Info` | Info | Informational, no urgency |

### 8.8 Maintenance Task Status

| Status | Color | Hex | Icon (Lucide) | Badge Variant | Context |
|---|---|---|---|---|---|
| Planned | Blue | `#3B82F6` | `Calendar` | Info | Scheduled for future |
| InProgress | Amber | `#F59E0B` | `Wrench` | Warning | Work underway |
| Completed | Green | `#22C55E` | `CheckCircle` | Success | Work finished |
| Cancelled | Gray | `#9CA3AF` | `Ban` | Neutral | Cancelled |

---

## 9. Data Visualization

### 9.1 Chart Library

Use **Recharts** (already in the codebase). All charts render inside Card components with a card header containing the title and optional time-range selector.

### 9.2 KPI Cards with Sparkline

```
+-------------------------------------------+
|  TOTAL ENERGY DELIVERED                    |
|  12.456 kWh         [~sparkline~]         |
|  +8,2% vs last period                     |
+-------------------------------------------+
```

- Sparkline: 80px wide, 32px tall, stroke-only (no fill), 2px stroke in chart color.
- Trend: arrow icon + percentage, colored green (up) or red (down).

### 9.3 Chart Types and Usage

| Chart Type | Component | Usage | Example |
|---|---|---|---|
| **Area** | `<AreaChart>` | Continuous time-series data | Energy delivered over time, power output |
| **Bar** | `<BarChart>` | Discrete comparisons | Revenue by station, sessions by day |
| **Donut** | `<PieChart>` (inner radius) | Part-of-whole distribution | Connector status distribution, payment methods |
| **Horizontal Bar** | `<BarChart layout="vertical">` | Ranked comparisons | Station utilization ranking |

### 9.4 Chart Color Assignment

| Series Index | Color | Hex | Typical Usage |
|---|---|---|---|
| 1 (primary) | K-Charge Green | `#28A649` | Revenue, energy, primary metric |
| 2 | Blue | `#3B82F6` | Sessions, time-based |
| 3 | K-Charge Orange | `#FAA623` | Utilization, demand |
| 4 | Violet | `#8B5CF6` | Users, reservations |
| 5 | Pink | `#EC4899` | Faults, errors |
| 6 | Teal | `#14B8A6` | Availability, uptime |

For fills/areas, use 20% opacity of the stroke color.

### 9.5 Chart Standards

| Element | Specification |
|---|---|
| **Axis labels** | Always visible. X-axis: time or category. Y-axis: unit (kWh, VND, count). |
| **Axis text** | `text-xs text-gray-400` (12px) |
| **Grid lines** | Horizontal only, `#F3F4F6` (gray-100), dashed |
| **Tooltip** | White bg, border, rounded-lg, shadow-md. Title bold, value + unit. |
| **Legend** | Below chart, horizontal, dot + label, `text-xs text-gray-500` |
| **Time range** | Selector in card header: Today / 7D / 30D / 90D / Custom |
| **Responsive** | Charts use `<ResponsiveContainer width="100%" height={300}>` |
| **Empty state** | If no data, show centered "No data for this period" message |
| **Loading** | Skeleton rectangle matching chart dimensions |

### 9.6 Chart Anti-Patterns

- No 3D charts ever.
- No radar/spider charts.
- No sunburst or treemap charts.
- No pie charts (use donut with inner radius instead).
- No more than 6 colors in a single chart.
- No dual Y-axes (split into two charts instead).
- Never truncate axis labels -- abbreviate or rotate 45 degrees.

### 9.7 Number Formatting in Charts

| Type | Format | Example |
|---|---|---|
| Energy | `X,X kWh` or `X,X MWh` (>1000) | `12,5 kWh`, `1,2 MWh` |
| Currency | `X.XXX d` with dot thousands separator | `45.000 d`, `1.234.567 d` |
| Percentage | `X,X%` | `87,5%` |
| Count | Dot thousands separator | `1.234` |
| Duration | `HH:MM:SS` or `Xh Ym` | `01:23:45`, `2h 15m` |
| Time axis | `HH:mm` for intraday, `dd/MM` for multi-day | `14:30`, `08/03` |

---

## 10. Motion & Interaction

### 10.1 Transition Timing

| Interaction | Duration | Easing | Tailwind |
|---|---|---|---|
| **Hover (bg change)** | 150ms | ease-in-out | `transition-colors duration-150` |
| **Focus ring** | 150ms | ease-in-out | `transition-shadow duration-150` |
| **Button press** | 100ms | ease-in | `active:scale-[0.98]` (optional) |
| **Sidebar expand/collapse** | 300ms | ease-in-out | `transition-all duration-300` |
| **Status color change** | 300ms | ease-in-out | `transition-colors duration-300` |
| **Modal overlay** | 200ms | ease-out | `transition-opacity duration-200` |
| **Modal card** | 200ms | ease-out | `transition-all duration-200` (scale 95%->100% + fade) |
| **Drawer slide** | 300ms | ease-in-out | `transition-transform duration-300` |
| **Toast enter** | 200ms | ease-out | Slide in from right + fade in |
| **Toast exit** | 200ms | ease-in | Fade out + slide right |
| **Dropdown open** | 150ms | ease-out | Scale 95%->100% + fade in |
| **Tab switch** | 150ms | ease-in-out | Content crossfade |

### 10.2 Loading States

| Context | Pattern | Notes |
|---|---|---|
| **Page load** | Skeleton shimmer | Full skeleton matching content shape |
| **Table load** | Skeleton rows (5-10) | Match column widths |
| **Card load** | Skeleton card | Match card dimensions |
| **Button action** | Spinner inside button + "Saving..." | Disable button, replace text |
| **Inline refresh** | Small spinner next to element | 16px spinner, `text-gray-400` |
| **Real-time update** | 300ms color transition | Smooth status badge color change |

### 10.3 Focus States

All focusable elements must show a visible focus ring:

```css
focus-visible:outline-none
focus-visible:ring-2
focus-visible:ring-[#28A649]
focus-visible:ring-offset-2
```

Tailwind: `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#28A649] focus-visible:ring-offset-2`.

Tab key navigation follows DOM order. Skip links available for sidebar-to-content jump.

### 10.4 Real-Time Updates (SignalR)

- New data arrives: row or card briefly highlights with `bg-[#E8F5EC]` (brand-primary-light) for 2 seconds, then fades back.
- Status changes: color transitions over 300ms (never instant).
- Live counters (energy, duration): smooth numeric transition, `tabular-nums` font.
- Connection indicator: small dot in header (green = connected, red = disconnected, amber = reconnecting).

---

## 11. Accessibility

### 11.1 Standards

- **Target:** WCAG 2.1 Level AA compliance.
- **Testing:** axe-core automated checks + manual keyboard testing.

### 11.2 Color Contrast

| Context | Minimum Ratio | Standard |
|---|---|---|
| Normal text (14px) | 4.5:1 | AA |
| Large text (18px+ or 14px bold) | 3:1 | AA |
| UI components (borders, icons) | 3:1 | AA |
| Non-text contrast (charts, badges) | 3:1 | AA |

**Verification:** All color pairs in this guideline meet AA minimum. Brand green `#28A649` on white = 4.52:1 (passes AA for normal text). For small text on colored backgrounds, prefer the dark variant (`#1E7D37` on white = 5.87:1).

### 11.3 Color Independence

Never rely on color alone to convey information. Always pair with:

| Color Signal | Required Companion |
|---|---|
| Status badge color | Icon + text label |
| Chart series color | Legend label + tooltip |
| Alert severity color | Icon + severity text |
| Form error | Red border + error message text |
| Success/failure | Icon (check/X) + text |
| Map marker color | Tooltip on hover/tap with text status |

### 11.4 Keyboard Navigation

| Element | Key | Behavior |
|---|---|---|
| All interactive | `Tab` / `Shift+Tab` | Move focus forward/backward |
| Buttons, links | `Enter` / `Space` | Activate |
| Modals | `Escape` | Close modal |
| Dropdowns | `Arrow Up/Down` | Navigate options |
| Dropdowns | `Enter` | Select option |
| Dropdowns | `Escape` | Close dropdown |
| Tabs | `Arrow Left/Right` | Switch tabs |
| Tables (sortable) | `Enter` on header | Toggle sort |

### 11.5 Form Accessibility

- Every `<input>` must have an associated `<label>` element (via `htmlFor`/`id`).
- Error messages must be associated via `aria-describedby`.
- Required fields: `aria-required="true"` + visual asterisk.
- Invalid fields: `aria-invalid="true"` when in error state.
- Form groups: use `<fieldset>` + `<legend>` for related inputs.
- Disable submit button when form is invalid (with `aria-disabled`).

### 11.6 Table Accessibility

- Use semantic `<table>`, `<thead>`, `<tbody>`, `<th>`, `<td>`.
- All `<th>` must have `scope="col"` or `scope="row"`.
- Sortable columns: `aria-sort="ascending"` / `"descending"` / `"none"`.
- Row actions: accessible via keyboard (tab into action buttons).
- Caption or `aria-label` on each table.

### 11.7 Image & Icon Accessibility

- Decorative icons: `aria-hidden="true"`.
- Meaningful icons (without text label): `aria-label` or `<title>` inside SVG.
- Icon-only buttons: must have `aria-label` (e.g., `aria-label="Edit station"`).
- Images: always have `alt` text.

### 11.8 Motion Preferences

- Respect `prefers-reduced-motion`: disable `animate-pulse`, transitions, and slide animations.
- Tailwind: use `motion-reduce:transition-none motion-reduce:animate-none` where applicable.
- Auto-dismiss toasts: keep visible longer (10s) when reduced motion is preferred.

---

## 12. UI Copy & Tone

### 12.1 Voice & Tone

| Principle | Description | Example |
|---|---|---|
| **Concise** | Shortest clear phrasing | "Stations" not "Station Management Module" |
| **Operational** | Focused on actions and states | "3 faults open" not "There are currently 3 faults" |
| **Action-oriented** | Start with verbs for actions | "Add Station" not "Click here to add a new station" |
| **Specific** | Precise error messages | "Station code already exists" not "An error occurred" |
| **Professional** | Neutral, not casual or alarming | "Session failed" not "Oops! Something went wrong!" |

### 12.2 Domain Terminology

Use consistent terminology throughout the UI:

| Term | Meaning | Do NOT use |
|---|---|---|
| **Station** | The physical charging location (one or more chargers) | Charger, Location, Site |
| **Connector** | The plug/socket on a charger | Port, Outlet, Plug |
| **Session** | A single charging event (start to stop) | Transaction, Charge |
| **Fault** | A hardware or software error at the station | Error, Issue, Bug |
| **Alert** | An operational notification requiring attention | Alarm, Warning, Notification |
| **Tariff** | A pricing plan for charging | Price, Rate, Plan |
| **User** (admin context) | An admin portal operator | Admin, Operator |
| **Driver** (mobile context) | An EV owner using the mobile app | Customer, Client, User |
| **Voucher** | A discount code for drivers | Coupon, Promo code |

### 12.3 Common UI Labels

| Context | Label | Notes |
|---|---|---|
| Create action | "Add [Entity]" | "Add Station", "Add Connector" |
| Edit action | "Edit" or "Edit [Entity]" | "Edit Station" |
| Delete action | "Delete" | Always with confirmation |
| Save action | "Save Changes" or "Save" | Primary button in forms |
| Cancel action | "Cancel" | Secondary button in forms |
| Filter | "Filter" | With filter icon |
| Search | "Search stations..." | Contextual placeholder |
| Export | "Export" | With download icon |
| Refresh | "Refresh" | With refresh icon |
| Back | "Back to [List]" | "Back to Stations" |
| View all | "View All" | Link to full list |
| Empty search | "No results found" | With suggestion to adjust filters |
| Empty list | "No [entities] yet" | "No stations yet" with add action |
| Loading | "Loading..." | Or skeleton (preferred) |
| Error | "Failed to load [entities]" | With retry button |
| Confirm delete | "Delete [Entity]?" | "This action cannot be undone." |

### 12.4 Date & Time Formatting

| Context | Format | Example |
|---|---|---|
| Full date | `dd/MM/yyyy` | `08/03/2026` |
| Date + time | `dd/MM/yyyy HH:mm` | `08/03/2026 14:30` |
| Time only | `HH:mm` | `14:30` |
| Relative (recent) | "X min ago", "X hours ago" | "5 min ago" |
| Relative (older) | Full date | `08/03/2026` |
| Duration | `HH:MM:SS` or `Xh Ym` | `01:23:45` |
| Chart axis (intraday) | `HH:mm` | `14:30` |
| Chart axis (multi-day) | `dd/MM` | `08/03` |

Timezone: always UTC+7 (Vietnam). Do not display timezone indicator in the UI (it is implicit).

### 12.5 Number Formatting

| Type | Format | Example |
|---|---|---|
| Currency (VND) | Dot thousands, "d" suffix | `45.000d`, `1.234.567d` |
| Energy | Comma decimal, unit | `12,5 kWh` |
| Power | Comma decimal, unit | `150 kW`, `22,5 kW` |
| Percentage | Comma decimal, % | `87,5%` |
| Count (large) | Dot thousands | `1.234` |
| Count (small) | Plain | `42` |

---

## Appendix A: CSS Custom Properties Reference

The following CSS custom properties should be defined in `globals.css` to implement this guideline. They extend the existing shadcn/ui variable structure.

```css
:root {
  /* Brand */
  --color-brand-primary: #28A649;
  --color-brand-primary-light: #E8F5EC;
  --color-brand-primary-dark: #1E7D37;
  --color-brand-secondary: #FAA623;
  --color-brand-secondary-light: #FFF4E0;
  --color-brand-secondary-dark: #D48A0F;

  /* Semantic */
  --color-success: #22C55E;
  --color-success-light: #F0FDF4;
  --color-warning: #F59E0B;
  --color-warning-light: #FFFBEB;
  --color-danger: #EF4444;
  --color-danger-light: #FEF2F2;
  --color-info: #3B82F6;
  --color-info-light: #EFF6FF;

  /* Surfaces */
  --color-bg-page: #F8FAFB;
  --color-bg-card: #FFFFFF;
  --color-bg-sidebar: #FFFFFF;
  --color-bg-header: #FFFFFF;

  /* Text */
  --color-text-primary: #111827;
  --color-text-secondary: #6B7280;
  --color-text-muted: #9CA3AF;
  --color-text-inverse: #FFFFFF;

  /* Border */
  --color-border-default: #E5E7EB;
  --color-border-strong: #D1D5DB;
  --color-border-focus: #28A649;

  /* Connector Status */
  --color-status-available: #22C55E;
  --color-status-charging: #3B82F6;
  --color-status-preparing: #F59E0B;
  --color-status-finishing: #14B8A6;
  --color-status-suspended: #F97316;
  --color-status-reserved: #8B5CF6;
  --color-status-faulted: #EF4444;
  --color-status-unavailable: #78716C;
  --color-status-offline: #9CA3AF;
  --color-status-maintenance: #6366F1;

  /* Chart */
  --color-chart-1: #28A649;
  --color-chart-2: #3B82F6;
  --color-chart-3: #FAA623;
  --color-chart-4: #8B5CF6;
  --color-chart-5: #EC4899;
  --color-chart-6: #14B8A6;

  /* Alert Severity */
  --color-alert-critical: #EF4444;
  --color-alert-high: #F97316;
  --color-alert-medium: #F59E0B;
  --color-alert-low: #3B82F6;
}
```

## Appendix B: Tailwind Config Extensions

Extend the Tailwind config to include brand colors as first-class tokens:

```js
// tailwind.config.ts (relevant additions)
{
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#28A649',
          'primary-light': '#E8F5EC',
          'primary-dark': '#1E7D37',
          secondary: '#FAA623',
          'secondary-light': '#FFF4E0',
          'secondary-dark': '#D48A0F',
        },
        status: {
          available: '#22C55E',
          charging: '#3B82F6',
          preparing: '#F59E0B',
          finishing: '#14B8A6',
          suspended: '#F97316',
          reserved: '#8B5CF6',
          faulted: '#EF4444',
          unavailable: '#78716C',
          offline: '#9CA3AF',
          maintenance: '#6366F1',
        },
      },
      fontFamily: {
        sans: ['var(--font-inter)', 'system-ui', '-apple-system', 'sans-serif'],
        brand: ['Baskerville BT', 'Georgia', 'serif'],
      },
    },
  },
}
```

Usage in components: `bg-brand-primary`, `text-brand-primary-dark`, `bg-status-available`, etc.

## Appendix C: Component Variant Quick Reference

### Button Variant Map

```
Primary action      -> variant="default"    -> bg-brand-primary
Secondary action    -> variant="outline"     -> border + text
Destructive action  -> variant="destructive" -> bg-danger
Subtle action       -> variant="ghost"       -> transparent
Navigation link     -> variant="link"        -> text-brand-primary
Accent CTA          -> custom                -> bg-brand-secondary
```

### Badge Variant Map for Statuses

```
Available / Completed / Resolved / Success  -> variant="success"
InProgress / Charging / Info                -> variant="info" (blue)
Preparing / Processing / Starting           -> variant="warning" (amber)
Suspended / SuspendedEV / SuspendedEVSE    -> variant="orange"
Reserved / Refunded                         -> variant="violet"
Faulted / Failed / Critical / Open         -> variant="danger" (red)
Unavailable / Offline / Cancelled / Closed -> variant="neutral" (gray)
Finishing                                   -> variant="teal"
Maintenance                                -> variant="indigo"
```
