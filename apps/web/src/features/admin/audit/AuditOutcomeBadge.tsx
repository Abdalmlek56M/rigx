"use client";

import { useTranslations } from "next-intl";
import { auditOutcomeKey, type AuditOutcomeKey } from "./audit-rules";

const TONES: Record<AuditOutcomeKey, string> = {
  Succeeded: "bg-success text-surface",
  Failed: "bg-error text-surface",
  Rejected: "bg-orange/15 text-orange-text",
  Recorded: "bg-line text-ink-muted",
  Other: "bg-line text-ink-muted"
};

export function AuditOutcomeBadge({ outcome }: { outcome: string }) {
  const t = useTranslations("admin.audit.outcomes");
  const key = auditOutcomeKey(outcome);
  return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${TONES[key]}`}>{t(key)}</span>;
}
