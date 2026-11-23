"use client";

import { useCallback, useMemo } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatWeekday } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { CloseIcon } from "@/components/ui/icons";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { IconButton } from "../kit/Button";
import { EmptyPanel, ErrorPanel, LoadingCard } from "../kit/LoadState";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { AuditEntryDetail } from "./AuditEntryDetail";
import { AuditEntryRow } from "./AuditEntryRow";
import { fetchEntries } from "./audit-api";
import { groupByKind, type AuditSet, type AuditValues } from "./audit-rules";

/** The picked day over the month list: its entries by kind of work, and, once one is picked, that entry's before/after in the same sheet. */
export function DaySheet({ values, set }: { values: AuditValues; set: AuditSet }) {
  const t = useTranslations("admin.audit");
  const close = () => set({ day: "", entry: "" });
  return (
    <ModalSheet open={values.day !== ""} onClose={close} label={t("day.label")} edge="end" fullHeight wide>
      <div className="h-full overflow-y-auto overscroll-contain bg-paper p-4 md:p-5">
        <DayContent values={values} set={set} onClose={close} />
      </div>
    </ModalSheet>
  );
}

function DayContent({ values, set, onClose }: { values: AuditValues; set: AuditSet; onClose: () => void }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const page = pageNumber(values.page);
  const entries = useLoad(useCallback(
    () => fetchEntries({
      module: values.module || undefined, entity: values.entity || undefined, action: values.action || undefined,
      from: values.day, to: values.day, page
    }),
    [values.module, values.entity, values.action, values.day, page]
  ));
  const items = entries.state.status === "ready" ? entries.state.data.items : null;
  const groups = useMemo(() => (items ? groupByKind(items) : []), [items]);
  const noon = `${values.day}T12:00:00Z`;

  if (values.entry) {
    return <AuditEntryDetail id={values.entry} onClose={() => set({ entry: "" })} onRestored={entries.reload} />;
  }
  return (
    <div className="flex flex-col gap-4">
      <header className="flex items-start justify-between gap-3">
        <div className="flex flex-col gap-0.5">
          <h2 className="type-section-title m-0">{formatDate(noon, locale)}</h2>
          <p className="m-0 flex flex-wrap items-center gap-2 text-sm text-ink-muted">
            {formatWeekday(noon, locale)}
            {entries.state.status === "ready" ? <StatusBadge tone="neutral">{t("changes.count", { count: entries.state.data.total })}</StatusBadge> : null}
          </p>
        </div>
        <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
      </header>
      {entries.state.status === "loading" ? <LoadingCard className="h-64" /> : null}
      {entries.state.status === "error" ? (
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={entries.reload} />
      ) : null}
      {items && items.length === 0 ? <EmptyPanel message={t("empty")} /> : null}
      {groups.map(({ group, entries: rows }) => (
        <div key={group} className="overflow-hidden rounded-card border border-line bg-surface">
          <h3 className="m-0 border-b border-line bg-paper px-4 py-2 text-[13px] font-bold text-ink-muted">{t(`groups.${group}`)}</h3>
          <ul className="m-0 list-none divide-y divide-line p-0">
            {rows.map((entry) => <AuditEntryRow key={entry.id} entry={entry} picked={false} onPick={(id) => set({ entry: id })} />)}
          </ul>
        </div>
      ))}
      {entries.state.status === "ready" ? (
        <Pager info={{ page: entries.state.data.page, pageSize: entries.state.data.pageSize, total: entries.state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
    </div>
  );
}
