"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { NameCell } from "../kit/NameCell";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { PurchaseStatusBadge } from "./PurchaseStatusBadge";
import { fetchPurchases, type PurchaseListItem } from "./purchases-api";
import { isDraft, kindKey, PURCHASE_STATUSES } from "./purchase-rules";
import { PlusIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize } from "../kit/Button";

const FILTERS = { status: "", q: "", page: "1" };

/** ADM-Purchase list: every purchase with its seller, total and what is still owed; filtered by status and search. */
export function Purchases() {
  const t = useTranslations("admin.purchases");
  const locale = useLocale();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const page = pageNumber(values.page);
  const { state, reload } = useLoad(useCallback(
    () => fetchPurchases({ status: values.status || undefined, q: values.q || undefined, page }),
    [values.status, values.q, page]
  ));
  const tableState: Load<readonly PurchaseListItem[]> = state.status === "ready" ? { status: "ready", data: state.data.items } : state;

  const columns: Column<PurchaseListItem>[] = [
    {
      key: "code",
      header: t("columns.code"),
      cell: (purchase) => <Link href={`/admin/purchases/${purchase.id}`} dir="ltr" className="whitespace-nowrap font-mono text-[13px] font-bold hover:underline">{purchase.code}</Link>
    },
    {
      key: "seller",
      header: t("columns.seller"),
      cell: (purchase) => <NameCell name={purchase.seller.name} detail={purchase.seller.phone} />
    },
    { key: "kind", header: t("columns.kind"), cell: (purchase) => <span className="whitespace-nowrap">{t(`kinds.${kindKey(purchase.kind)}`)}</span> },
    { key: "date", header: t("columns.date"), cell: (purchase) => <span className="whitespace-nowrap text-xs">{formatDate(purchase.purchaseDate, locale)}</span> },
    { key: "lines", header: t("columns.lines"), align: "end", cell: (purchase) => <span>{purchase.lineCount}</span> },
    { key: "total", header: t("columns.total"), align: "end", cell: (purchase) => <span className="whitespace-nowrap font-bold">{formatEgp(purchase.total, locale)}</span> },
    {
      key: "remaining",
      header: t("columns.remaining"),
      align: "end",
      cell: (purchase) => <span className={`whitespace-nowrap ${purchase.remaining > 0 ? "font-bold text-error-text" : "text-ink-muted"}`}>{formatEgp(purchase.remaining, locale)}</span>
    },
    { key: "status", header: t("columns.status"), cell: (purchase) => <PurchaseStatusBadge status={purchase.status} /> },
    actionsColumn((purchase) => (
      <RowAction kind={isDraft(purchase) && has("Purchasing.CreateOrder") ? "edit" : "view"} subject={purchase.code} href={`/admin/purchases/${purchase.id}`} />
    ))
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={has("Purchasing.CreateOrder") ? <Link href="/admin/purchases/new" className={buttonClass({ variant: "primary" })}><PlusIcon size={buttonIconSize()} />{t("new")}</Link> : null}
      />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q })} />
        <FilterSelect
          label={t("statusLabel")} value={values.status} onChange={(status) => set({ status })}
          options={[{ value: "", label: t("statuses.all") }, ...PURCHASE_STATUSES.map((status) => ({ value: status, label: t(`statuses.${status}`) }))]}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(purchase) => purchase.id} empty={t("empty")} />
      {state.status === "ready" ? (
        <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
    </>
  );
}
