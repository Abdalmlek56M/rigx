"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTimeFull } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { deviceOf, diffRows, entryTitle, type AuditValue, type DiffRow, type DiffStatus } from "./audit-rules";
import { fetchEntry, fetchLinkedNames } from "./audit-api";
import { AuditActionBadge } from "./AuditActionBadge";
import { AuditField as Row } from "./AuditField";
import { AuditRestore } from "./AuditRestore";
import { useAuditLabels } from "./use-audit-labels";
import { usePermissions } from "../use-permissions";
import { CloseIcon, RefreshIcon } from "@/components/ui/icons";
import { Button, IconButton } from "../kit/Button";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";

/** One data-change entry: who, when and from which device, then each field before → after with additions and removals marked. */
export function AuditEntryDetail({ id, onClose, onRestored }: { id: string; onClose: () => void; onRestored: () => void }) {
  const t = useTranslations("admin.audit");
  const labels = useAuditLabels();
  const locale = useLocale();
  const load = useCallback(() => fetchEntry(id), [id]);
  const { state, reload } = useLoad(load);
  const names = useLoad(fetchLinkedNames).state;
  const { has } = usePermissions();
  if (state.status === "loading") return <div aria-hidden="true" className="h-64 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("failed")}</p>
        <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
      </div>
    );
  }

  const entry = state.data;
  const linkedNames = names.status === "ready" ? names.data : EMPTY_NAMES;
  const rows = diffRows(entry.changes);
  const title = entryTitle(entry.changes);
  const device = deviceOf(entry.device);
  const deviceText = !entry.device ? null
    : device?.browser && device.os ? t("device.full", { browser: device.browser, os: device.os })
    : device?.browser ?? device?.os ?? t("device.unknown");
  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5" aria-label={t("changes.title")}>
      <header className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 flex-col items-start gap-1.5">
          <AuditActionBadge action={entry.action} />
          <h2 className="m-0 text-[17px] font-bold">
            {labels.entity(entry.entity)}
            {title ? <span className="font-normal"> «<span dir="auto" className="[overflow-wrap:anywhere]">{title}</span>»</span> : null}
          </h2>
        </div>
        <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
      </header>
      <dl className="m-0 grid gap-3 sm:grid-cols-2">
        <Row label={t("changes.time")} value={formatDateTimeFull(entry.occurredAtUtc, locale)} />
        <Row label={t("changes.actor")} value={entry.actorName ?? labels.actor(entry.actorKind)} />
        <Row label={t("changes.device")} value={deviceText} />
        <Row label={t("changes.ip")} value={entry.actorIp} mono />
      </dl>
      {entry.restoredFromEntryId ? <p className="m-0 text-sm text-ink-muted">{t("changes.restored")}</p> : null}
      <div className="flex flex-col gap-2 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("changes.fields")}</h3>
        {rows.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("changes.noChanges")}</p> : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {rows.map((row) => <DiffItem key={row.field} entity={entry.entity} row={row} linkedNames={linkedNames} />)}
          </ul>
        )}
      </div>
      {has("Audit.Restore") && entry.canRestore && rows.length > 0 ? <AuditRestore id={entry.id} onRestored={() => { reload(); onRestored(); }} /> : null}
    </section>
  );
}

const EMPTY_NAMES: ReadonlyMap<string, string> = new Map();

const STATUS_TONE: Record<DiffStatus, { item: string; chip: StatusTone }> = {
  changed: { item: "border-orange/40", chip: "warning" },
  added: { item: "border-success/40", chip: "success" },
  removed: { item: "border-error/40", chip: "danger" }
};

/** One field: its Arabic name, whether it was added, changed or removed, and its value(s). */
function DiffItem({ entity, row, linkedNames }: { entity: string; row: DiffRow; linkedNames: ReadonlyMap<string, string> }) {
  const t = useTranslations("admin.audit");
  const labels = useAuditLabels();
  const tone = STATUS_TONE[row.status];
  const name = (text: string) => labels.value(entity, row.field, text);
  const linkSwap = row.status === "changed" && row.before.kind === "linked" && row.after.kind === "linked"
    && !(isNamed(row.before, linkedNames) && isNamed(row.after, linkedNames));
  return (
    <li className={`flex flex-col gap-2 rounded-control border border-s-4 bg-paper p-3 ${tone.item}`}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-sm font-bold">{labels.field(row.field)}</span>
        <StatusBadge tone={tone.chip}>{t(`diff.${row.status}`)}</StatusBadge>
      </div>
      {linkSwap ? <p className="m-0 text-sm">{t("diff.linkedChanged")}</p>
        : row.status === "changed" ? (
          <div className="grid gap-2 text-sm sm:grid-cols-2">
            <Side label={t("changes.before")} value={row.before} name={name} linkedNames={linkedNames} struck />
            <Side label={t("changes.after")} value={row.after} name={name} linkedNames={linkedNames} />
          </div>
        ) : row.status === "added" ? <ValueText value={row.after} name={name} linkedNames={linkedNames} />
        : <ValueText value={row.before} name={name} linkedNames={linkedNames} struck />}
    </li>
  );
}

/** Whether every id of a link has a known name. */
const isNamed = (value: AuditValue, linkedNames: ReadonlyMap<string, string>) => value.kind === "linked" && value.ids.every((id) => linkedNames.has(id));

function Side({ label, value, name, linkedNames, struck = false }: { label: string; value: AuditValue; name: (text: string) => string; linkedNames: ReadonlyMap<string, string>; struck?: boolean }) {
  return (
    <span className="flex min-w-0 flex-col gap-0.5">
      <span className="text-xs text-ink-muted">{label}</span>
      <ValueText value={value} name={name} linkedNames={linkedNames} struck={struck} />
    </span>
  );
}

/** A value in words: coded values by their admin label, yes/no, hidden, a set link and times read as text; nested data stays as a small scrollable block. */
function ValueText({ value, name, linkedNames, struck = false }: { value: AuditValue; name: (text: string) => string; linkedNames: ReadonlyMap<string, string>; struck?: boolean }) {
  const t = useTranslations("admin.audit.diff");
  const locale = useLocale();
  const tone = struck ? "text-ink-muted line-through" : "font-bold";
  switch (value.kind) {
    case "data":
      return <pre dir="ltr" className={`m-0 max-h-48 overflow-auto whitespace-pre-wrap rounded-control bg-surface p-2 text-xs [overflow-wrap:anywhere] ${struck ? "text-ink-muted" : ""}`}>{value.text}</pre>;
    case "text":
      return <span dir="auto" className={`text-sm [overflow-wrap:anywhere] ${tone}`}>{name(value.text)}</span>;
    case "time":
      return <span className={`text-sm ${tone}`}>{formatDateTimeFull(value.iso, locale)}</span>;
    case "linked":
      return <span dir="auto" className={`text-sm [overflow-wrap:anywhere] ${tone}`}>{isNamed(value, linkedNames) ? value.ids.map((id) => linkedNames.get(id)).join("، ") : t("linked")}</span>;
    default:
      return <span className={`text-sm ${tone}`}>{t(value.kind)}</span>;
  }
}
