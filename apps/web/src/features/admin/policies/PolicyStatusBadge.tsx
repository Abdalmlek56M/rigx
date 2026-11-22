"use client";

import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { policyStatusKey, type PolicyStatusKey } from "./policy-rules";

const TONES: Record<PolicyStatusKey, StatusTone> = { Draft: "neutral", Review: "warning", Published: "success", Archived: "neutral", Other: "neutral" };

export function PolicyStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.policies.statuses");
  const key = policyStatusKey(status);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}
