"use client";

import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { REQUEST_TYPES, requestStatusKey, requestTypeKey, type RequestStatusKey } from "./request-labels";

const STATUS_TONES: Record<RequestStatusKey, StatusTone> = {
  New: "warning",
  Following: "neutral",
  Offered: "info",
  Accepted: "success",
  Rejected: "danger",
  Converted: "info",
  Closed: "neutral",
  Other: "neutral"
};

export function RequestTypeBadge({ type }: { type: string }) {
  const t = useTranslations("admin.requests.types");
  return <StatusBadge tone="neutral">{t(requestTypeKey(type))}</StatusBadge>;
}

export function RequestStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.requests.statuses");
  const key = requestStatusKey(status);
  return <StatusBadge tone={STATUS_TONES[key]}>{t(key)}</StatusBadge>;
}

/** The type filter as a segmented control; the badge counts the requests still New (the only counts the API returns). */
export function RequestTabs({ active, counts, onChange }: {
  active: string;
  counts: Record<string, number>;
  onChange: (type: string) => void;
}) {
  const t = useTranslations("admin.requests");
  const labels = useTranslations("admin.requests.types");
  const tabs: { value: string; label: string; count: number }[] = [
    { value: "", label: t("types.All"), count: Object.values(counts).reduce((sum, value) => sum + value, 0) },
    ...(REQUEST_TYPES).map((type) => ({
      value: type,
      label: labels(type),
      count: counts[type] ?? 0
    }))
  ];
  return (
    <div role="group" aria-label={t("tabsLabel")} className="flex max-w-full flex-wrap gap-1 rounded-card border border-line bg-surface p-1">
      {tabs.map((tab) => (
        <button
          key={tab.value || "all"} type="button" aria-pressed={active === tab.value} onClick={() => onChange(tab.value)}
          className={`flex h-10 items-center gap-1.5 whitespace-nowrap rounded-control px-3.5 text-[13px] ${active === tab.value ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
        >
          {tab.label}
          {tab.count > 0 ? (
            <span aria-label={t("newCount", { count: tab.count })}><StatusBadge tone="warning">{tab.count}</StatusBadge></span>
          ) : null}
        </button>
      ))}
    </div>
  );
}
