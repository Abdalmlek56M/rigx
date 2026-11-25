"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { DateField } from "../kit/DateField";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useReveal } from "../kit/use-reveal";
import { AuditOutcomeBadge } from "./AuditOutcomeBadge";
import { fetchEvents, type SecurityEvent } from "./audit-api";
import { AUDIT_OUTCOMES, auditRangeUtc, shortId, type AuditSet, type AuditValues } from "./audit-rules";
import { SecurityEventDetail } from "./SecurityEventDetail";
import { CloseIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Sign-in and security events: filters, the paged table and the picked event below it. */
export function SecurityView({ values, set }: { values: AuditValues; set: AuditSet }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const page = pageNumber(values.page);
  const { fromUtc, toUtc } = auditRangeUtc(values.from, values.to);
  const load = useCallback(
    () => fetchEvents({
      fromUtc: fromUtc ?? undefined, toUtc: toUtc ?? undefined,
      eventType: values.eventType || undefined, outcome: values.outcome || undefined, search: values.search || undefined, page
    }),
    [fromUtc, toUtc, values.eventType, values.outcome, values.search, page]
  );
  const { state, reload } = useLoad(load);
  const tableState: Load<readonly SecurityEvent[]> = state.status === "ready" ? { status: "ready", data: state.data.items } : state;
  const detailRef = useReveal<HTMLDivElement>(values.entry || null);
  const selected = state.status === "ready" ? state.data.items.find((event) => event.id === values.entry) : undefined;
  const clear = () => set({ entry: "", page: values.page });

  const columns: Column<SecurityEvent>[] = [
    {
      key: "time", header: t("columns.time"), cell: (event) => (
        <button
          type="button" aria-current={values.entry === event.id ? "true" : undefined}
          onClick={() => set({ entry: event.id, page: values.page })}
          className={`whitespace-nowrap font-mono text-xs ${values.entry === event.id ? "font-bold text-orange-text" : ""}`}
        >
          {formatDateTime(event.occurredAtUtc, locale)}
        </button>
      )
    },
    { key: "event", header: t("columns.event"), cell: (event) => <span dir="ltr" className="block max-w-64 break-words font-mono text-xs">{event.eventType}</span> },
    { key: "outcome", header: t("columns.outcome"), cell: (event) => <AuditOutcomeBadge outcome={event.outcome} /> },
    {
      key: "actor", header: t("columns.actor"), cell: (event) => (
        <span className="flex flex-col gap-0.5 text-xs">
          <span dir="ltr" className="font-mono">{event.actorKind}</span>
          {event.actorUserAccountId ? <span dir="ltr" className="font-mono text-ink-muted">{shortId(event.actorUserAccountId)}</span> : null}
        </span>
      )
    },
    { key: "subject", header: t("columns.subject"), cell: (event) => <span dir="ltr" className="font-mono text-xs text-ink-muted">{shortId(event.subjectUserAccountId) || t("none")}</span> },
    actionsColumn((event) => (
      <RowAction
        kind="view" subject={formatDateTime(event.occurredAtUtc, locale)} current={values.entry === event.id}
        onClick={() => set({ entry: event.id, page: values.page })}
      />
    ))
  ];

  return (
    <>
      <FilterBar>
        <SearchBox label={t("filters.eventType")} value={values.eventType} onChange={(eventType) => set({ eventType, entry: "" })} />
        <FilterSelect
          titled label={t("filters.outcome")} value={values.outcome} onChange={(outcome) => set({ outcome, entry: "" })}
          options={[
            { value: "", label: t("filters.all") },
            ...AUDIT_OUTCOMES.map((outcome) => ({ value: outcome, label: t(`outcomes.${outcome}`) }))
          ]}
        />
        <SearchBox label={t("filters.search")} value={values.search} onChange={(search) => set({ search, entry: "" })} />
        <div className="w-36"><DateField label={t("filters.from")} name="from" value={values.from} onChange={(from) => set({ from, entry: "" })} /></div>
        <div className="w-36"><DateField label={t("filters.to")} name="to" value={values.to} onChange={(to) => set({ to, entry: "" })} /></div>
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(event) => event.id} empty={t("empty")} />
      {state.status === "ready" ? (
        <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.entry && state.status === "ready" ? selected ? (
          <SecurityEventDetail event={selected} onClose={clear} />
        ) : (
          <div className="flex flex-wrap items-center justify-between gap-2 rounded-card border border-line bg-surface p-5 text-sm text-ink-muted">
            {t("security.notOnPage")}
            <Button icon={CloseIcon} onClick={clear}>{t("close")}</Button>
          </div>
        ) : null}
      </div>
    </>
  );
}
