"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { TextField } from "@/components/ui/TextField";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useReveal } from "../kit/use-reveal";
import { AuditEntryDetail } from "./AuditEntryDetail";
import type { AuditEntry } from "./audit-api";
import { fetchAuditFilters, fetchEntries } from "./audit-api";
import { shortId, type AuditSet, type AuditValues } from "./audit-rules";

/** The data-change log: filters, the paged table and the picked entry below it. */
export function ChangesView({ values, set }: { values: AuditValues; set: AuditSet }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const page = pageNumber(values.page);
  const filters = useLoad(useCallback(() => fetchAuditFilters(), []));
  const entries = useLoad(useCallback(
    () => fetchEntries({
      module: values.module || undefined, entity: values.entity || undefined, action: values.action || undefined,
      from: values.from || undefined, to: values.to || undefined, page
    }),
    [values.module, values.entity, values.action, values.from, values.to, page]
  ));
  const options = filters.state.status === "ready" ? filters.state.data : { modules: [], entities: [], actions: [] };
  const tableState: Load<readonly AuditEntry[]> = entries.state.status === "ready"
    ? { status: "ready", data: entries.state.data.items }
    : entries.state;
  const detailRef = useReveal<HTMLDivElement>(values.entry || null);

  const columns: Column<AuditEntry>[] = [
    {
      key: "time", header: t("columns.time"), cell: (entry) => (
        <button
          type="button" aria-current={values.entry === entry.id ? "true" : undefined}
          onClick={() => set({ entry: entry.id, page: values.page })}
          className={`whitespace-nowrap font-mono text-xs ${values.entry === entry.id ? "font-bold text-orange-text" : ""}`}
        >
          {formatDateTime(entry.occurredAtUtc, locale)}
        </button>
      )
    },
    {
      key: "who", header: t("columns.who"), cell: (entry) => (
        <span className="flex flex-col gap-0.5">
          <span>{entry.actorName ?? entry.actorKind}</span>
          {entry.actorName ? <span dir="ltr" className="font-mono text-[11px] text-ink-muted">{entry.actorKind}</span> : null}
        </span>
      )
    },
    { key: "module", header: t("columns.module"), cell: (entry) => <span dir="ltr" className="block max-w-32 break-words font-mono text-xs">{entry.module}</span> },
    {
      key: "entity", header: t("columns.entity"), cell: (entry) => (
        <span className="flex min-w-0 flex-col gap-0.5" title={entry.entityId}>
          <span dir="ltr" className="break-words font-mono text-xs">{entry.entity}</span>
          <span dir="ltr" className="font-mono text-[11px] text-ink-muted">{shortId(entry.entityId)}</span>
        </span>
      )
    },
    { key: "action", header: t("columns.action"), cell: (entry) => <span dir="ltr" className="inline-block whitespace-nowrap rounded-[4px] border border-input-line bg-surface px-2 py-[3px] font-mono text-[11px] font-bold">{entry.action}</span> }
  ];

  return (
    <>
      <FilterBar>
        <FilterSelect
          label={t("filters.module")} value={values.module} onChange={(module) => set({ module, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.modules.map((module) => ({ value: module, label: module }))]}
        />
        <FilterSelect
          label={t("filters.entity")} value={values.entity} onChange={(entity) => set({ entity, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.entities.map((entity) => ({ value: entity, label: entity }))]}
        />
        <FilterSelect
          label={t("filters.action")} value={values.action} onChange={(action) => set({ action, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.actions.map((action) => ({ value: action, label: action }))]}
        />
        <div className="w-36"><TextField label={t("filters.from")} name="from" type="date" ltr value={values.from} onChange={(from) => set({ from, entry: "" })} /></div>
        <div className="w-36"><TextField label={t("filters.to")} name="to" type="date" ltr value={values.to} onChange={(to) => set({ to, entry: "" })} /></div>
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={entries.reload} rowKey={(entry) => entry.id} empty={t("empty")} />
      {entries.state.status === "ready" ? (
        <Pager info={{ page: entries.state.data.page, pageSize: entries.state.data.pageSize, total: entries.state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.entry ? <AuditEntryDetail id={values.entry} onClose={() => set({ entry: "", page: values.page })} onRestored={entries.reload} /> : null}
      </div>
    </>
  );
}
