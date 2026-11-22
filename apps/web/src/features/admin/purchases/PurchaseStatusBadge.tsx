import { useTranslations } from "next-intl";
import { statusKey } from "./purchase-rules";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";

const TONES = { Draft: "neutral", Received: "warning", Cancelled: "neutral", other: "neutral" } as const satisfies Record<string, StatusTone>;

/** Draft, Received or Cancelled; any other word the server may send reads as a generic label. */
export function PurchaseStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.purchases.statuses");
  const key = statusKey(status);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}
