"use client";

import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { AuditOutcomeBadge } from "./AuditOutcomeBadge";
import type { SecurityEvent } from "./audit-api";
import { AuditField as Row } from "./AuditField";

/** One sign-in/security event: every field of the row and its safe metadata. */
export function SecurityEventDetail({ event, onClose }: { event: SecurityEvent; onClose: () => void }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const metadata = Object.entries(event.metadata);

  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("security.title")}</h2>
        <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("close")}</button>
      </header>
      <dl className="m-0 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <Row label={t("security.id")} value={event.id} mono />
        <Row label={t("columns.time")} value={formatDateTime(event.occurredAtUtc, locale)} />
        <Row label={t("security.event")} value={event.eventType} mono />
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("security.outcome")}</dt>
          <dd className="m-0"><AuditOutcomeBadge outcome={event.outcome} /></dd>
        </div>
        <Row label={t("security.actorKind")} value={event.actorKind} mono />
        <Row label={t("security.actorId")} value={event.actorUserAccountId} mono />
        <Row label={t("security.subjectId")} value={event.subjectUserAccountId} mono />
        <Row label={t("security.correlation")} value={event.correlationId} mono />
      </dl>
      <div className="flex flex-col gap-2 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("security.metadata")}</h3>
        {metadata.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("security.noMetadata")}</p> : (
          <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
            {metadata.map(([key, value]) => (
              <li key={key} className="grid gap-1 rounded-control border border-line bg-paper p-2 text-sm sm:grid-cols-[180px_minmax(0,1fr)]">
                <span dir="ltr" className="font-mono text-xs font-bold text-ink-muted">{key}</span>
                <span dir="auto" className="[overflow-wrap:anywhere]">{value}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
