"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime, formatEgp } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { NameCell } from "../kit/NameCell";
import { PageHeader } from "../kit/PageHeader";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useUrlFilters } from "../kit/use-url-filters";
import { useReveal } from "../kit/use-reveal";
import { BackInStockPanel } from "./BackInStockPanel";
import { RequestDetail } from "./RequestDetail";
import { RequestStatusBadge, RequestTabs, RequestTypeBadge } from "./RequestBadges";
import { REQUEST_STATUSES, requestTitle } from "./request-labels";
import { fetchRequests, type RequestRow } from "./requests-api";

const FILTERS = { type: "", status: "", q: "", page: "1", id: "" };

/** ADM-Requests: the offers/upgrade/support inbox with a detail panel for the selected request. */
export function Requests() {
  const t = useTranslations("admin.requests");
  const locale = useLocale();
  const { values, set } = useUrlFilters(FILTERS);
  const page = pageNumber(values.page);
  const load = useCallback(
    () => fetchRequests({ type: values.type || undefined, status: values.status || undefined, q: values.q || undefined, page }),
    [values.type, values.status, values.q, page]
  );
  const { state, reload } = useLoad(load);
  const tableState: Load<readonly RequestRow[]> = state.status === "ready" ? { status: "ready", data: state.data.items } : state;
  const select = (id: string) => set({ id, page: values.page });
  const detailRef = useReveal<HTMLDivElement>(values.id || null);

  const columns: Column<RequestRow>[] = [
    {
      key: "request",
      header: t("columns.request"),
      cell: (row) => {
        // The list has no subject/message, so a support or contact request would only repeat its code.
        const title = requestTitle(row) === row.code ? null : requestTitle(row);
        return (
          <button type="button" aria-current={values.id === row.id ? "true" : undefined} onClick={() => select(row.id)} className={`flex min-w-0 flex-col items-start gap-0.5 text-start ${values.id === row.id ? "text-orange-text" : ""}`}>
            {title ? <span dir="auto" className="max-w-56 truncate font-bold [unicode-bidi:isolate]">{title}</span> : null}
            <span dir="ltr" className={`font-mono text-xs ${title ? "text-ink-muted" : "font-bold"}`}>{row.code}</span>
          </button>
        );
      }
    },
    {
      key: "customer",
      header: t("columns.customer"),
      cell: (row) => <NameCell name={row.name} detail={row.phone} />
    },
    { key: "type", header: t("columns.type"), cell: (row) => <RequestTypeBadge type={row.type} /> },
    { key: "status", header: t("columns.status"), cell: (row) => <RequestStatusBadge status={row.status} /> },
    { key: "date", header: t("columns.date"), cell: (row) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{formatDateTime(row.createdAtUtc, locale)}</span> },
    {
      key: "offer",
      header: t("columns.offer"),
      align: "end",
      cell: (row) => <span className="whitespace-nowrap font-bold">{row.offerAmount !== null ? formatEgp(row.offerAmount, locale) : t("none")}</span>
    },
    actionsColumn((row) => <RowAction kind="view" subject={row.code} current={values.id === row.id} onClick={() => select(row.id)} />)
  ];

  return (
    <>
      <PageHeader title={t("title")} />
      <RequestTabs active={values.type} counts={state.status === "ready" ? state.data.newByType : {}} onChange={(type) => set({ type, id: "" })} />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q, id: "" })} />
        <FilterSelect
          label={t("statusLabel")} value={values.status} onChange={(status) => set({ status, id: "" })}
          options={[
            { value: "", label: t("statuses.All") },
            ...REQUEST_STATUSES.map((status) => ({ value: status, label: t(`statuses.${status}`) }))
          ]}
        />
      </FilterBar>
      <DataTable
        label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(row) => row.id}
        empty={t("empty")}
      />
      {state.status === "ready" ? <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} /> : null}
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.id ? <RequestDetail key={values.id} id={values.id} onClose={() => set({ id: "", page: values.page })} onChanged={reload} /> : null}
      </div>
      <BackInStockPanel onChanged={reload} />
    </>
  );
}
