"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDate, formatEgp } from "@/lib/format";
import { itemsPreview } from "./account-labels";
import { fetchOrders, type Order } from "./account-api";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { OrderProgress } from "./OrderProgress";
import { useLoad } from "@/lib/hooks/use-load";

const PREVIEW_ITEMS = 2;
const linkClass = "inline-flex min-h-11 items-center text-sm font-bold text-orange-text underline hover:text-orange-text-hover";

function OrderCard({ order }: { order: Order }) {
  const t = useTranslations("account.orders");
  const locale = useLocale();
  const { shown, more } = itemsPreview(order.items.map((item) => item.name), PREVIEW_ITEMS);

  return (
    <li className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{order.reference}</span>
        <span className="text-sm text-ink-muted">{formatDate(order.createdAtUtc, locale)}</span>
      </div>
      <OrderProgress order={order} />
      <p dir="auto" className="m-0 text-sm text-ink-muted [unicode-bidi:isolate]">
        {shown.join("، ")}
        {more > 0 ? ` ${t("more", { count: more })}` : ""}
      </p>
      <div className="flex flex-wrap items-center justify-between gap-x-4">
        <b>{formatEgp(order.total, locale)}</b>
        <div className="flex flex-wrap gap-x-5">
          <Link href={`/account/orders/${order.id}`} className={linkClass}>{t("details")}</Link>
          <Link href={`/t/${order.trackingToken}`} className={linkClass}>{t("track")}</Link>
        </div>
      </div>
    </li>
  );
}

/** "طلباتي": the person's orders, newest first (the API's order), each with its progress and links. */
export function OrdersList() {
  const t = useTranslations("account.orders");
  const { state, reload } = useLoad(fetchOrders);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="m-0 text-xl font-bold">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(orders) =>
          orders.length === 0 ? (
            <EmptyState message={t("empty")} action={<Link href="/products" className="text-orange-text underline hover:text-orange-text-hover">{t("browse")}</Link>} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {orders.map((order) => <OrderCard key={order.id} order={order} />)}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
