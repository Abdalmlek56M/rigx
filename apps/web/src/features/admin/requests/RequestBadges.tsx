"use client";

import { useTranslations } from "next-intl";
import { requestStatusKey, requestTypeKey, type RequestStatusKey } from "./request-labels";

const TYPE_BADGE = "inline-block whitespace-nowrap rounded-[4px] border border-input-line bg-surface px-2 py-[3px] text-xs font-bold";

const STATUS_TONES: Record<RequestStatusKey, string> = {
  New: "bg-orange text-on-orange",
  Following: "bg-paper text-ink-muted",
  Offered: "bg-paper text-orange-text",
  Accepted: "bg-success text-surface",
  Rejected: "bg-error text-surface",
  Converted: "bg-carbon text-on-carbon",
  Closed: "bg-line text-ink-muted",
  Other: "bg-line text-ink-muted"
};

export function RequestTypeBadge({ type }: { type: string }) {
  const t = useTranslations("admin.requests.types");
  return <span className={TYPE_BADGE}>{t(requestTypeKey(type))}</span>;
}

export function RequestStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.requests.statuses");
  const key = requestStatusKey(status);
  return (
    <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${STATUS_TONES[key]}`}>
      {t(key)}
    </span>
  );
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
    ...(["Sell", "TradeIn", "Upgrade", "NotifyMe", "Support", "Contact", "Service"] as const).map((type) => ({
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
            <span aria-label={t("newCount", { count: tab.count })} className={`rounded-[4px] px-1.5 text-[11px] font-bold ${active === tab.value ? "bg-on-carbon/15" : "bg-orange/15 text-orange-text"}`}>
              {tab.count}
            </span>
          ) : null}
        </button>
      ))}
    </div>
  );
}
