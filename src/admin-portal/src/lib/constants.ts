import {
  Circle,
  Zap,
  Clock,
  CheckCircle,
  XCircle,
  AlertTriangle,
  MinusCircle,
  Pause,
  Lock,
  WifiOff,
  Wrench,
  Ban,
  type LucideIcon,
  AlertOctagon,
  Info,
  CreditCard,
  RefreshCw,
  ArrowUpRight,
  ArrowDownRight,
  Play,
} from "lucide-react";

// ============================================
// Status Configuration System
// ============================================

export interface StatusConfig {
  label: string;
  color: string;        // Tailwind text color class
  bgColor: string;      // Tailwind bg color class
  dotColor: string;     // CSS hex for dots/markers
  icon: LucideIcon;
  badgeVariant: "default" | "secondary" | "destructive" | "outline" | "success" | "warning" | "info" | "brand";
}

// --- Connector Status (OCPP) ---
export const CONNECTOR_STATUS: Record<number, StatusConfig> = {
  0: { label: "Available",     color: "text-green-600",  bgColor: "bg-green-50",   dotColor: "#22C55E", icon: CheckCircle,    badgeVariant: "success" },
  1: { label: "Preparing",     color: "text-amber-600",  bgColor: "bg-amber-50",   dotColor: "#F59E0B", icon: Clock,          badgeVariant: "warning" },
  2: { label: "Charging",      color: "text-blue-600",   bgColor: "bg-blue-50",    dotColor: "#3B82F6", icon: Zap,            badgeVariant: "info" },
  3: { label: "SuspendedEV",   color: "text-orange-600", bgColor: "bg-orange-50",  dotColor: "#F97316", icon: Pause,          badgeVariant: "warning" },
  4: { label: "SuspendedEVSE", color: "text-orange-600", bgColor: "bg-orange-50",  dotColor: "#F97316", icon: Pause,          badgeVariant: "warning" },
  5: { label: "Finishing",     color: "text-teal-600",   bgColor: "bg-teal-50",    dotColor: "#14B8A6", icon: CheckCircle,    badgeVariant: "success" },
  6: { label: "Reserved",      color: "text-violet-600", bgColor: "bg-violet-50",  dotColor: "#8B5CF6", icon: Lock,           badgeVariant: "default" },
  7: { label: "Unavailable",   color: "text-stone-600",  bgColor: "bg-stone-50",   dotColor: "#78716C", icon: MinusCircle,    badgeVariant: "secondary" },
  8: { label: "Faulted",       color: "text-red-600",    bgColor: "bg-red-50",     dotColor: "#EF4444", icon: XCircle,        badgeVariant: "destructive" },
};

// --- Station Status ---
export const STATION_STATUS: Record<number, StatusConfig> = {
  0: { label: "Offline",         color: "text-gray-500",   bgColor: "bg-gray-50",    dotColor: "#9CA3AF", icon: WifiOff,        badgeVariant: "secondary" },
  1: { label: "Available",       color: "text-green-600",  bgColor: "bg-green-50",   dotColor: "#22C55E", icon: CheckCircle,    badgeVariant: "success" },
  2: { label: "Occupied",        color: "text-blue-600",   bgColor: "bg-blue-50",    dotColor: "#3B82F6", icon: Zap,            badgeVariant: "info" },
  3: { label: "Unavailable",     color: "text-stone-600",  bgColor: "bg-stone-50",   dotColor: "#78716C", icon: MinusCircle,    badgeVariant: "secondary" },
  4: { label: "Faulted",         color: "text-red-600",    bgColor: "bg-red-50",     dotColor: "#EF4444", icon: XCircle,        badgeVariant: "destructive" },
  5: { label: "Decommissioned",  color: "text-gray-400",   bgColor: "bg-gray-50",    dotColor: "#D1D5DB", icon: Ban,            badgeVariant: "secondary" },
};

// --- Session Status ---
export const SESSION_STATUS: Record<number, StatusConfig> = {
  0: { label: "Pending",     color: "text-gray-500",   bgColor: "bg-gray-50",    dotColor: "#9CA3AF", icon: Clock,          badgeVariant: "secondary" },
  1: { label: "Starting",    color: "text-amber-600",  bgColor: "bg-amber-50",   dotColor: "#F59E0B", icon: Play,           badgeVariant: "warning" },
  2: { label: "In Progress", color: "text-blue-600",   bgColor: "bg-blue-50",    dotColor: "#3B82F6", icon: Zap,            badgeVariant: "info" },
  3: { label: "Suspended",   color: "text-orange-600", bgColor: "bg-orange-50",  dotColor: "#F97316", icon: Pause,          badgeVariant: "warning" },
  4: { label: "Stopping",    color: "text-amber-600",  bgColor: "bg-amber-50",   dotColor: "#F59E0B", icon: Clock,          badgeVariant: "warning" },
  5: { label: "Completed",   color: "text-green-600",  bgColor: "bg-green-50",   dotColor: "#22C55E", icon: CheckCircle,    badgeVariant: "success" },
  6: { label: "Failed",      color: "text-red-600",    bgColor: "bg-red-50",     dotColor: "#EF4444", icon: XCircle,        badgeVariant: "destructive" },
};

