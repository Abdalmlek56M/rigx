"use client";

import { useTranslations } from "next-intl";
import { ORDER_STATUSES, orderStatusKey, paymentStatusKey, type OrderStatus, type PaymentStatus } from "./order-rules";

const BADGE = "inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold";

const STATUS_TONES: Record<OrderStatus, string> = {
  New: "bg-orange text-on-orange",
  Confirmed: "bg-paper text-orange-text",
  Preparing: "bg-paper text-orange-text",
  Shipped: "bg-carbon text-on-carbon",
  Delivered: "bg-success text-surface",
  Completed: "bg-success text-surface",
  Cancelled: "bg-error text-surface"
};

const PAYMENT_TONES: Record<PaymentStatus, string> = {
  Unpaid: "bg-line text-ink-muted",
  PendingVerification: "bg-orange/15 text-orange-text",
  Paid: "bg-success text-surface",
  Refunded: "bg-paper text-ink-muted",
  Waived: "bg-paper text-ink-muted"
};

export function OrderStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.orders.statuses");
  const key = orderStatusKey(status);
  return <span className={`${BADGE} ${STATUS_TONES[key]}`}>{t(key)}</span>;
}

export function PaymentStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.orders.paymentStatuses");
  const key = paymentStatusKey(status);
  return <span className={`${BADGE} ${PAYMENT_TONES[key]}`}>{t(key)}</span>;
}

/** The status filter as a segmented control (All + every status). */
export function OrderTabs({ active, onChange }: { active: string; onChange: (status: string) => void }) {
  const t = useTranslations("admin.orders");
  const tabs: ("" | OrderStatus)[] = ["", ...ORDER_STATUSES];
  return (
    <div role="group" aria-label={t("tabsLabel")} className="flex max-w-full flex-wrap gap-1 rounded-card border border-line bg-surface p-1">
      {tabs.map((status) => (
        <button
          key={status || "all"} type="button" aria-pressed={active === status} onClick={() => onChange(status)}
          className={`flex h-10 items-center whitespace-nowrap rounded-control px-3.5 text-[13px] ${active === status ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
        >
          {t(`statuses.${status || "All"}`)}
        </button>
      ))}
    </div>
  );
}
