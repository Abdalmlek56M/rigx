"use client";

import { useTranslations } from "next-intl";
import { policyStatusKey, type PolicyStatusKey } from "./policy-rules";

const TONES: Record<PolicyStatusKey, string> = {
  Draft: "bg-line text-ink-muted",
  Review: "bg-orange/15 text-orange-text",
  Published: "bg-success text-surface",
  Archived: "bg-line text-ink-muted",
  Other: "bg-line text-ink-muted"
};

export function PolicyStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.policies.statuses");
  const key = policyStatusKey(status);
  return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${TONES[key]}`}>{t(key)}</span>;
}
