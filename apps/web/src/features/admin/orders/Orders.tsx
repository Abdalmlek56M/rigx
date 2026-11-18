"use client";

import { useCallback, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime, formatEgp } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { usePermissions } from "../use-permissions";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { useReveal } from "../kit/use-reveal";
import { useUrlFilters } from "../kit/use-url-filters";
import { BlacklistPanel } from "./BlacklistPanel";
import { OrderDetail } from "./OrderDetail";
import { OrderStatusBadge, OrderTabs, PaymentStatusBadge } from "./OrderBadges";
import { fetchOrders, type Order } from "./orders-api";
import { filterOrders, orderContent, ORDER_CHANNELS, reservationState } from "./order-rules";

const FILTERS = { status: "", channel: "", q: "", id: "" };

/** The API returns the newest orders only, up to this many. */
const LIST_LIMIT = 300;

function ReservationCell({ order }: { order: Order }) {
  const t = useTranslations("admin.orders");
  const state = reservationState(order, new Date());
  if (state.kind === "held") return <span className="whitespace-nowrap">{t("reservation.held", { hours: state.hoursLeft })}</span>;
  if (state.kind === "expired") return <span className="whitespace-nowrap font-bold text-error">{t("reservation.expired")}</span>;
  return <span className="text-ink-muted">{t("none")}</span>;
}

/** ADM-Orders: the order list with status tabs, the selected order below it, and the blacklist. */
export function Orders() {
  const t = useTranslations("admin.orders");
  const locale = useLocale();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const { state, reload } = useLoad(useCallback(() => fetchOrders(values.status), [values.status]));
  // Bumped when the blacklist changes: the open order and the panel read it again.
  const [blacklistVersion, setBlacklistVersion] = useState(0);
  const detailRef = useReveal<HTMLDivElement>(values.id || null);

  const tableState: Load<readonly Order[]> = useMemo(
    () => (state.status === "ready" ? { status: "ready", data: filterOrders(state.data, values) } : state),
    [state, values]
  );
  const blacklistChanged = () => {
    setBlacklistVersion((version) => version + 1);
    reload();
  };

  const columns: Column<Order>[] = [
    {
      key: "order",
      header: t("columns.order"),
      cell: (order) => (
        <button
          type="button" dir="ltr" aria-current={values.id === order.id ? "true" : undefined} onClick={() => set({ id: order.id })}
          className={`min-h-8 whitespace-nowrap font-mono text-[13px] font-bold ${values.id === order.id ? "text-orange-text" : ""}`}
        >
          {order.reference}
        </button>
      )
    },
    {
      key: "customer",
      header: t("columns.customer"),
      cell: (order) => (
        <span className="flex flex-col items-start gap-0.5">
          <span className="flex flex-wrap items-center gap-1.5">
            <span>{order.customerName}</span>
            {order.isBlacklisted ? <span className="rounded-[4px] bg-error px-1.5 py-0.5 text-[11px] font-bold text-surface">{t("blacklisted")}</span> : null}
          </span>
          <span className="text-xs text-ink-muted">{t(`channels.${order.channel === "Store" ? "Store" : "Online"}`)}</span>
        </span>
      )
    },
    {
      key: "content",
      header: t("columns.content"),
      cell: (order) => {
        const { first, more } = orderContent(order);
        return (
          <span className="flex items-center gap-1.5">
            <span dir="auto" className="max-w-48 truncate [unicode-bidi:isolate]">{first}</span>
            {more > 0 ? <span dir="ltr" className="text-xs text-ink-muted">{t("moreItems", { count: more })}</span> : null}
          </span>
        );
      }
    },
    { key: "total", header: t("columns.total"), align: "end", cell: (order) => <span className="whitespace-nowrap font-bold">{formatEgp(order.total, locale)}</span> },
    { key: "status", header: t("columns.status"), cell: (order) => <OrderStatusBadge status={order.status} /> },
    { key: "payment", header: t("columns.payment"), cell: (order) => <PaymentStatusBadge status={order.paymentStatus} /> },
    { key: "reservation", header: t("columns.reservation"), cell: (order) => <ReservationCell order={order} /> },
    { key: "date", header: t("columns.date"), cell: (order) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{formatDateTime(order.createdAtUtc, locale)}</span> }
  ];

  return (
    <>
      <PageHeader title={t("title")} />
      <OrderTabs active={values.status} onChange={(status) => set({ status, id: "" })} />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q })} />
        <FilterSelect
          label={t("channelLabel")} value={values.channel} onChange={(channel) => set({ channel })}
          options={[{ value: "", label: t("channels.All") }, ...ORDER_CHANNELS.map((channel) => ({ value: channel, label: t(`channels.${channel}`) }))]}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(order) => order.id} empty={t("empty")} />
      {state.status === "ready" && state.data.length >= LIST_LIMIT ? <p className="m-0 text-sm text-ink-muted">{t("newestOnly", { count: LIST_LIMIT })}</p> : null}
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.id ? (
          <OrderDetail
            key={`${values.id}-${blacklistVersion}`} id={values.id} onClose={() => set({ id: "" })}
            onChanged={reload} onBlacklistChanged={blacklistChanged}
          />
        ) : null}
      </div>
      {has("Sales.ViewCustomerContact") ? <BlacklistPanel reloadKey={blacklistVersion} onChanged={blacklistChanged} canManage={has("Sales.ApproveOrder")} /> : null}
    </>
  );
}
