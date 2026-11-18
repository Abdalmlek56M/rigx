import { useTranslations } from "next-intl";
import { statusKey } from "./purchase-rules";

const COLORS = {
  Draft: "bg-paper text-ink border border-input-line",
  Received: "bg-orange/15 text-orange-text",
  Cancelled: "bg-line text-ink-muted",
  other: "bg-line text-ink-muted"
} as const;

/** Draft, Received or Cancelled; any other word the server may send reads as a generic label. */
export function PurchaseStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.purchases.statuses");
  const key = statusKey(status);
  return <span className={`inline-block whitespace-nowrap rounded px-2 py-[3px] text-xs font-bold ${COLORS[key]}`}>{t(key)}</span>;
}
