"use client";

import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { auditOutcomeKey, type AuditOutcomeKey } from "./audit-rules";

const TONES: Record<AuditOutcomeKey, StatusTone> = { Succeeded: "success", Failed: "danger", Rejected: "warning", Recorded: "neutral", Other: "neutral" };

export function AuditOutcomeBadge({ outcome }: { outcome: string }) {
  const t = useTranslations("admin.audit.outcomes");
  const key = auditOutcomeKey(outcome);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}
