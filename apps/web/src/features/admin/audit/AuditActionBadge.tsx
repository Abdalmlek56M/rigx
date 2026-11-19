"use client";

import { useAuditLabels } from "./use-audit-labels";

const ACTION_TONE: Readonly<Record<string, string>> = {
  Created: "border-success/40 text-success",
  Deleted: "border-error/40 text-error",
  SoftDeleted: "border-error/40 text-error"
};

/** The action as a small coloured tag: additions green, deletions red, the rest neutral. */
export function AuditActionBadge({ action }: { action: string }) {
  const labels = useAuditLabels();
  const tone = (Object.hasOwn(ACTION_TONE, action) ? ACTION_TONE[action] : undefined) ?? "border-input-line text-ink";
  return <span className={`inline-block shrink-0 whitespace-nowrap rounded-[4px] border bg-surface px-2 py-[3px] text-[11px] font-bold ${tone}`}>{labels.action(action)}</span>;
}