// --- Payment Status ---
export const PAYMENT_STATUS: Record<number, StatusConfig> = {
  0: { label: "Pending",    color: "text-amber-600",  bgColor: "bg-amber-50",   dotColor: "#F59E0B", icon: Clock,          badgeVariant: "warning" },
  1: { label: "Processing", color: "text-blue-600",   bgColor: "bg-blue-50",    dotColor: "#3B82F6", icon: RefreshCw,      badgeVariant: "info" },
  2: { label: "Completed",  color: "text-green-600",  bgColor: "bg-green-50",   dotColor: "#22C55E", icon: CheckCircle,    badgeVariant: "success" },
  3: { label: "Failed",     color: "text-red-600",    bgColor: "bg-red-50",     dotColor: "#EF4444", icon: XCircle,        badgeVariant: "destructive" },
  4: { label: "Refunded",   color: "text-gray-500",   bgColor: "bg-gray-50",    dotColor: "#9CA3AF", icon: RefreshCw,      badgeVariant: "secondary" },
};

// --- Fault Severity ---
export const FAULT_SEVERITY: Record<number, StatusConfig> = {
  1: { label: "Critical", color: "text-red-600",    bgColor: "bg-red-50",    dotColor: "#EF4444", icon: AlertOctagon,   badgeVariant: "destructive" },
  2: { label: "High",     color: "text-orange-600", bgColor: "bg-orange-50", dotColor: "#F97316", icon: AlertTriangle,  badgeVariant: "warning" },
  3: { label: "Medium",   color: "text-amber-600",  bgColor: "bg-amber-50",  dotColor: "#F59E0B", icon: AlertTriangle,  badgeVariant: "warning" },
  4: { label: "Low",      color: "text-blue-600",   bgColor: "bg-blue-50",   dotColor: "#3B82F6", icon: Info,           badgeVariant: "info" },
};

// --- Fault Status ---
export const FAULT_STATUS: Record<number, StatusConfig> = {
  0: { label: "Open",          color: "text-red-600",    bgColor: "bg-red-50",    dotColor: "#EF4444", icon: AlertOctagon,   badgeVariant: "destructive" },
  1: { label: "Investigating", color: "text-amber-600",  bgColor: "bg-amber-50",  dotColor: "#F59E0B", icon: RefreshCw,      badgeVariant: "warning" },
  2: { label: "Resolved",      color: "text-green-600",  bgColor: "bg-green-50",  dotColor: "#22C55E", icon: CheckCircle,    badgeVariant: "success" },
  3: { label: "Closed",        color: "text-gray-500",   bgColor: "bg-gray-50",   dotColor: "#9CA3AF", icon: MinusCircle,    badgeVariant: "secondary" },
};

// --- Alert Severity ---
export const ALERT_SEVERITY: Record<number, StatusConfig> = {
  1: { label: "Critical", color: "text-red-600",    bgColor: "bg-red-50",    dotColor: "#EF4444", icon: AlertOctagon,   badgeVariant: "destructive" },
  2: { label: "Warning",  color: "text-amber-600",  bgColor: "bg-amber-50",  dotColor: "#F59E0B", icon: AlertTriangle,  badgeVariant: "warning" },
  3: { label: "Info",     color: "text-blue-600",   bgColor: "bg-blue-50",   dotColor: "#3B82F6", icon: Info,           badgeVariant: "info" },
};

// --- Maintenance Task Status ---
export const MAINTENANCE_STATUS: Record<number, StatusConfig> = {
  0: { label: "Scheduled",   color: "text-blue-600",   bgColor: "bg-blue-50",   dotColor: "#3B82F6", icon: Clock,        badgeVariant: "info" },
  1: { label: "In Progress", color: "text-amber-600",  bgColor: "bg-amber-50",  dotColor: "#F59E0B", icon: RefreshCw,    badgeVariant: "warning" },
  2: { label: "Completed",   color: "text-green-600",  bgColor: "bg-green-50",  dotColor: "#22C55E", icon: CheckCircle,  badgeVariant: "success" },
  3: { label: "Cancelled",   color: "text-gray-500",   bgColor: "bg-gray-50",   dotColor: "#9CA3AF", icon: XCircle,      badgeVariant: "secondary" },
};

// ============================================
// Chart Colors (Brand Palette)
// ============================================

export const CHART_COLORS = {
  green:  "#28A649",
  blue:   "#3B82F6",
  orange: "#FAA623",
  purple: "#8B5CF6",
  pink:   "#EC4899",
  teal:   "#14B8A6",
} as const;

export const CHART_PALETTE = [
  CHART_COLORS.green,
  CHART_COLORS.blue,
  CHART_COLORS.orange,
  CHART_COLORS.purple,
  CHART_COLORS.pink,
  CHART_COLORS.teal,
];

// ============================================
// Trend Indicators
// ============================================

export const TREND_CONFIG = {
  up:   { icon: ArrowUpRight, color: "text-green-600", label: "Up" },
  down: { icon: ArrowDownRight, color: "text-red-600", label: "Down" },
} as const;

// ============================================
// Connector Type Labels
// ============================================

export const CONNECTOR_TYPE_LABELS: Record<number, string> = {
  0: "Type 1",
  1: "Type 2",
  2: "CCS1",
  3: "CCS2",
  4: "CHAdeMO",
  5: "GBT",
};

// ============================================
// Payment Gateway Labels
// ============================================

export const PAYMENT_GATEWAY_LABELS: Record<number, string> = {
  0: "ZaloPay",
  1: "MoMo",
  2: "OnePay",
  3: "Wallet",
  4: "VnPay",
  5: "QR Payment",
  6: "Voucher",
  7: "Urbox",
};
