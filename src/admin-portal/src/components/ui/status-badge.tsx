import { cn } from "@/lib/utils";
import {
  CONNECTOR_STATUS,
  STATION_STATUS,
  SESSION_STATUS,
  PAYMENT_STATUS,
  FAULT_SEVERITY,
  FAULT_STATUS,
  ALERT_SEVERITY,
  MAINTENANCE_STATUS,
  type StatusConfig,
} from "@/lib/constants";
import { Badge } from "./badge";

type StatusType =
  | "connector"
  | "station"
  | "session"
  | "payment"
  | "faultSeverity"
  | "faultStatus"
  | "alertSeverity"
  | "maintenance";

const STATUS_MAPS: Record<StatusType, Record<number, StatusConfig>> = {
  connector: CONNECTOR_STATUS,
  station: STATION_STATUS,
  session: SESSION_STATUS,
  payment: PAYMENT_STATUS,
  faultSeverity: FAULT_SEVERITY,
  faultStatus: FAULT_STATUS,
  alertSeverity: ALERT_SEVERITY,
  maintenance: MAINTENANCE_STATUS,
};

interface StatusBadgeProps {
  type: StatusType;
  value: number;
  showIcon?: boolean;
  showDot?: boolean;
  className?: string;
}

export function StatusBadge({ type, value, showIcon = false, showDot = true, className }: StatusBadgeProps) {
  const config = STATUS_MAPS[type]?.[value];
  if (!config) {
    return <Badge variant="secondary" className={className}>Unknown</Badge>;
  }

  const Icon = config.icon;

  return (
    <Badge variant={config.badgeVariant} className={cn("gap-1.5", className)}>
      {showDot && !showIcon && (
        <span
          className="status-dot flex-shrink-0"
          style={{ backgroundColor: config.dotColor }}
        />
      )}
      {showIcon && <Icon className="h-3 w-3 flex-shrink-0" />}
      {config.label}
    </Badge>
  );
}

interface StatusDotProps {
  type: StatusType;
  value: number;
  pulse?: boolean;
  className?: string;
}

export function StatusDot({ type, value, pulse = false, className }: StatusDotProps) {
  const config = STATUS_MAPS[type]?.[value];
  if (!config) return null;

  return (
    <span
      className={cn("status-dot", pulse && "status-dot-pulse", className)}
      style={{ backgroundColor: config.dotColor }}
      title={config.label}
    />
  );
}

export function getStatusConfig(type: StatusType, value: number): StatusConfig | undefined {
  return STATUS_MAPS[type]?.[value];
}
