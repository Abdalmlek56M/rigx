import type { ComponentType, ReactNode } from "react";
import type { StatusTone } from "./status-badge-rules";

const TONES: Record<StatusTone, string> = {
  neutral: "bg-line text-ink",
  success: "bg-success text-surface",
  warning: "accent-strong",
  danger: "bg-error text-white",
  info: "surface-strong"
};

interface StatusBadgeProps {
  tone: StatusTone;
  /** A small mark before the word (check, clock, alert). */
  icon?: ComponentType<{ size?: number }>;
  children: ReactNode;
}

/** One pill for every status on the site: order, warranty, request, payment, condition and void invoices. */
export function StatusBadge({ tone, icon: Icon, children }: StatusBadgeProps) {
  return (
    <span className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-bold ${TONES[tone]}`}>
      {Icon ? <Icon size={13} /> : null}
      {children}
    </span>
  );
}
