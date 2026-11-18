import { useTranslations } from "next-intl";
import { statusKey } from "./build-rules";

const COLORS = {
  Draft: "bg-paper text-ink border border-input-line",
  Published: "bg-orange/15 text-orange-text",
  Sold: "bg-carbon text-on-carbon",
  Disassembled: "bg-line text-ink-muted",
  other: "bg-line text-ink-muted"
} as const;

/** Draft, Published, Sold or Disassembled; any other word the server may send reads as a generic label. */
export function BuildStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.builds.statuses");
  const key = statusKey(status);
  return <span className={`inline-block whitespace-nowrap rounded px-2 py-[3px] text-xs font-bold ${COLORS[key]}`}>{t(key)}</span>;
}
