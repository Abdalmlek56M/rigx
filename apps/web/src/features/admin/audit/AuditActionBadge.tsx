"use client";

import { useAuditLabels } from "./use-audit-labels";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";

const ACTION_TONE: Readonly<Record<string, StatusTone>> = { Created: "success", Deleted: "danger", SoftDeleted: "danger" };

/** The action as a small coloured tag: additions green, deletions red, the rest neutral. */
export function AuditActionBadge({ action }: { action: string }) {
  const labels = useAuditLabels();
  const tone = (Object.hasOwn(ACTION_TONE, action) ? ACTION_TONE[action] : undefined) ?? "neutral";
  return <StatusBadge tone={tone}>{labels.action(action)}</StatusBadge>;
}
