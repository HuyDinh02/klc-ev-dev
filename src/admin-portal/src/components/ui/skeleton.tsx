import { cn } from "@/lib/utils";

interface SkeletonProps {
  className?: string;
}

export function Skeleton({ className }: SkeletonProps) {
  return <div aria-hidden="true" className={cn("skeleton", className)} />;
}

export function SkeletonCard() {
  return (
    <div role="status" aria-label="Loading" className="rounded-lg border bg-card p-5 space-y-3">
      <Skeleton className="h-3 w-24" />
      <Skeleton className="h-7 w-20" />
      <Skeleton className="h-3 w-16" />
      <span className="sr-only">Loading...</span>
    </div>
  );
}

export function SkeletonTable({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <div role="status" aria-label="Loading table" className="rounded-lg border bg-card">
      <div className="flex gap-4 border-b px-4 py-3">
        {Array.from({ length: cols }).map((_, i) => (
          <Skeleton key={i} className="h-4 flex-1" />
        ))}
      </div>
      {Array.from({ length: rows }).map((_, r) => (
        <div key={r} className="flex gap-4 border-b last:border-0 px-4 py-3">
          {Array.from({ length: cols }).map((_, c) => (
            <Skeleton key={c} className="h-4 flex-1" />
          ))}
        </div>
      ))}
      <span className="sr-only">Loading table data...</span>
    </div>
  );
}

export function SkeletonChart() {
  return (
    <div role="status" aria-label="Loading chart" className="rounded-lg border bg-card p-6 space-y-4">
      <Skeleton className="h-4 w-32" />
      <Skeleton className="h-48 w-full" />
      <span className="sr-only">Loading chart...</span>
    </div>
  );
}
