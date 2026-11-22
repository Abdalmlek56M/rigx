import { useTranslations } from "next-intl";
import { statusKey } from "./build-rules";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";

const TONES = { Draft: "neutral", Published: "warning", Sold: "info", Disassembled: "neutral", other: "neutral" } as const satisfies Record<string, StatusTone>;

/** Draft, Published, Sold or Disassembled; any other word the server may send reads as a generic label. */
export function BuildStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.builds.statuses");
  const key = statusKey(status);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}
