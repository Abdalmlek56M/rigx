"use client";

import { useLocale } from "next-intl";
import { formatTime } from "@/lib/format";
import { AuditActionBadge } from "./AuditActionBadge";
import type { AuditEntry } from "./audit-api";
import { entryTitle } from "./audit-rules";
import { useAuditLabels } from "./use-audit-labels";

/** One change as a sentence: what happened to which record, by whom and when. */
export function AuditEntryRow({ entry, picked, onPick }: { entry: AuditEntry; picked: boolean; onPick: (id: string) => void }) {
  const labels = useAuditLabels();
  const locale = useLocale();
  const title = entryTitle(entry.changes);
  return (
    <li>
      <button
        type="button" aria-current={picked ? "true" : undefined} onClick={() => onPick(entry.id)}
        className={`flex w-full min-w-0 items-start gap-3 px-4 py-3 text-start transition-colors hover:bg-paper ${picked ? "bg-orange/10 hover:bg-orange/10" : ""}`}
      >
        <AuditActionBadge action={entry.action} />
        <span className="flex min-w-0 flex-1 flex-col gap-0.5">
          <span className="text-sm">
            <span className="font-bold">{labels.entity(entry.entity)}</span>
            {title ? <> «<span dir="auto" className="[overflow-wrap:anywhere]">{title}</span>»</> : null}
          </span>
          <span className="text-xs text-ink-muted">{entry.actorName ?? labels.actor(entry.actorKind)}</span>
        </span>
        <span className="shrink-0 text-xs text-ink-muted">{formatTime(entry.occurredAtUtc, locale)}</span>
      </button>
    </li>
  );
}
