/* eslint-disable @next/next/no-img-element */
export default function Loading() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-3">
        <img src="/logo-icon.svg" alt="K-Charge" className="h-12 w-12 animate-pulse" />
        <div className="h-1 w-24 overflow-hidden rounded-full bg-muted">
          <div className="h-full w-1/2 animate-[shimmer_1.5s_ease-in-out_infinite] rounded-full bg-primary/60" />
        </div>
      </div>
    </div>
  );
}
