"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { changeRows } from "./audit-rules";
import { fetchEntry } from "./audit-api";
import { AuditField as Row } from "./AuditField";
import { AuditRestore } from "./AuditRestore";
import { useAuditLabels } from "./use-audit-labels";
import { usePermissions } from "../use-permissions";
import { CloseIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** One data-change entry: every field of the entry and its before → after rows. */
export function AuditEntryDetail({ id, onClose, onRestored }: { id: string; onClose: () => void; onRestored: () => void }) {
  const t = useTranslations("admin.audit");
  const labels = useAuditLabels();
  const locale = useLocale();
  const load = useCallback(() => fetchEntry(id), [id]);
  const { state, reload } = useLoad(load);
  const { has } = usePermissions();
  if (state.status === "loading") return <div aria-hidden="true" className="h-40 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("failed")}</p>
        <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
      </div>
    );
  }

  const entry = state.data;
  const rows = changeRows(entry.changes);
  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("changes.title")}</h2>
        <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
      </header>
      <dl className="m-0 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Row label={t("changes.time")} value={formatDateTime(entry.occurredAtUtc, locale)} />
        <Row label={t("changes.actor")} value={entry.actorName ?? labels.actor(entry.actorKind)} />
        <Row label={t("changes.actorKind")} value={labels.actor(entry.actorKind)} />
        <Row label={t("changes.actorId")} value={entry.actorUserId} mono />
        <Row label={t("changes.action")} value={labels.action(entry.action)} />
        <Row label={t("changes.module")} value={labels.module(entry.module)} />
        <Row label={t("changes.entity")} value={labels.entity(entry.entity)} />
        <Row label={t("changes.entityId")} value={entry.entityId} mono />
        <Row label={t("changes.ip")} value={entry.actorIp} mono />
        <Row label={t("changes.device")} value={entry.device} />
        <Row label={t("changes.correlation")} value={entry.correlationId} mono />
        <Row label={t("changes.restoredFrom")} value={entry.restoredFromEntryId} mono />
      </dl>
      <div className="flex flex-col gap-2 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("changes.fields")}</h3>
        {rows.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("changes.noChanges")}</p> : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {rows.map((row) => (
              <li key={row.field} className="flex flex-col gap-1 rounded-control border border-line bg-paper p-2.5">
                <span dir="ltr" className="font-mono text-xs font-bold text-ink-muted">{row.field}</span>
                <div className="grid gap-1 text-sm sm:grid-cols-2">
                  <span className="flex flex-col gap-0.5"><span className="text-xs text-ink-muted">{t("changes.before")}</span><span dir="auto" className="[overflow-wrap:anywhere]">{row.before || t("none")}</span></span>
                  <span className="flex flex-col gap-0.5"><span className="text-xs text-ink-muted">{t("changes.after")}</span><span dir="auto" className="font-bold [overflow-wrap:anywhere]">{row.after || t("none")}</span></span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
      {has("Audit.Restore") && entry.canRestore && rows.length > 0 ? <AuditRestore id={entry.id} onRestored={() => { reload(); onRestored(); }} /> : null}
      <p className="m-0 text-xs text-ink-muted [overflow-wrap:anywhere]">{t("changes.idLine", { id: entry.id })}</p>
    </section>
  );
}
