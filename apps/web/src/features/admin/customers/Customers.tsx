"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDateTime, formatEgp } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, SearchBox } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useUrlFilters } from "../kit/use-url-filters";
import { fetchCustomers, type CustomerRow } from "./customers-api";

const FILTERS = { search: "", page: "1" };

/** ADM-Customer list: name, phone, orders, total spent and the last order. */
export function Customers() {
  const t = useTranslations("admin.customers");
  const locale = useLocale();
  const { values, set } = useUrlFilters(FILTERS);
  const page = pageNumber(values.page);
  const load = useCallback(() => fetchCustomers({ search: values.search || undefined, page }), [values.search, page]);
  const { state, reload } = useLoad(load);
  const tableState: Load<readonly CustomerRow[]> = state.status === "ready" ? { status: "ready", data: state.data.items } : state;

  const columns: Column<CustomerRow>[] = [
    {
      key: "name",
      header: t("columns.name"),
      cell: (row) => (
        <span className="flex flex-col gap-0.5">
          <span className="flex flex-wrap items-center gap-2">
            <Link href={`/admin/customers/${row.customer.id}`} className="font-bold hover:underline">{row.customer.name}</Link>
            {row.customer.isActive ? null : <span className="rounded-[4px] bg-line px-2 py-[3px] text-xs font-bold text-ink-muted">{t("inactive")}</span>}
          </span>
          <span dir="ltr" className="font-mono text-xs text-ink-muted">{row.customer.personCode}</span>
        </span>
      )
    },
    { key: "phone", header: t("columns.phone"), cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.customer.phone ?? t("none")}</span> },
    { key: "orders", header: t("columns.orders"), align: "end", cell: (row) => <span className="font-bold">{row.orderCount}</span> },
    { key: "spent", header: t("columns.spent"), align: "end", cell: (row) => <span className="whitespace-nowrap font-bold">{formatEgp(row.totalSpent, locale)}</span> },
    {
      key: "lastOrder",
      header: t("columns.lastOrder"),
      cell: (row) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{row.lastOrderAtUtc ? formatDateTime(row.lastOrderAtUtc, locale) : t("never")}</span>
    }
  ];

  return (
    <>
      <PageHeader title={t("title")} />
      <FilterBar>
        <SearchBox label={t("search")} value={values.search} onChange={(search) => set({ search })} />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(row) => row.customer.id} empty={t("empty")} />
      {state.status === "ready" ? (
        <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
    </>
  );
}
