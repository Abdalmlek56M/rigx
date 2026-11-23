import type { ComponentType } from "react";
import { cardClass } from "@/components/ui/Card";
import { InboxIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "./Button";

/** A card-shaped placeholder that holds the place of a panel while it loads (reduced motion stops the pulse). */
export function LoadingCard({ className = "h-64", label }: { className?: string; label?: string }) {
  const shape = `animate-pulse motion-reduce:animate-none ${cardClass({ padding: "none", className })}`;
  return label ? <div role="status" aria-label={label} className={shape} /> : <div aria-hidden="true" className={shape} />;
}

/** A failed load, in the store's card: what failed and one retry action. */
export function ErrorPanel({ message, retryLabel, onRetry }: { message: string; retryLabel: string; onRetry: () => void }) {
  return (
    <div role="alert" className={cardClass({ className: "items-start gap-3" })}>
      <p className="m-0 font-bold">{message}</p>
      <Button variant="primary" icon={RefreshIcon} onClick={onRetry}>{retryLabel}</Button>
    </div>
  );
}

/** Nothing to list: an outline icon in a circle and one line, in the store's card. */
export function EmptyPanel({ message, icon: Icon = InboxIcon }: { message: string; icon?: ComponentType<{ size?: number }> }) {
  return (
    <div className={cardClass({ className: "items-center gap-2 text-center text-ink-muted" })}>
      <span aria-hidden="true" className="flex size-11 items-center justify-center rounded-full bg-paper"><Icon size={22} /></span>
      <p className="m-0 text-sm font-bold">{message}</p>
    </div>
  );
}
