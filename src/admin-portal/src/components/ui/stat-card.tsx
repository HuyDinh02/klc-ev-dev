import type React from "react";
import { type LucideIcon, ArrowUpRight, ArrowDownRight } from "lucide-react";
import { Card, CardContent } from "./card";
import { cn } from "@/lib/utils";

interface StatCardProps {
  label: string;
  value: string | number;
  icon?: LucideIcon;
  trend?: {
    value: number;
    direction: "up" | "down";
    label?: string;
  };
  iconColor?: string;
  className?: string;
  onClick?: () => void;
  children?: React.ReactNode;
}

export function StatCard({ label, value, icon: Icon, trend, iconColor, className, onClick, children }: StatCardProps) {
  const TrendIcon = trend?.direction === "up" ? ArrowUpRight : ArrowDownRight;
  const trendColor = trend?.direction === "up" ? "text-green-600" : "text-red-600";
  const trendDescription = trend
    ? `${trend.direction === "up" ? "Up" : "Down"} ${Math.abs(trend.value)}%${trend.label ? ` ${trend.label}` : ""}`
    : undefined;

  const handleKeyDown = onClick
    ? (e: React.KeyboardEvent) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onClick();
        }
      }
    : undefined;

  return (
    <Card
      className={cn(
        "transition-colors",
        onClick && "cursor-pointer hover:border-primary/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
        className
      )}
      onClick={onClick}
      {...(onClick && { role: "button" as const, tabIndex: 0, onKeyDown: handleKeyDown })}
    >
      <CardContent className="p-5">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <p className="kpi-label">{label}</p>
            <p className="kpi-value">{value}</p>
            {trend && (
              <div
                className={cn("flex items-center gap-1 text-xs font-medium", trendColor)}
                aria-label={trendDescription}
              >
                <TrendIcon className="h-3 w-3" aria-hidden="true" />
                <span>{Math.abs(trend.value)}%</span>
                {trend.label && <span className="text-muted-foreground">{trend.label}</span>}
              </div>
            )}
            {children}
          </div>
          {Icon && (
            <div className={cn("rounded-lg p-2.5", iconColor || "bg-primary/10 text-primary")}>
              <Icon className="h-5 w-5" aria-hidden="true" />
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
