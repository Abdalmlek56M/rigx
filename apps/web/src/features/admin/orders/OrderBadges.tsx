"use client";

import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { ORDER_STATUSES, orderStatusKey, paymentStatusKey, type OrderStatus, type PaymentStatus } from "./order-rules";

const STATUS_TONES: Record<OrderStatus, StatusTone> = {
  New: "warning",
  Confirmed: "info",
  Preparing: "info",
  Shipped: "info",
  Delivered: "success",
  Completed: "success",
  Cancelled: "danger"
};

const PAYMENT_TONES: Record<PaymentStatus, StatusTone> = { Unpaid: "neutral", PendingVerification: "warning", Paid: "success", Refunded: "neutral", Waived: "neutral" };

export function OrderStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.orders.statuses");
  const key = orderStatusKey(status);
  return <StatusBadge tone={STATUS_TONES[key]}>{t(key)}</StatusBadge>;
}

export function PaymentStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.orders.paymentStatuses");
  const key = paymentStatusKey(status);
  return <StatusBadge tone={PAYMENT_TONES[key]}>{t(key)}</StatusBadge>;
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
